using System.Collections;
using TMPro;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private float fadeDuration = 0.15f;

        private Chapter1InteractionController interactionController;
        private PlayerInputLock inputLock;
        private Coroutine fadeRoutine;
        private string currentPrompt = string.Empty;

        private void Awake()
        {
            ResolveReferences();
            ApplyVisible(false, true);
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Bind(Chapter1InteractionController controller, PlayerInputLock lockReference)
        {
            Unbind();
            interactionController = controller;
            inputLock = lockReference;

            if (interactionController != null)
            {
                interactionController.PromptChanged += SetPrompt;
                SetPrompt(interactionController.CurrentPrompt);
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged += HandleLockStateChanged;
            }

            RefreshVisibility();
        }

        public void SetPrompt(string prompt)
        {
            currentPrompt = prompt ?? string.Empty;
            if (promptText != null)
            {
                promptText.text = currentPrompt;
            }

            RefreshVisibility();
        }

        private void Unbind()
        {
            if (interactionController != null)
            {
                interactionController.PromptChanged -= SetPrompt;
            }

            if (inputLock != null)
            {
                inputLock.LockStateChanged -= HandleLockStateChanged;
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (promptText == null)
            {
                promptText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void HandleLockStateChanged(bool locked)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool visible = !string.IsNullOrWhiteSpace(currentPrompt) && (inputLock == null || !inputLock.IsLocked);
            ApplyVisible(visible, false);
        }

        private void ApplyVisible(bool visible, bool immediate)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (immediate || fadeDuration <= 0f)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = false;
                return;
            }

            fadeRoutine = StartCoroutine(FadeTo(visible ? 1f : 0f));
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = targetAlpha > 0.5f;
            canvasGroup.blocksRaycasts = false;
            fadeRoutine = null;
        }
    }
}
