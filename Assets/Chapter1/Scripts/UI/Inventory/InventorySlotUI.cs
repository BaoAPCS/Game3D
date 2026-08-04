using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image selectedHighlight;
        [SerializeField] private Color normalTint = new Color(0.18f, 0.18f, 0.19f, 0.92f);
        [SerializeField] private Color hoverTint = new Color(0.28f, 0.25f, 0.25f, 0.96f);
        [SerializeField] private Color selectedTint = new Color(0.42f, 0.06f, 0.06f, 0.9f);

        private InventoryItem boundItem;
        private Action<InventorySlotUI, InventoryItem> clickHandler;
        private bool selected;
        private bool hovering;

        public InventoryItem BoundItem => boundItem;

        private void Awake()
        {
            ResolveReferences();
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(InventoryItem item, Action<InventorySlotUI, InventoryItem> onClicked)
        {
            ResolveReferences();
            boundItem = item;
            clickHandler = onClicked;

            bool hasItem = item != null && item.Definition != null;
            if (iconImage != null)
            {
                iconImage.sprite = hasItem ? item.Definition.Icon : null;
                iconImage.enabled = hasItem && item.Definition.Icon != null;
            }

            if (quantityText != null)
            {
                bool showQuantity = hasItem && item.Definition.IsStackable && item.Quantity > 1;
                quantityText.gameObject.SetActive(showQuantity);
                quantityText.text = showQuantity ? item.Quantity.ToString() : string.Empty;
            }

            if (button != null)
            {
                button.interactable = hasItem;
            }

            SetSelected(false);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            ApplyVisualState();
        }

        private void HandleClicked()
        {
            if (boundItem != null && boundItem.Definition != null)
            {
                clickHandler?.Invoke(this, boundItem);
            }
        }

        private void ApplyVisualState()
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.enabled = selected;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedTint : hovering ? hoverTint : normalTint;
            }
        }

        private void ResolveReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (iconImage == null)
            {
                Transform icon = transform.Find("Icon");
                iconImage = icon != null ? icon.GetComponent<Image>() : GetComponentInChildren<Image>(true);
            }

            if (quantityText == null)
            {
                quantityText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (selectedHighlight == null)
            {
                Transform highlight = transform.Find("SelectedHighlight");
                selectedHighlight = highlight != null ? highlight.GetComponent<Image>() : null;
            }
        }
    }
}
