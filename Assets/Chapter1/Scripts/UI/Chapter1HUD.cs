using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class Chapter1HUD : MonoBehaviour
    {
        [SerializeField] private InteractionPromptUI interactionPromptUI;
        [SerializeField] private StaminaHUD staminaHUD;
        [SerializeField] private InventoryHUD inventoryHUD;
        [SerializeField] private NotificationUI notificationUI;
        [SerializeField] private ObjectiveHUD objectiveHUD;

        private void Awake()
        {
            ValidateReferences();
        }

        public void Configure(
            Chapter1Manager manager,
            Chapter1InteractionController interactionController,
            PlayerInputLock inputLock,
            PlayerStamina stamina,
            Chapter1PlayerMotor playerMotor,
            PlayerInventory inventory,
            FlashlightController flashlightController)
        {
            if (interactionPromptUI != null)
            {
                interactionPromptUI.Bind(interactionController, inputLock);
            }

            if (staminaHUD != null)
            {
                staminaHUD.Bind(stamina, playerMotor);
            }

            if (inventoryHUD != null)
            {
                inventoryHUD.Bind(inventory, flashlightController);
            }

            if (objectiveHUD != null)
            {
                objectiveHUD.Bind(manager);
            }
        }

        private void ValidateReferences()
        {
            if (interactionPromptUI == null)
            {
                interactionPromptUI = GetComponentInChildren<InteractionPromptUI>(true);
            }

            if (staminaHUD == null)
            {
                staminaHUD = GetComponentInChildren<StaminaHUD>(true);
            }

            if (inventoryHUD == null)
            {
                inventoryHUD = GetComponentInChildren<InventoryHUD>(true);
            }

            if (notificationUI == null)
            {
                notificationUI = GetComponentInChildren<NotificationUI>(true);
            }

            if (objectiveHUD == null)
            {
                objectiveHUD = GetComponentInChildren<ObjectiveHUD>(true);
            }

            if (interactionPromptUI == null || staminaHUD == null || inventoryHUD == null || notificationUI == null || objectiveHUD == null)
            {
                Debug.LogWarning($"[Chapter1HUD] GameObject '{gameObject.name}' chưa được gán đủ HUD con.", this);
            }
        }
    }
}
