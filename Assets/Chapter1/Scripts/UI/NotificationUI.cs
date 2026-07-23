using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class NotificationUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float displaySeconds = 2f;
        [SerializeField] private float fadeDuration = 0.2f;

        private readonly Queue<string> messageQueue = new Queue<string>();
        private Coroutine queueRoutine;

        private void Awake()
        {
            ResolveReferences();
            SetVisible(false);
        }

        private void OnEnable()
        {
            Chapter1EventBus.NotificationRequested += ShowMessage;
        }

        private void OnDisable()
        {
            Chapter1EventBus.NotificationRequested -= ShowMessage;
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            messageQueue.Enqueue(message);
            if (queueRoutine == null && isActiveAndEnabled)
            {
                queueRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (notificationText == null)
            {
                notificationText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                if (notificationText != null)
                {
                    notificationText.text = message;
                }

                yield return FadeTo(1f);
                yield return new WaitForSecondsRealtime(displaySeconds);
                yield return FadeTo(0f);
            }

            queueRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
