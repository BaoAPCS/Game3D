using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class Chapter1GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private Chapter1Manager chapter1Manager;
        [SerializeField] private Transform player;
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [SerializeField] private PlayerInputLock playerInputLock;
        [SerializeField] private CameraTarget cameraTarget;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private Chapter1InteractionController interactionController;
        [SerializeField] private FlashlightController flashlightController;
        [SerializeField] private Chapter1HUD chapter1HUD;
        [SerializeField] private Camera gameplayCamera;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void Start()
        {
            if (chapter1Manager == null)
            {
                Debug.LogWarning($"[Chapter1GameplayBootstrap] GameObject '{gameObject.name}' chưa có Chapter1Manager. Bootstrap không tạo manager mới.", this);
            }

            if (inputReader != null)
            {
                inputReader.SetGameplayInputEnabled(true);
            }

            if (cameraRig != null)
            {
                Transform targetTransform = cameraTarget != null ? cameraTarget.transform : player;
                cameraRig.SetTarget(targetTransform);
                cameraRig.SetLookEnabled(true);
                cameraRig.SetCursorLocked(true);
                cameraRig.SnapBehindTarget();

                if (gameplayCamera == null)
                {
                    gameplayCamera = cameraRig.GetComponentInChildren<Camera>(true);
                    LogFallback(nameof(gameplayCamera), gameplayCamera);
                }
            }
            else
            {
                Debug.LogWarning($"[Chapter1GameplayBootstrap] GameObject '{gameObject.name}' chưa có ThirdPersonCameraRig.", this);
            }

            if (playerInputLock == null)
            {
                Debug.LogWarning($"[Chapter1GameplayBootstrap] GameObject '{gameObject.name}' chưa có PlayerInputLock.", this);
            }
            if (playerInventory != null)
            {
                playerInventory.SetChapterManager(chapter1Manager);
            }

            if (interactionController != null)
            {
                interactionController.SetReferences(inputReader, playerInputLock, playerInventory, chapter1Manager);
                interactionController.SetGameplayCamera(gameplayCamera);
            }

            if (flashlightController != null)
            {
                flashlightController.SetReferences(inputReader, playerInputLock, playerInventory);
                flashlightController.SetGameplayCamera(gameplayCamera);
            }

            if (chapter1HUD != null)
            {
                Chapter1PlayerMotor playerMotor = player != null ? player.GetComponent<Chapter1PlayerMotor>() : null;
                PlayerStamina playerStamina = player != null ? player.GetComponent<PlayerStamina>() : null;
                chapter1HUD.Configure(chapter1Manager, interactionController, playerInputLock, playerStamina, playerMotor, playerInventory, flashlightController);
            }
        }

        private void ResolveMissingReferences()
        {
            if (chapter1Manager == null)
            {
                chapter1Manager = FindAnyObjectByType<Chapter1Manager>();
                LogFallback(nameof(chapter1Manager), chapter1Manager);
            }

            Chapter1PlayerMotor playerMotor = null;
            if (player == null)
            {
                playerMotor = FindAnyObjectByType<Chapter1PlayerMotor>();
                if (playerMotor != null)
                {
                    player = playerMotor.transform;
                    LogFallback(nameof(player), player);
                }
            }

            if (inputReader == null)
            {
                inputReader = player != null ? player.GetComponent<Chapter1InputReader>() : FindAnyObjectByType<Chapter1InputReader>();
                LogFallback(nameof(inputReader), inputReader);
            }

            if (playerInputLock == null)
            {
                playerInputLock = player != null ? player.GetComponent<PlayerInputLock>() : FindAnyObjectByType<PlayerInputLock>();
                LogFallback(nameof(playerInputLock), playerInputLock);
            }

            if (playerInventory == null)
            {
                playerInventory = player != null ? player.GetComponent<PlayerInventory>() : FindAnyObjectByType<PlayerInventory>();
                LogFallback(nameof(playerInventory), playerInventory);
            }

            if (interactionController == null)
            {
                interactionController = player != null ? player.GetComponent<Chapter1InteractionController>() : FindAnyObjectByType<Chapter1InteractionController>();
                LogFallback(nameof(interactionController), interactionController);
            }

            if (flashlightController == null)
            {
                flashlightController = player != null ? player.GetComponent<FlashlightController>() : FindAnyObjectByType<FlashlightController>();
                LogFallback(nameof(flashlightController), flashlightController);
            }

            if (cameraTarget == null)
            {
                cameraTarget = player != null ? player.GetComponentInChildren<CameraTarget>() : FindAnyObjectByType<CameraTarget>();
                LogFallback(nameof(cameraTarget), cameraTarget);
            }

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
                LogFallback(nameof(cameraRig), cameraRig);
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = cameraRig != null ? cameraRig.GetComponentInChildren<Camera>(true) : Camera.main;
                LogFallback(nameof(gameplayCamera), gameplayCamera);
            }

            if (chapter1HUD == null)
            {
                chapter1HUD = FindAnyObjectByType<Chapter1HUD>();
                LogFallback(nameof(chapter1HUD), chapter1HUD);
            }
        }

        private void LogFallback(string referenceName, Object resolvedObject)
        {
            if (resolvedObject != null)
            {
                Debug.LogWarning($"[Chapter1GameplayBootstrap] Reference '{referenceName}' chưa được liên kết trong Inspector, đã tự tìm một lần trong Awake.", this);
            }
        }
    }
}
