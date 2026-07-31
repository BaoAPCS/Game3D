using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class DoorInteractable : Chapter1Interactable
    {
        private const string DoorRoomNamHingeName = "Door_Room_Nam_Hinge";
        private const string DoorRoomNamInteractionPointName =
            "Door_Room_Nam_InteractionPoint";
        private const string RoomInsideMarkerName = "room_nam_in";
        private const string RoomOutsideMarkerName = "room_nam_out";
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
        [SerializeField] private Transform insideMarker;
        [SerializeField] private Transform outsideMarker;

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
        private bool colliderDisabledForAnimation;

        public bool IsOpen => isOpen;
        public bool IsAnimating => isAnimating;
        public bool IsFullyClosed => !isOpen && !isAnimating;

        public Transform InsideMarker
        {
            get
            {
                ResolveReferences();
                return insideMarker;
            }
        }

        public Transform OutsideMarker
        {
            get
            {
                ResolveReferences();
                return outsideMarker;
            }
        }

        public Vector3 DoorwayPoint
        {
            get
            {
                ResolveReferences();
                return GetDoorwayPoint();
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallDoorRoomNamInteraction(scene);
        }

        private static void InstallDoorRoomNamInteraction(Scene scene)
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
                if (candidate.gameObject.scene != scene ||
                    candidate.name != DoorRoomNamHingeName)
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
                RestoreDoorCollider();
            }
        }

        protected override void OnDisable()
        {
            RestoreDoorCollider();
            base.OnDisable();
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

        public bool TryGetPlanarSide(Vector3 position, out float side)
        {
            ResolveReferences();
            side = 0f;

            if (!TryGetInsideDirection(out Vector3 insideDirection))
            {
                return false;
            }

            Vector3 offset = position - GetDoorwayPoint();
            offset.y = 0f;
            side = Vector3.Dot(offset, insideDirection);
            return true;
        }

        public bool IsOnInsideSide(Vector3 position, float margin = 0f)
        {
            return TryGetPlanarSide(position, out float side) &&
                   side > Mathf.Max(0f, margin);
        }

        public bool DidCrossDoorway(
            Vector3 from,
            Vector3 to,
            bool towardInside,
            float halfWidth = 1.1f)
        {
            if (halfWidth <= 0f ||
                !TryGetPlanarSide(from, out float fromSide) ||
                !TryGetPlanarSide(to, out float toSide))
            {
                return false;
            }

            bool crossedInRequestedDirection = towardInside
                ? fromSide < 0f && toSide >= 0f
                : fromSide > 0f && toSide <= 0f;
            if (!crossedInRequestedDirection)
            {
                return false;
            }

            float sideDelta = toSide - fromSide;
            if (Mathf.Approximately(sideDelta, 0f))
            {
                return false;
            }

            float intersectionRatio = -fromSide / sideDelta;
            if (intersectionRatio < 0f || intersectionRatio > 1f)
            {
                return false;
            }

            Vector3 crossingPoint =
                Vector3.LerpUnclamped(from, to, intersectionRatio);
            if (!IsAtDoorStorey(crossingPoint.y) ||
                !TryGetInsideDirection(out Vector3 insideDirection))
            {
                return false;
            }

            Vector3 doorwayOffset = crossingPoint - GetDoorwayPoint();
            doorwayOffset.y = 0f;
            Vector3 doorwayTangent =
                new Vector3(-insideDirection.z, 0f, insideDirection.x);

            return Mathf.Abs(Vector3.Dot(
                       doorwayOffset,
                       doorwayTangent)) <= halfWidth;
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
            ResolveReferences();
            if (doorCollider != null && doorCollider.enabled)
            {
                doorCollider.enabled = false;
                colliderDisabledForAnimation = true;
            }
        }

        private void RestoreDoorCollider()
        {
            if (!colliderDisabledForAnimation)
            {
                return;
            }

            if (doorCollider != null)
            {
                doorCollider.enabled = true;
                Physics.SyncTransforms();
            }

            colliderDisabledForAnimation = false;
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

            ResolveDoorMarkers();
        }

        private void ResolveDoorMarkers()
        {
            if (insideMarker != null && outsideMarker != null)
            {
                return;
            }

            Transform ancestor = transform.parent;
            while (ancestor != null)
            {
                Transform foundInside =
                    ancestor.Find(RoomInsideMarkerName);
                Transform foundOutside =
                    ancestor.Find(RoomOutsideMarkerName);
                if (foundInside != null && foundOutside != null)
                {
                    insideMarker = foundInside;
                    outsideMarker = foundOutside;
                    return;
                }

                ancestor = ancestor.parent;
            }
        }

        private bool TryGetInsideDirection(out Vector3 insideDirection)
        {
            insideDirection = Vector3.zero;
            if (insideMarker == null || outsideMarker == null)
            {
                return false;
            }

            insideDirection = insideMarker.position - outsideMarker.position;
            insideDirection.y = 0f;
            float directionLengthSquared = insideDirection.sqrMagnitude;
            if (directionLengthSquared <= Mathf.Epsilon)
            {
                insideDirection = Vector3.zero;
                return false;
            }

            insideDirection /= Mathf.Sqrt(directionLengthSquared);
            return true;
        }

        private Vector3 GetDoorwayPoint()
        {
            if (interactionTarget != null)
            {
                return interactionTarget.position;
            }

            if (doorCollider != null)
            {
                return doorCollider.bounds.center;
            }

            return transform.position;
        }

        private bool IsAtDoorStorey(float positionY)
        {
            if (doorCollider != null)
            {
                const float VerticalTolerance = 0.25f;
                Bounds doorBounds = doorCollider.bounds;
                return positionY >= doorBounds.min.y - VerticalTolerance &&
                       positionY <= doorBounds.max.y + VerticalTolerance;
            }

            if (insideMarker == null || outsideMarker == null)
            {
                return false;
            }

            const float FallbackDoorHeight = 2.5f;
            float floorHeight =
                (insideMarker.position.y + outsideMarker.position.y) * 0.5f;
            return positionY >= floorHeight - 0.25f &&
                   positionY <= floorHeight + FallbackDoorHeight;
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
