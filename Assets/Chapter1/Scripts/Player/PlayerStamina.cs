using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float startingStamina = 100f;
        [SerializeField] private float sprintDrainPerSecond = 22f;
        [SerializeField] private float regenerationPerSecond = 14f;
        [SerializeField] private float regenerationDelay = 1f;
        [SerializeField] private float exhaustedRecoveryThreshold = 15f;

        private float currentStamina;
        private float timeSinceLastConsume;
        private bool isInitialized;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float NormalizedStamina => maxStamina > 0f ? currentStamina / maxStamina : 0f;
        public bool IsExhausted { get; private set; }
        public bool CanSprint => !IsExhausted && currentStamina > 0f;

        public event Action<float, float> StaminaChanged;
        public event Action Exhausted;
        public event Action RecoveredFromExhaustion;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnValidate()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            startingStamina = Mathf.Clamp(startingStamina, 0f, maxStamina);
            sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
            exhaustedRecoveryThreshold = Mathf.Clamp(exhaustedRecoveryThreshold, 0f, maxStamina);
        }

        public void ConsumeSprint(float deltaTime)
        {
            InitializeIfNeeded();

            if (deltaTime <= 0f || currentStamina <= 0f)
            {
                SetExhaustedIfNeeded();
                return;
            }

            timeSinceLastConsume = 0f;
            SetCurrentStamina(currentStamina - sprintDrainPerSecond * deltaTime);
            SetExhaustedIfNeeded();
        }

        public void TickRegeneration(float deltaTime)
        {
            InitializeIfNeeded();

            if (deltaTime <= 0f || currentStamina >= maxStamina)
            {
                return;
            }

            timeSinceLastConsume += deltaTime;
            if (timeSinceLastConsume < regenerationDelay)
            {
                return;
            }

            SetCurrentStamina(currentStamina + regenerationPerSecond * deltaTime);
            if (IsExhausted && currentStamina >= exhaustedRecoveryThreshold)
            {
                IsExhausted = false;
                RecoveredFromExhaustion?.Invoke();
            }
        }

        public void ResetStamina()
        {
            isInitialized = true;
            timeSinceLastConsume = regenerationDelay;
            IsExhausted = false;
            SetCurrentStamina(startingStamina);
        }

        public void SetStaminaForTesting(float value)
        {
            isInitialized = true;
            SetCurrentStamina(value);
            IsExhausted = Mathf.Approximately(currentStamina, 0f);
            if (!IsExhausted && currentStamina < exhaustedRecoveryThreshold)
            {
                IsExhausted = true;
            }
        }

        private void InitializeIfNeeded()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            currentStamina = Mathf.Clamp(startingStamina, 0f, maxStamina);
            timeSinceLastConsume = regenerationDelay;
            IsExhausted = Mathf.Approximately(currentStamina, 0f);
        }

        private void SetCurrentStamina(float value)
        {
            float previousValue = currentStamina;
            currentStamina = Mathf.Clamp(value, 0f, maxStamina);

            if (!Mathf.Approximately(previousValue, currentStamina))
            {
                StaminaChanged?.Invoke(currentStamina, maxStamina);
            }
        }

        private void SetExhaustedIfNeeded()
        {
            if (!IsExhausted && Mathf.Approximately(currentStamina, 0f))
            {
                IsExhausted = true;
                Exhausted?.Invoke();
            }
        }
    }
}
