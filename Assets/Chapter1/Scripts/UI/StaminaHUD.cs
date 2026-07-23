using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    public sealed class StaminaHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private Chapter1PlayerMotor playerMotor;
        [SerializeField] private float hideDelay = 1.5f;
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine fadeRoutine;
        private float hideTimer;
        private float targetAlpha = -1f;

        private void Awake()
        {
            ResolveReferences();
            Refresh(true);
        }

        private void OnEnable()
        {
            if (stamina != null)
            {
                stamina.StaminaChanged += HandleStaminaChanged;
                stamina.Exhausted += HandleStaminaStateChanged;
                stamina.RecoveredFromExhaustion += HandleStaminaStateChanged;
            }
        }

        private void OnDisable()
        {
            if (stamina != null)
            {
                stamina.StaminaChanged -= HandleStaminaChanged;
                stamina.Exhausted -= HandleStaminaStateChanged;
                stamina.RecoveredFromExhaustion -= HandleStaminaStateChanged;
            }
        }

        private void Update()
        {
            Refresh(false);
        }

        public void Bind(PlayerStamina playerStamina, Chapter1PlayerMotor motor)
        {
            OnDisable();
            stamina = playerStamina;
            playerMotor = motor;
            OnEnable();
            Refresh(true);
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (staminaSlider == null)
            {
                staminaSlider = GetComponentInChildren<Slider>(true);
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            Refresh(false);
        }

        private void HandleStaminaStateChanged()
        {
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            ResolveReferences();
            if (staminaSlider != null)
            {
                staminaSlider.value = stamina != null ? stamina.NormalizedStamina : 1f;
            }

            bool shouldShow = stamina != null
                && (stamina.IsExhausted
                    || stamina.NormalizedStamina < 0.999f
                    || playerMotor != null && playerMotor.IsSprinting);

            if (shouldShow)
            {
                hideTimer = hideDelay;
                FadeTo(1f, immediate);
                return;
            }

            if (hideTimer > 0f)
            {
                hideTimer -= Time.unscaledDeltaTime;
                FadeTo(1f, immediate);
                return;
            }

            FadeTo(0f, immediate);
        }

        private void FadeTo(float targetAlpha, bool immediate)
        {
            if (canvasGroup == null)
            {
                return;
            }

            if (!immediate && Mathf.Approximately(this.targetAlpha, targetAlpha))
            {
                return;
            }

            this.targetAlpha = targetAlpha;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (immediate || fadeDuration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            fadeRoutine = null;
        }
    }
}
