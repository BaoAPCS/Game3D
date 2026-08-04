using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private PhoneUIController phoneUIController;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private List<InventorySlotUI> slots = new List<InventorySlotUI>();
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailQuantityText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip useClip;

        private InventoryItem selectedItem;
        private bool listenersBound;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public InventoryItem SelectedItem => selectedItem;

        private void Awake()
        {
            ResolveReferences();
            BindButtonListeners();
            SetOpenState(false, false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindButtonListeners();
            SubscribeInventory();
            RefreshInventory();
        }

        private void OnDisable()
        {
            UnsubscribeInventory();
        }

        public void Configure(
            InventoryController inventory,
            PhoneUIController phone,
            PlayerInputLock lockReference)
        {
            UnsubscribeInventory();
            inventoryController = inventory;
            phoneUIController = phone;
            inputLock = lockReference;
            SubscribeInventory();
            RefreshInventory();
        }

        public void SetAudio(
            AudioSource source,
            AudioClip open,
            AudioClip close,
            AudioClip select,
            AudioClip use)
        {
            audioSource = source;
            openClip = open;
            closeClip = close;
            selectClip = select;
            useClip = use;
        }

        public void OpenInventory()
        {
            ResolveReferences();
            if (isOpen)
            {
                return;
            }

            SetOpenState(true, true);
            inputLock?.AcquireInputLock(PlayerInputLock.InventoryReason);
            Chapter1UICursorLock.ApplyForOpenUi();
            selectedItem = null;
            RefreshInventory();
            PlayClip(openClip);
        }

        public void CloseInventory()
        {
            CloseInventory(true);
        }

        public void ToggleInventory()
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        public void RefreshInventory()
        {
            ResolveReferences();
            IReadOnlyList<InventoryItem> items = inventoryController != null
                ? inventoryController.Items
                : System.Array.Empty<InventoryItem>();

            int slotCount = Mathf.Max(slots.Count, 12);
            for (int i = 0; i < slotCount && i < slots.Count; i++)
            {
                InventoryItem item = i < items.Count ? items[i] : null;
                slots[i].Bind(item, HandleSlotClicked);
                slots[i].SetSelected(ReferenceEquals(item, selectedItem));
            }

            if (selectedItem != null && !ContainsItem(items, selectedItem))
            {
                selectedItem = null;
            }

            UpdateDetailPanel();
        }

        public void SelectItem(InventoryItem item)
        {
            selectedItem = item;
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetSelected(ReferenceEquals(slots[i].BoundItem, selectedItem));
            }

            UpdateDetailPanel();
            PlayClip(selectClip);
        }

        public void UseSelectedItem()
        {
            if (selectedItem == null || selectedItem.Definition == null || !selectedItem.Definition.IsUsable)
            {
                return;
            }

            PlayClip(useClip);
            if (selectedItem.Definition.Category == ItemCategory.Phone)
            {
                if (phoneUIController != null)
                {
                    phoneUIController.OpenPhone();
                }

                CloseInventory(true);
            }
        }

        private void CloseInventory(bool updateCursor)
        {
            if (!isOpen)
            {
                return;
            }

            SetOpenState(false, false);
            selectedItem = null;
            UpdateDetailPanel();
            inputLock?.ReleaseInputLock(PlayerInputLock.InventoryReason);
            if (updateCursor)
            {
                Chapter1UICursorLock.ApplyAfterClose(inputLock);
            }

            PlayClip(closeClip);
        }

        private void HandleSlotClicked(InventorySlotUI slot, InventoryItem item)
        {
            SelectItem(item);
        }

        private void UpdateDetailPanel()
        {
            ItemDefinition definition = selectedItem != null ? selectedItem.Definition : null;
            bool hasSelection = definition != null;

            if (detailIcon != null)
            {
                detailIcon.sprite = hasSelection ? definition.Icon : null;
                detailIcon.enabled = hasSelection && definition.Icon != null;
            }

            if (detailNameText != null)
            {
                detailNameText.text = hasSelection ? definition.DisplayName : string.Empty;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = hasSelection ? definition.Description : string.Empty;
            }

            if (detailQuantityText != null)
            {
                detailQuantityText.text = hasSelection && definition.IsStackable ? $"x{selectedItem.Quantity}" : string.Empty;
            }

            if (useButton != null)
            {
                useButton.interactable = hasSelection && definition.IsUsable;
            }
        }

        private void SetOpenState(bool open, bool makeInteractable)
        {
            isOpen = open;
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (root.activeSelf != open)
            {
                root.SetActive(open);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = open ? 1f : 0f;
                canvasGroup.interactable = makeInteractable;
                canvasGroup.blocksRaycasts = makeInteractable;
            }
        }

        private void SubscribeInventory()
        {
            if (inventoryController != null)
            {
                inventoryController.InventoryChanged -= RefreshInventory;
                inventoryController.InventoryChanged += RefreshInventory;
            }
        }

        private void UnsubscribeInventory()
        {
            if (inventoryController != null)
            {
                inventoryController.InventoryChanged -= RefreshInventory;
            }
        }

        private void BindButtonListeners()
        {
            if (listenersBound)
            {
                return;
            }

            listenersBound = true;
            if (useButton != null)
            {
                useButton.onClick.RemoveListener(UseSelectedItem);
                useButton.onClick.AddListener(UseSelectedItem);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseInventory);
                closeButton.onClick.AddListener(CloseInventory);
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (inventoryController == null)
            {
                inventoryController = FindAnyObjectByType<InventoryController>();
            }

            if (inputLock == null)
            {
                inputLock = FindAnyObjectByType<PlayerInputLock>();
            }

            if (phoneUIController == null)
            {
                phoneUIController = FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            }

            if (slots.Count == 0)
            {
                slots.AddRange(GetComponentsInChildren<InventorySlotUI>(true));
            }

            if (detailIcon == null)
            {
                detailIcon = FindChildComponent<Image>("DetailIcon", "LargeIcon", "ItemIcon");
            }

            if (detailNameText == null)
            {
                detailNameText = FindChildComponent<TextMeshProUGUI>("DetailName", "ItemName");
            }

            if (detailDescriptionText == null)
            {
                detailDescriptionText = FindChildComponent<TextMeshProUGUI>("DetailDescription", "Description");
            }

            if (detailQuantityText == null)
            {
                detailQuantityText = FindChildComponent<TextMeshProUGUI>("DetailQuantity", "Quantity");
            }

            if (useButton == null)
            {
                useButton = FindChildComponent<Button>("UseButton");
            }

            if (closeButton == null)
            {
                closeButton = FindChildComponent<Button>("CloseButton");
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private T FindChildComponent<T>(params string[] names) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null
                        && string.Equals(components[componentIndex].name, names[nameIndex], System.StringComparison.Ordinal))
                    {
                        return components[componentIndex];
                    }
                }
            }

            return null;
        }

        private static bool ContainsItem(IReadOnlyList<InventoryItem> items, InventoryItem item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i], item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
