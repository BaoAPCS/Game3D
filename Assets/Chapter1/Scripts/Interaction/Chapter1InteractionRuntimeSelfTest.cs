using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter1
{
    public sealed class Chapter1InteractionRuntimeSelfTest : MonoBehaviour
    {
        private static readonly FieldInfo InteractActionReferenceField = typeof(Chapter1InputReader).GetField("interactActionReference", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TalkActionReferenceField = typeof(Chapter1InputReader).GetField("talkActionReference", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool showDetailedLogs = true;

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunAfterSceneStart());
            }
        }

        public void RunSelfTest()
        {
            int errors = 0;

            PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>();
            Chapter1InteractionController interactionController = FindAnyObjectByType<Chapter1InteractionController>();
            Chapter1InputReader inputReader = FindAnyObjectByType<Chapter1InputReader>();
            InteractionPromptUI promptUI = FindAnyObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            InventoryHUD inventoryHUD = FindAnyObjectByType<InventoryHUD>(FindObjectsInactive.Include);
            NotificationUI notificationUI = FindAnyObjectByType<NotificationUI>(FindObjectsInactive.Include);
            Light flashlightLight = FindFlashlightLight();

            Check(inventory != null, "PlayerInventory tồn tại.", ref errors);
            Check(interactionController != null, "InteractionController tồn tại.", ref errors);
            Check(inputReader != null, "InputReader tồn tại.", ref errors);
            Check(IsInteractActionEnabled(inputReader), "Interact action tồn tại và enabled.", ref errors);
            Check(IsTalkActionEnabled(inputReader), "Talk action tồn tại và enabled.", ref errors);
            Check(GetGameplayCamera(interactionController) != null, "Gameplay Camera tồn tại.", ref errors);
            Check(InteractionMaskHasInteractable(interactionController), "Interaction mask có Interactable.", ref errors);
            Check(promptUI != null, "UI Prompt tồn tại.", ref errors);
            Check(inventoryHUD != null, "InventoryHUD tồn tại.", ref errors);
            Check(notificationUI != null, "NotificationUI tồn tại.", ref errors);
            Check(CountInteractables(out int interactablesMissingCollider) >= 6, "Có ít nhất sáu object tương tác.", ref errors);
            Check(interactablesMissingCollider == 0, "Mỗi object tương tác có collider.", ref errors);
            Check(flashlightLight != null, "Flashlight Light tồn tại.", ref errors);

            if (errors == 0)
            {
                Debug.Log("[Chapter1 Runtime Test] PASS: Interaction, Inventory và HUD đã sẵn sàng để test thủ công.", this);
            }
        }

        private IEnumerator RunAfterSceneStart()
        {
            yield return new WaitForSeconds(0.5f);
            RunSelfTest();
        }

        private void Check(bool condition, string message, ref int errors)
        {
            if (condition)
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"[Chapter1 Runtime Test] PASS: {message}", this);
                }

                return;
            }

            errors++;
            Debug.LogError($"[Chapter1 Runtime Test] ERROR: {message}", this);
        }

        private static bool IsInteractActionEnabled(Chapter1InputReader inputReader)
        {
            if (inputReader == null || InteractActionReferenceField == null)
            {
                return false;
            }

            InputActionReference actionReference = InteractActionReferenceField.GetValue(inputReader) as InputActionReference;
            return actionReference != null && actionReference.action != null && actionReference.action.enabled;
        }

        private static bool IsTalkActionEnabled(Chapter1InputReader inputReader)
        {
            if (inputReader == null || TalkActionReferenceField == null)
            {
                return false;
            }

            InputActionReference actionReference =
                TalkActionReferenceField.GetValue(inputReader) as InputActionReference;
            return actionReference != null &&
                   actionReference.action != null &&
                   actionReference.action.enabled;
        }

        private static Camera GetGameplayCamera(Chapter1InteractionController interactionController)
        {
            if (interactionController != null && interactionController.GameplayCamera != null)
            {
                return interactionController.GameplayCamera;
            }

            return Camera.main;
        }

        private static bool InteractionMaskHasInteractable(Chapter1InteractionController interactionController)
        {
            if (interactionController == null)
            {
                return false;
            }

            int layer = LayerMask.NameToLayer("Interactable");
            return layer >= 0 && (interactionController.InteractionMaskValue & (1 << layer)) != 0;
        }

        private static int CountInteractables(out int missingColliderCount)
        {
            missingColliderCount = 0;
            int count = 0;
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IChapter1Interactable)
                {
                    continue;
                }

                count++;
                if (behaviours[i].GetComponentInChildren<Collider>(true) == null)
                {
                    missingColliderCount++;
                }
            }

            return count;
        }

        private static Light FindFlashlightLight()
        {
            FlashlightController flashlightController = FindAnyObjectByType<FlashlightController>();
            if (flashlightController != null)
            {
                Light childLight = flashlightController.GetComponentInChildren<Light>(true);
                if (childLight != null)
                {
                    return childLight;
                }
            }

            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].name == "FlashlightLight")
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
