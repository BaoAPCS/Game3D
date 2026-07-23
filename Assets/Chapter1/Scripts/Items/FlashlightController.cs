using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chapter1InputReader))]
    [RequireComponent(typeof(PlayerInputLock))]
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Chapter1InputReader inputReader;
        [SerializeField] private PlayerInputLock inputLock;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Light flashlightLight;
        [SerializeField] private Transform flashlightPivot;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private bool autoTurnOnAfterPickup = true;
        [SerializeField] private float rotationFollowSpeed = 18f;

        private bool wasFlashlightOwned;
        private bool missingLightLogged;

        public event Action<bool> FlashlightStateChanged;

        public bool HasFlashlight => inventory != null && inventory.HasFlashlight;
        public bool IsFlashlightOn { get; private set; }

        private void Awake()
        {
            ResolveLocalReferences();
            ApplyLightState(false);
        }

        private void Start()
        {
            wasFlashlightOwned = HasFlashlight;
            if (flashlightLight == null && !missingLightLogged)
            {
                missingLightLogged = true;
                Debug.LogWarning($"[FlashlightController] GameObject '{gameObject.name}' chưa có Light đèn pin.", this);
            }
        }

        private void OnEnable()
        {
            ResolveLocalReferences();
            if (inputReader != null)
            {
                inputReader.ToggleFlashlightPressed += HandleToggleFlashlightPressed;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ToggleFlashlightPressed -= HandleToggleFlashlightPressed;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -= HandleInventoryChanged;
            }
        }

        private void LateUpdate()
        {
            if (gameplayCamera == null || flashlightPivot == null)
            {
                return;
            }

            flashlightPivot.rotation = Quaternion.Slerp(
                flashlightPivot.rotation,
                gameplayCamera.transform.rotation,
                rotationFollowSpeed * Time.unscaledDeltaTime);
        }

        private void OnValidate()
        {
            rotationFollowSpeed = Mathf.Max(0f, rotationFollowSpeed);
        }

        public void SetGameplayCamera(Camera camera)
        {
            gameplayCamera = camera;
        }

        public void SetReferences(Chapter1InputReader reader, PlayerInputLock lockReference, PlayerInventory playerInventory)
        {
            if (inputReader != null)
            {
                inputReader.ToggleFlashlightPressed -= HandleToggleFlashlightPressed;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -= HandleInventoryChanged;
            }

            inputReader = reader;
            inputLock = lockReference;
            inventory = playerInventory;

            if (isActiveAndEnabled && inputReader != null)
            {
                inputReader.ToggleFlashlightPressed += HandleToggleFlashlightPressed;
            }

            if (isActiveAndEnabled && inventory != null)
            {
                inventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        public void SetFlashlightLight(Light lightReference, Transform pivot)
        {
            flashlightLight = lightReference;
            flashlightPivot = pivot;
            ApplyLightState(IsFlashlightOn && HasFlashlight);
        }

        public void SetFlashlightOn(bool enabled)
        {
            bool shouldEnable = enabled && HasFlashlight;
            if (IsFlashlightOn == shouldEnable)
            {
                ApplyLightState(shouldEnable);
                return;
            }

            IsFlashlightOn = shouldEnable;
            ApplyLightState(IsFlashlightOn);
            FlashlightStateChanged?.Invoke(IsFlashlightOn);
        }

        private void ResolveLocalReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Chapter1InputReader>();
            }

            if (inputLock == null)
            {
                inputLock = GetComponent<PlayerInputLock>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void HandleToggleFlashlightPressed()
        {
            if (inputLock != null && inputLock.IsLocked)
            {
                return;
            }

            if (!HasFlashlight)
            {
                SetFlashlightOn(false);
                Chapter1EventBus.RaiseNotification("Bạn chưa có đèn pin.");
                return;
            }

            SetFlashlightOn(!IsFlashlightOn);
        }

        private void HandleInventoryChanged()
        {
            bool hasFlashlight = HasFlashlight;
            if (hasFlashlight && !wasFlashlightOwned && autoTurnOnAfterPickup)
            {
                SetFlashlightOn(true);
            }
            else if (!hasFlashlight)
            {
                SetFlashlightOn(false);
            }

            wasFlashlightOwned = hasFlashlight;
        }

        private void ApplyLightState(bool enabled)
        {
            if (flashlightLight == null)
            {
                return;
            }

            flashlightLight.enabled = enabled;
        }
    }
}
