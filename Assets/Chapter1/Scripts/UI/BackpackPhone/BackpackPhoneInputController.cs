using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chapter1InputReader))]
    [RequireComponent(typeof(PlayerInputLock))]
    [RequireComponent(typeof(InventoryController))]
    public sealed class BackpackPhoneInputController : MonoBehaviour
    {
        private const string BackpackCanvasName = "Chapter1BackpackPhoneCanvas";

        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private InventoryUIController inventoryUIController;
        [SerializeField] private PhoneUIController phoneUIController;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private bool createRuntimeUiIfMissing = true;

        private bool subscribed;

        public InventoryUIController InventoryUIController => inventoryUIController;
        public PhoneUIController PhoneUIController => phoneUIController;

        private void Awake()
        {
            ResolveReferences();
            EnsureUi();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureUi();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            InventoryController inventory,
            InventoryUIController inventoryUi,
            PhoneUIController phoneUi,
            Canvas canvas,
            PlayerInputLock lockReference)
        {
            inventoryController = inventory;
            inventoryUIController = inventoryUi;
            phoneUIController = phoneUi;
            targetCanvas = canvas;
            inputLock = lockReference;

            if (inventoryUIController != null)
            {
                inventoryUIController.Configure(inventoryController, phoneUIController, inputLock);
            }

            if (phoneUIController != null)
            {
                phoneUIController.Configure(inputLock);
            }
        }

        private void HandleInventoryPressed()
        {
            EnsureUi();
            if (phoneUIController != null && phoneUIController.IsOpen)
            {
                return;
            }

            inventoryUIController?.ToggleInventory();
        }

        private void HandlePausePressed()
        {
            EnsureUi();
            if (phoneUIController != null && phoneUIController.IsOpen)
            {
                phoneUIController.ClosePhone();
                return;
            }

            if (inventoryUIController != null && inventoryUIController.IsOpen)
            {
                inventoryUIController.CloseInventory();
            }
        }

        private void EnsureUi()
        {
            if (!createRuntimeUiIfMissing)
            {
                return;
            }

            ResolveReferences();
            if (!IsBackpackCanvas(targetCanvas))
            {
                targetCanvas = FindBackpackCanvas() ?? CreateRuntimeCanvas();
            }

            EnsureRuntimeEventSystem();
            MoveUiToCanvas(phoneUIController);
            MoveUiToCanvas(inventoryUIController);

            if (phoneUIController == null)
            {
                phoneUIController = BackpackPhoneRuntimeUIFactory.EnsurePhoneUI(targetCanvas, inputLock);
            }

            if (inventoryUIController == null)
            {
                inventoryUIController = BackpackPhoneRuntimeUIFactory.EnsureInventoryUI(
                    targetCanvas,
                    inventoryController,
                    phoneUIController,
                    inputLock);
            }

            if (inventoryUIController != null)
            {
                inventoryUIController.Configure(inventoryController, phoneUIController, inputLock);
            }

            if (phoneUIController != null)
            {
                phoneUIController.Configure(inputLock);
            }
        }

        private void Subscribe()
        {
            if (subscribed || inputReader == null)
            {
                return;
            }

            inputReader.InventoryPressed += HandleInventoryPressed;
            inputReader.PausePressed += HandlePausePressed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inputReader == null)
            {
                return;
            }

            inputReader.InventoryPressed -= HandleInventoryPressed;
            inputReader.PausePressed -= HandlePausePressed;
            subscribed = false;
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Chapter1InputReader>();
            }

            if (inputLock == null)
            {
                inputLock = GetComponent<PlayerInputLock>();
            }

            if (inventoryController == null)
            {
                inventoryController = GetComponent<InventoryController>();
            }

            if (inventoryUIController == null)
            {
                inventoryUIController = FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            }

            if (phoneUIController == null)
            {
                phoneUIController = FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindBackpackCanvas();
            }
        }

        private static bool IsBackpackCanvas(Canvas canvas)
        {
            return canvas != null
                && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                && string.Equals(canvas.gameObject.name, BackpackCanvasName, System.StringComparison.Ordinal);
        }

        private static Canvas FindBackpackCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (IsBackpackCanvas(canvas))
                {
                    return canvas;
                }
            }

            return null;
        }

        private Canvas CreateRuntimeCanvas()
        {
            GameObject canvasObject = new GameObject(BackpackCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            return canvas;
        }

        private void MoveUiToCanvas(Behaviour uiController)
        {
            if (uiController == null || targetCanvas == null)
            {
                return;
            }

            Transform uiTransform = uiController.transform;
            if (uiTransform.parent != targetCanvas.transform)
            {
                uiTransform.SetParent(targetCanvas.transform, false);
            }

            uiTransform.SetAsLastSibling();
            RectTransform rect = uiTransform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void EnsureRuntimeEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
