using TMPro;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class InventoryHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private FlashlightController flashlightController;
        [SerializeField] private TextMeshProUGUI flashlightText;
        [SerializeField] private TextMeshProUGUI fuseText;
        [SerializeField] private TextMeshProUGUI canText;
        [SerializeField] private TextMeshProUGUI hardDriveText;
        [SerializeField] private Color ownedColor = Color.white;
        [SerializeField] private Color missingColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color flashlightOnColor = new Color(1f, 0.95f, 0.45f, 1f);

        private void OnEnable()
        {
            ResolveTextReferences();
            Subscribe();
            UpdateDisplay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(PlayerInventory playerInventory, FlashlightController controller)
        {
            Unsubscribe();
            inventory = playerInventory;
            flashlightController = controller;
            ResolveTextReferences();
            Subscribe();
            UpdateDisplay();
        }

        private void Subscribe()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += UpdateDisplay;
            }

            if (flashlightController != null)
            {
                flashlightController.FlashlightStateChanged += HandleFlashlightStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= UpdateDisplay;
            }

            if (flashlightController != null)
            {
                flashlightController.FlashlightStateChanged -= HandleFlashlightStateChanged;
            }
        }

        private void HandleFlashlightStateChanged(bool enabled)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            ResolveTextReferences();
            bool hasFlashlight = inventory != null && inventory.HasFlashlight;
            bool flashlightOn = flashlightController != null && flashlightController.IsFlashlightOn;
            bool hasFuse = inventory != null && inventory.HasFuse;
            bool hasHardDrive = inventory != null && inventory.HasHardDrive;
            int canCount = inventory != null ? inventory.ThrowableCanCount : 0;

            SetLabel(flashlightText, $"Đèn pin: {(hasFlashlight ? flashlightOn ? "Bật" : "Có" : "Chưa có")}", hasFlashlight, flashlightOn);
            SetLabel(fuseText, $"Cầu chì: {(hasFuse ? "Có" : "Chưa có")}", hasFuse, false);
            SetLabel(canText, $"Lon: x{canCount}", canCount > 0, false);
            SetLabel(hardDriveText, $"Ổ cứng: {(hasHardDrive ? "Có" : "Chưa có")}", hasHardDrive, false);
        }

        private void SetLabel(TextMeshProUGUI label, string text, bool owned, bool active)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            label.color = active ? flashlightOnColor : owned ? ownedColor : missingColor;
        }

        private void ResolveTextReferences()
        {
            flashlightText = flashlightText != null ? flashlightText : FindLabel("FlashlightText", "Flashlight");
            fuseText = fuseText != null ? fuseText : FindLabel("FuseText", "Fuse");
            canText = canText != null ? canText : FindLabel("CanText", "Can");
            hardDriveText = hardDriveText != null ? hardDriveText : FindLabel("HardDriveText", "HardDrive");
        }

        private TextMeshProUGUI FindLabel(params string[] names)
        {
            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < names.Length; i++)
            {
                for (int j = 0; j < labels.Length; j++)
                {
                    if (labels[j] != null && string.Equals(labels[j].name, names[i], System.StringComparison.Ordinal))
                    {
                        return labels[j];
                    }
                }
            }

            return null;
        }
    }
}
