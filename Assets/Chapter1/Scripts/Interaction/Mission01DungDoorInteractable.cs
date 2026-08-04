using System.Collections;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Mission01DungDoorInteractable : Chapter1Interactable
    {
        [SerializeField] private Mission01AudioSeparatorManager missionManager;
        [SerializeField] private Mission01KeypadUIController keypadController;
        [SerializeField] private Transform doorToRotate;
        [SerializeField] private float openAngle = 88f;
        [SerializeField] private float openSeconds = 0.55f;

        private Quaternion closedLocalRotation;
        private Coroutine openRoutine;
        private bool doorOpened;

        public bool DoorOpened => doorOpened;
        public string CorrectPassword => Mission01AudioSeparatorManager.CorrectDoorPassword;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            ConfigureCollider();
            if (doorToRotate != null)
            {
                closedLocalRotation = doorToRotate.localRotation;
            }
        }

        private void Start()
        {
            if (missionManager != null && missionManager.DoorUnlocked)
            {
                ApplyOpenRotationImmediate();
            }
        }

        private void OnValidate()
        {
            openAngle = Mathf.Clamp(openAngle, 1f, 179f);
            openSeconds = Mathf.Max(0.01f, openSeconds);
            ConfigureCollider();
        }

        public void Configure(Mission01AudioSeparatorManager manager, Mission01KeypadUIController keypad, Transform door)
        {
            missionManager = manager;
            keypadController = keypad;
            doorToRotate = door;
            if (doorToRotate != null)
            {
                closedLocalRotation = doorToRotate.localRotation;
            }
        }

        public override string GetInteractionPrompt(InteractionContext context)
        {
            ResolveReferences();
            if (missionManager == null || missionManager.State < FirstMissionState.GoToDungRoom)
            {
                return string.Empty;
            }

            if (missionManager.DoorUnlocked || doorOpened)
            {
                return "[F] Mở cửa phòng Dũng";
            }

            if (missionManager.State >= FirstMissionState.SolveBirthdayPassword)
            {
                return "[F] Nhập mật khẩu phòng Dũng";
            }

            return "[F] Kiểm tra cửa phòng Dũng";
        }

        public override bool CanInteract(InteractionContext context)
        {
            ResolveReferences();
            return base.CanInteract(context)
                && missionManager != null
                && missionManager.State >= FirstMissionState.GoToDungRoom;
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            ResolveReferences();
            if (missionManager == null)
            {
                return InteractionResult.Failed("Chưa tìm thấy Mission 01 Manager.");
            }

            if (missionManager.DoorUnlocked || doorOpened)
            {
                OpenDoor();
                return InteractionResult.Succeeded("Cửa phòng Dũng đã mở.");
            }

            if (missionManager.State == FirstMissionState.GoToDungRoom)
            {
                missionManager.DiscoverLockedDoor();
                return InteractionResult.Succeeded("Cửa được khóa bằng mật khẩu.");
            }

            if (missionManager.State < FirstMissionState.SolveBirthdayPassword)
            {
                return InteractionResult.Succeeded("Cửa được khóa bằng mật khẩu.");
            }

            if (keypadController == null)
            {
                return InteractionResult.Failed("Keypad phòng Dũng chưa được liên kết.");
            }

            return keypadController.Open(this, context);
        }

        public bool SubmitPassword(string password)
        {
            ResolveReferences();
            if (missionManager == null)
            {
                return false;
            }

            if (!missionManager.TryUnlockDungDoor(password))
            {
                return false;
            }

            OpenDoor();
            return true;
        }

        private void OpenDoor()
        {
            if (doorOpened)
            {
                return;
            }

            if (doorToRotate == null)
            {
                doorOpened = true;
                return;
            }

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(OpenDoorRoutine());
        }

        private IEnumerator OpenDoorRoutine()
        {
            Quaternion start = doorToRotate.localRotation;
            Quaternion target = closedLocalRotation * Quaternion.Euler(0f, openAngle, 0f);
            float elapsed = 0f;

            while (elapsed < openSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openSeconds);
                doorToRotate.localRotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            doorToRotate.localRotation = target;
            doorOpened = true;
            openRoutine = null;
        }

        private void ApplyOpenRotationImmediate()
        {
            if (doorToRotate != null)
            {
                doorToRotate.localRotation = closedLocalRotation * Quaternion.Euler(0f, openAngle, 0f);
            }

            doorOpened = true;
        }

        private void ResolveReferences()
        {
            if (missionManager == null)
            {
                missionManager = Mission01AudioSeparatorManager.Instance;
            }

            if (keypadController == null)
            {
                keypadController = GetComponentInChildren<Mission01KeypadUIController>(true)
                    ?? GetComponentInParent<Mission01KeypadUIController>();
            }
        }

        private void ConfigureCollider()
        {
            Collider attachedCollider = GetComponent<Collider>();
            if (attachedCollider != null)
            {
                attachedCollider.isTrigger = true;
            }
        }
    }
}
