using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DormitoryMystery.Chapter1
{
    public sealed class PlayerDebugOverlay : MonoBehaviour
    {
        private static readonly FieldInfo InteractActionReferenceField = typeof(Chapter1InputReader).GetField("interactActionReference", BindingFlags.Instance | BindingFlags.NonPublic);

        [SerializeField] private bool showOverlay = true;
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private Chapter1PlayerMotor playerMotor;
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private ThirdPersonCameraRig cameraRig;
        [SerializeField] private Chapter1InteractionController interactionController;
        [SerializeField] private Camera gameplayCamera;

        private GUIStyle labelStyle;

        private void Start()
        {
            ResolveMissingReferences();
        }

        private void OnGUI()
        {
            if (!showOverlay)
            {
                return;
            }

            EnsureStyle();

            GUI.Box(new Rect(12f, 12f, 360f, 292f), "Debug Player Chương 1");
            DrawLabel(32f, $"Move input: {GetMoveInputText()}");
            DrawLabel(52f, $"Tốc độ hiện tại: {GetCurrentSpeedText()}");
            DrawLabel(72f, $"Đang chạy: {GetBoolText(playerMotor != null && playerMotor.IsSprinting)}");
            DrawLabel(92f, $"Đang cúi: {GetBoolText(playerMotor != null && playerMotor.IsCrouching)}");
            DrawLabel(112f, $"Thể lực: {GetStaminaText()}");
            DrawLabel(132f, $"Input locked: {GetBoolText(inputLock != null && inputLock.IsLocked)}");
            DrawLabel(152f, $"Camera yaw/pitch: {GetCameraText()}");
            DrawLabel(172f, $"Camera assigned: {GetBoolText(GetGameplayCameraAssigned())}");
            DrawLabel(192f, $"Interaction mask: {GetInteractionMaskText()}");
            DrawLabel(212f, $"Current target: {GetSafeDebugText(interactionController != null ? interactionController.CurrentTargetName : null)}");
            DrawLabel(232f, $"Nearest candidate: {GetLastHitText()}");
            DrawLabel(252f, $"Candidate distance: {GetHitDistanceText()}");
            DrawLabel(272f, $"Interact action enabled: {GetBoolText(IsInteractActionEnabled())}");
        }

        private void ResolveMissingReferences()
        {
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<Chapter1InputReader>();
            }

            if (playerMotor == null)
            {
                playerMotor = FindAnyObjectByType<Chapter1PlayerMotor>();
            }

            if (playerStamina == null)
            {
                playerStamina = FindAnyObjectByType<PlayerStamina>();
            }

            if (inputLock == null)
            {
                inputLock = FindAnyObjectByType<PlayerInputLock>();
            }

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
            }

            if (interactionController == null)
            {
                interactionController = FindAnyObjectByType<Chapter1InteractionController>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = interactionController != null && interactionController.GameplayCamera != null
                    ? interactionController.GameplayCamera
                    : Camera.main;
            }
        }

        private void EnsureStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 13
            };
        }

        private void DrawLabel(float top, string text)
        {
            GUI.Label(new Rect(24f, top, 336f, 20f), text, labelStyle);
        }

        private string GetMoveInputText()
        {
            if (inputReader == null)
            {
                return "N/A";
            }

            Vector2 input = inputReader.MoveInput;
            return $"{input.x:0.00}, {input.y:0.00}";
        }

        private string GetCurrentSpeedText()
        {
            return playerMotor != null ? playerMotor.CurrentSpeed.ToString("0.00") : "N/A";
        }

        private string GetStaminaText()
        {
            if (playerStamina == null)
            {
                return "N/A";
            }

            return $"{playerStamina.CurrentStamina:0}/{playerStamina.MaxStamina:0}";
        }

        private string GetCameraText()
        {
            if (cameraRig == null)
            {
                return "N/A";
            }

            return $"{cameraRig.Yaw:0.0} / {cameraRig.Pitch:0.0}";
        }

        private bool GetGameplayCameraAssigned()
        {
            return gameplayCamera != null || interactionController != null && interactionController.HasGameplayCamera;
        }

        private string GetInteractionMaskText()
        {
            if (interactionController == null)
            {
                return "N/A";
            }

            int value = interactionController.InteractionMaskValue;
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            bool hasInteractable = interactableLayer >= 0 && (value & (1 << interactableLayer)) != 0;
            return $"{value} / Interactable: {GetBoolText(hasInteractable)}";
        }

        private string GetLastHitText()
        {
            if (interactionController == null)
            {
                return "N/A";
            }

            return GetSafeDebugText(interactionController.LastHitName);
        }

        private string GetHitDistanceText()
        {
            if (interactionController == null)
            {
                return "N/A";
            }

            return interactionController.LastHitDistance > 0f ? interactionController.LastHitDistance.ToString("0.00") : "N/A";
        }

        private bool IsInteractActionEnabled()
        {
            if (inputReader == null || InteractActionReferenceField == null)
            {
                return false;
            }

            InputActionReference actionReference = InteractActionReferenceField.GetValue(inputReader) as InputActionReference;
            return actionReference != null && actionReference.action != null && actionReference.action.enabled;
        }

        private static string GetSafeDebugText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }

        private string GetBoolText(bool value)
        {
            return value ? "Có" : "Không";
        }
    }
}
