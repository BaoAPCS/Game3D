using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class DoorInteractable : Chapter1Interactable
    {
        private const string DoorRoomNamHingeName = "Door_Room_Nam_Hinge";
        private const string DoorRoomNamInteractionPointName =
            "Door_Room_Nam_InteractionPoint";
        private const string InteractableLayerName = "Interactable";

        private static readonly int OpenIntoRoomState =
            Animator.StringToHash("Base Layer.Door_Room_OpenIntoRoom");
        private static readonly int OpenIntoHallwayState =
            Animator.StringToHash("Base Layer.Door_Room_OpenIntoHallway");
        private static readonly int CloseFromRoomState =
            Animator.StringToHash("Base Layer.Door_Room_CloseFromRoom");
        private static readonly int CloseFromHallwayState =
            Animator.StringToHash("Base Layer.Door_Room_CloseFromHallway");

        [Header("Door References")]
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private Collider doorCollider;
        [SerializeField] private Transform interactionTarget;
        [SerializeField] private RoomDoorKeypadController keypadController;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float maximumDistanceFromDoor = 0.5f;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float animationDuration = 0.6f;
        [SerializeField] private float openAngle = 90f;

        private bool isOpen;
        private bool isAnimating;
        private bool openedIntoRoom;
        private float animationEndsAt;
        private Vector3 closedDoorLeafOffset;
        private Vector3 closedUp;
        private Vector3 closedForward;

        public bool IsOpen => isOpen;
        public bool IsAnimating => isAnimating;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallDoorRoomNamInteraction()
        {
            int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                Debug.LogWarning(
                    $"[DoorInteractable] Layer '{InteractableLayerName}' does not exist.");
            }

            Transform[] sceneTransforms =
                FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (Transform candidate in sceneTransforms)
            {
                if (candidate.name != DoorRoomNamHingeName)
                {
                    continue;
                }

                InstallInteractionOnHinge(candidate.gameObject, interactableLayer);
            }
        }

        private static void InstallInteractionOnHinge(
            GameObject hinge,
            int interactableLayer)
        {
            if (interactableLayer >= 0)
            {
                Transform[] hierarchy = hinge.GetComponentsInChildren<Transform>(true);
                foreach (Transform item in hierarchy)
                {
                    item.gameObject.layer = interactableLayer;
                }
            }

            Transform fixedInteractionPoint =
                FindOrCreateFixedInteractionPoint(hinge, interactableLayer);

            if (!hinge.TryGetComponent(out DoorInteractable interactable))
            {
                interactable = hinge.AddComponent<DoorInteractable>();
            }

            interactable.ResolveReferences();
            interactable.interactionTarget = fixedInteractionPoint;
            interactable.CaptureClosedDoorPose();
        }

        private static Transform FindOrCreateFixedInteractionPoint(
            GameObject hinge,
            int interactableLayer)
        {
            Transform hingeTransform = hinge.transform;
            Transform fixedPoint = null;

            if (hingeTransform.parent != null)
            {
                fixedPoint = hingeTransform.parent.Find(
                    DoorRoomNamInteractionPointName);
            }

            if (fixedPoint == null)
            {
                GameObject pointObject =
                    new GameObject(DoorRoomNamInteractionPointName);
                fixedPoint = pointObject.transform;
                fixedPoint.SetParent(hingeTransform.parent, true);
            }

            Collider leafCollider =
                hinge.GetComponentInChildren<Collider>(true);
            fixedPoint.position = leafCollider != null
                ? leafCollider.bounds.center
                : hingeTransform.position;
            fixedPoint.rotation = hingeTransform.rotation;
            fixedPoint.localScale = Vector3.one;

            if (interactableLayer >= 0)
            {
                fixedPoint.gameObject.layer = interactableLayer;
            }

            return fixedPoint;
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            CaptureClosedDoorPose();
        }

        private void Update()
        {
            if (isAnimating && Time.time >= animationEndsAt)
            {
                isAnimating = false;
            }
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            if (isAnimating)
            {
                return string.Empty;
            }

            ResolveReferences();
            if (keypadController != null &&
                keypadController.IsInKeypadMode)
            {
                return string.Empty;
            }

            if (isOpen)
            {
                return "[F] Đóng cửa";
            }

            return "[F] Mở cửa";
        }

        public override bool CanInteract(InteractionContext context)
        {
            if (!base.CanInteract(context) ||
                isAnimating ||
                context.PlayerTransform == null)
            {
                return false;
            }

            ResolveReferences();
            if (keypadController != null &&
                keypadController.IsInKeypadMode)
            {
                return false;
            }

            if (doorCollider == null || !doorCollider.enabled)
            {
                return false;
            }

            Vector3 playerPosition = context.PlayerTransform.position;
            Vector3 leafPoint = doorCollider.ClosestPoint(playerPosition);
            Vector3 doorwayPoint = interactionTarget != null
                ? interactionTarget.position
                : leafPoint;
            float maximumDistanceSquared =
                maximumDistanceFromDoor * maximumDistanceFromDoor;

            return IsWithinPlanarRange(
                       playerPosition,
                       leafPoint,
                       maximumDistanceSquared) ||
                   IsWithinPlanarRange(
                       playerPosition,
                       doorwayPoint,
                       maximumDistanceSquared);
        }

        private static bool IsWithinPlanarRange(
            Vector3 firstPosition,
            Vector3 secondPosition,
            float maximumDistanceSquared)
        {
            firstPosition.y = 0f;
            secondPosition.y = 0f;

            return (firstPosition - secondPosition).sqrMagnitude <=
                   maximumDistanceSquared;
        }

        public override Transform GetInteractionTransform()
        {
            ResolveReferences();
            return interactionTarget != null ? interactionTarget : transform;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            if (isAnimating)
            {
                return InteractionResult.Ignored();
            }

            ResolveReferences();
            if (doorAnimator == null)
            {
                return InteractionResult.Failed(
                    "Cửa chưa được gắn Animator.");
            }

            if (isOpen)
            {
                return CloseDoor();
            }

            if (context.PlayerTransform == null)
            {
                return InteractionResult.Failed(
                    "Không xác định được vị trí người chơi.");
            }

            if (keypadController != null &&
                keypadController.RequiresPassword(
                    context.PlayerTransform.position))
            {
                return keypadController.BeginKeypadEntry(this, context);
            }

            return OpenDoorAwayFrom(context.PlayerTransform.position);
        }

        internal InteractionResult OpenAfterKeypad(
            Vector3 playerPosition)
        {
            if (isOpen || isAnimating)
            {
                return InteractionResult.Ignored();
            }

            ResolveReferences();
            if (doorAnimator == null)
            {
                return InteractionResult.Failed(
                    "Cửa chưa được gắn Animator.");
            }

            return OpenDoorAwayFrom(playerPosition);
        }

        private InteractionResult OpenDoorAwayFrom(Vector3 playerPosition)
        {
            openedIntoRoom = ShouldOpenIntoRoom(playerPosition);
            int stateHash = openedIntoRoom
                ? OpenIntoRoomState
                : OpenIntoHallwayState;

            if (!PlayState(stateHash))
            {
                return InteractionResult.Failed(
                    "Không tìm thấy animation mở cửa.");
            }

            isOpen = true;
            BeginAnimationLock();
            return InteractionResult.Succeeded("Đang mở cửa.");
        }

        private InteractionResult CloseDoor()
        {
            int stateHash = openedIntoRoom
                ? CloseFromRoomState
                : CloseFromHallwayState;

            if (!PlayState(stateHash))
            {
                return InteractionResult.Failed(
                    "Không tìm thấy animation đóng cửa.");
            }

            isOpen = false;
            BeginAnimationLock();
            return InteractionResult.Succeeded("Đang đóng cửa.");
        }

        private bool ShouldOpenIntoRoom(Vector3 playerPosition)
        {
            Quaternion positiveRotation =
                Quaternion.AngleAxis(openAngle, closedUp);
            Quaternion negativeRotation =
                Quaternion.AngleAxis(-openAngle, closedUp);

            Vector3 positiveDoorCenter =
                transform.position + positiveRotation * closedDoorLeafOffset;
            Vector3 negativeDoorCenter =
                transform.position + negativeRotation * closedDoorLeafOffset;

            float positiveDistance =
                (positiveDoorCenter - playerPosition).sqrMagnitude;
            float negativeDistance =
                (negativeDoorCenter - playerPosition).sqrMagnitude;

            if (!Mathf.Approximately(positiveDistance, negativeDistance))
            {
                return positiveDistance > negativeDistance;
            }

            return Vector3.Dot(
                       playerPosition - transform.position,
                       closedForward) <= 0f;
        }

        private bool PlayState(int stateHash)
        {
            if (!doorAnimator.isActiveAndEnabled ||
                !doorAnimator.HasState(0, stateHash))
            {
                return false;
            }

            doorAnimator.Play(stateHash, 0, 0f);
            return true;
        }

        private void BeginAnimationLock()
        {
            isAnimating = true;
            animationEndsAt = Time.time + animationDuration;
        }

        private void ResolveReferences()
        {
            if (doorAnimator == null)
            {
                doorAnimator = GetComponent<Animator>();
            }

            if (doorCollider == null)
            {
                doorCollider = GetComponentInChildren<Collider>(true);
            }

            if (interactionTarget == null && doorCollider != null)
            {
                interactionTarget = doorCollider.transform;
            }

            if (keypadController == null)
            {
                keypadController =
                    GetComponentInParent<RoomDoorKeypadController>();
            }
        }

        private void CaptureClosedDoorPose()
        {
            ResolveReferences();

            closedUp = transform.up;
            closedForward = transform.forward;

            if (doorCollider != null)
            {
                closedDoorLeafOffset =
                    doorCollider.bounds.center - transform.position;
            }
            else
            {
                closedDoorLeafOffset = -transform.right;
            }
        }

        private void OnValidate()
        {
            maximumDistanceFromDoor =
                Mathf.Max(0.5f, maximumDistanceFromDoor);
            animationDuration = Mathf.Max(0.01f, animationDuration);
            openAngle = Mathf.Clamp(Mathf.Abs(openAngle), 1f, 179f);
            ResolveReferences();
        }
    }
}
