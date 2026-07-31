using NavKeypad;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class RoomDoorKeypadController : MonoBehaviour
    {
        private const string InputLockReason = "RoomDoorKeypad";
        private const string InsideMarkerName = "room_nam_in";
        private const string OutsideMarkerName = "room_nam_out";
        private const string KeypadCameraName = "Keypad_camera";
        private const string DoorHingeName = "Door_Room_Nam_Hinge";
        private const string RoomDoorName = "RoomDoor_Dung";

        [Header("Keypad")]
        [SerializeField] private Keypad keypad;
        [SerializeField] private Camera keypadCamera;
        [SerializeField, Min(0.1f)] private float maximumClickDistance = 10f;

        [Header("Door Sides")]
        [SerializeField] private Transform insideMarker;
        [SerializeField] private Transform outsideMarker;
        [SerializeField] private Transform doorPlane;

        private readonly RaycastHit[] keypadHitBuffer = new RaycastHit[32];
        private DoorInteractable activeDoor;
        private Vector3 pendingPlayerPosition;
        private PlayerInputLock activeInputLock;
        private PlayerVisualController activePlayerVisual;
        private ThirdPersonCameraRig activeCameraRig;
        private Camera gameplayCamera;
        private AudioListener gameplayAudioListener;
        private AudioListener keypadAudioListener;
        private bool gameplayCameraWasEnabled;
        private bool gameplayAudioListenerWasEnabled;
        private bool playerVisualWasVisible;
        private bool subscribedToKeypad;
        private bool missingSideMarkersLogged;

        public bool IsInKeypadMode => activeDoor != null;

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
            EnsureControllerInstalled(scene);
        }

        private static void EnsureControllerInstalled(Scene scene)
        {
            Transform[] sceneTransforms =
                FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (Transform candidate in sceneTransforms)
            {
                if (candidate.gameObject.scene != scene ||
                    candidate.name != RoomDoorName ||
                    candidate.GetComponent<RoomDoorKeypadController>() != null ||
                    candidate.GetComponentInChildren<Keypad>(true) == null)
                {
                    continue;
                }

                candidate.gameObject.AddComponent<RoomDoorKeypadController>();
            }
        }

        private void Awake()
        {
            ResolveReferences();
            SetKeypadCameraActive(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToKeypad();
        }

        private void OnDisable()
        {
            UnsubscribeFromKeypad();
            ExitKeypadMode();
        }

        private void Update()
        {
            if (!IsInKeypadMode ||
                keypadCamera == null ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Ray pointerRay = keypadCamera.ScreenPointToRay(pointerPosition);
            int hitCount = Physics.RaycastNonAlloc(
                pointerRay,
                keypadHitBuffer,
                maximumClickDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            if (hitCount <= 0)
            {
                return;
            }

            KeypadButton nearestButton = null;
            float nearestButtonDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = keypadHitBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                KeypadButton candidate =
                    hitCollider.GetComponentInParent<KeypadButton>();
                if (candidate == null ||
                    keypad == null ||
                    !candidate.transform.IsChildOf(keypad.transform) ||
                    keypadHitBuffer[i].distance >= nearestButtonDistance)
                {
                    continue;
                }

                nearestButton = candidate;
                nearestButtonDistance = keypadHitBuffer[i].distance;
            }

            nearestButton?.PressButton();
        }

        public bool RequiresPassword(Vector3 playerPosition)
        {
            ResolveReferences();
            if (insideMarker == null ||
                outsideMarker == null ||
                doorPlane == null)
            {
                if (!missingSideMarkersLogged)
                {
                    missingSideMarkersLogged = true;
                    Debug.LogWarning(
                        $"[{nameof(RoomDoorKeypadController)}] " +
                        $"'{gameObject.name}' thiếu mốc '{InsideMarkerName}' hoặc " +
                        $"'{OutsideMarkerName}', hoặc thiếu '{DoorHingeName}'. " +
                        "Cửa sẽ yêu cầu mật khẩu để tránh bỏ qua khóa ngoài.",
                        this);
                }

                return true;
            }

            Vector3 planarPlayerPosition = playerPosition;
            Vector3 planarInsidePosition = insideMarker.position;
            Vector3 planarOutsidePosition = outsideMarker.position;
            Vector3 planarDoorPosition = doorPlane.position;
            planarPlayerPosition.y = 0f;
            planarInsidePosition.y = 0f;
            planarOutsidePosition.y = 0f;
            planarDoorPosition.y = 0f;

            Vector3 outsideToInside =
                planarInsidePosition - planarOutsidePosition;
            if (outsideToInside.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            return Vector3.Dot(
                       planarPlayerPosition - planarDoorPosition,
                       outsideToInside) <= 0f;
        }

        public InteractionResult BeginKeypadEntry(
            DoorInteractable door,
            InteractionContext context)
        {
            if (IsInKeypadMode)
            {
                return InteractionResult.Ignored();
            }

            ResolveReferences();
            if (door == null || keypad == null || keypadCamera == null)
            {
                return InteractionResult.Failed(
                    "Keypad hoặc camera keypad chưa được liên kết.");
            }

            if (context.PlayerObject == null ||
                context.PlayerTransform == null)
            {
                return InteractionResult.Failed(
                    "Không xác định được người chơi để mở keypad.");
            }

            PlayerInputLock inputLock =
                context.PlayerObject.GetComponent<PlayerInputLock>();
            Camera resolvedGameplayCamera =
                context.InteractionController != null
                    ? context.InteractionController.GameplayCamera
                    : Camera.main;

            if (inputLock == null || resolvedGameplayCamera == null)
            {
                return InteractionResult.Failed(
                    "Thiếu khóa điều khiển hoặc camera góc nhìn thứ ba.");
            }

            activeDoor = door;
            pendingPlayerPosition = context.PlayerTransform.position;
            activeInputLock = inputLock;
            activePlayerVisual =
                context.PlayerObject.GetComponent<PlayerVisualController>();
            gameplayCamera = resolvedGameplayCamera;
            activeCameraRig =
                gameplayCamera.GetComponentInParent<ThirdPersonCameraRig>();
            gameplayAudioListener =
                gameplayCamera.GetComponent<AudioListener>();

            gameplayCameraWasEnabled = gameplayCamera.enabled;
            gameplayAudioListenerWasEnabled =
                gameplayAudioListener != null &&
                gameplayAudioListener.enabled;
            playerVisualWasVisible =
                activePlayerVisual == null ||
                activePlayerVisual.IsVisible;

            keypad.ResetForNewAttempt();
            activeInputLock.Lock(InputLockReason);
            activePlayerVisual?.SetVisible(false);

            if (activeCameraRig != null)
            {
                activeCameraRig.SetLookEnabled(false);
                activeCameraRig.SetCursorLocked(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled = false;
            }

            gameplayCamera.enabled = false;
            SetKeypadCameraActive(true);
            return InteractionResult.Succeeded();
        }

        private void HandleAccessGranted()
        {
            if (!IsInKeypadMode)
            {
                return;
            }

            DoorInteractable door = activeDoor;
            Vector3 playerPosition = pendingPlayerPosition;
            ExitKeypadMode();

            InteractionResult result =
                door.OpenAfterKeypad(playerPosition);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Chapter1EventBus.RaiseNotification(result.Message);
            }
        }

        private void ExitKeypadMode()
        {
            if (!IsInKeypadMode)
            {
                SetKeypadCameraActive(false);
                return;
            }

            SetKeypadCameraActive(false);

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
            }

            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled =
                    gameplayAudioListenerWasEnabled;
            }

            if (activeCameraRig != null)
            {
                activeCameraRig.SetLookEnabled(true);
                activeCameraRig.SetCursorLocked(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (activeInputLock != null)
            {
                activeInputLock.Unlock(InputLockReason);
            }

            if (activePlayerVisual != null)
            {
                activePlayerVisual.SetVisible(playerVisualWasVisible);
            }

            activeDoor = null;
            activeInputLock = null;
            activePlayerVisual = null;
            activeCameraRig = null;
            gameplayCamera = null;
            gameplayAudioListener = null;
        }

        private void ResolveReferences()
        {
            if (keypad == null)
            {
                keypad = GetComponentInChildren<Keypad>(true);
            }

            if (keypadCamera == null)
            {
                Transform cameraTransform = transform.Find(KeypadCameraName);
                if (cameraTransform != null)
                {
                    keypadCamera = cameraTransform.GetComponent<Camera>();
                }
            }

            if (insideMarker == null)
            {
                insideMarker = transform.Find(InsideMarkerName);
            }

            if (outsideMarker == null)
            {
                outsideMarker = transform.Find(OutsideMarkerName);
            }

            if (doorPlane == null)
            {
                doorPlane = transform.Find(DoorHingeName);
            }

            if (keypadCamera != null && keypadAudioListener == null)
            {
                keypadAudioListener =
                    keypadCamera.GetComponent<AudioListener>();
            }
        }

        private void SubscribeToKeypad()
        {
            if (subscribedToKeypad || keypad == null)
            {
                return;
            }

            keypad.OnAccessGranted.AddListener(HandleAccessGranted);
            subscribedToKeypad = true;
        }

        private void UnsubscribeFromKeypad()
        {
            if (!subscribedToKeypad || keypad == null)
            {
                return;
            }

            keypad.OnAccessGranted.RemoveListener(HandleAccessGranted);
            subscribedToKeypad = false;
        }

        private void SetKeypadCameraActive(bool active)
        {
            if (keypadCamera != null)
            {
                keypadCamera.enabled = active;
            }

            if (keypadAudioListener != null)
            {
                keypadAudioListener.enabled = active;
            }
        }

        private void OnValidate()
        {
            maximumClickDistance = Mathf.Max(0.1f, maximumClickDistance);
            ResolveReferences();
        }
    }
}
