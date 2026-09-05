using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2SceneTransitionController : MonoBehaviour
    {
        public const string Chapter3SceneName = "Abandoned Hospital";

        private const float FadeDuration = 0.65f;
        private const int OverlaySortingOrder = 32767;

        private static Chapter2SceneTransitionController instance;
        private CanvasGroup overlay;
        private Coroutine transitionRoutine;

        public bool IsTransitioning => transitionRoutine != null;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static bool BeginChapter3Transition(float delay = 1.2f)
        {
            Chapter2SceneTransitionController controller =
                GetOrCreateController();
            return controller != null &&
                   controller.RequestTransition(Mathf.Max(0f, delay));
        }

        private static Chapter2SceneTransitionController
            GetOrCreateController()
        {
            if (instance != null)
            {
                return instance;
            }

            Chapter2SceneTransitionController existing =
                FindAnyObjectByType<Chapter2SceneTransitionController>(
                    FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject owner = new GameObject(
                "Chapter2SceneTransitionController");
            return owner.AddComponent<Chapter2SceneTransitionController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private bool RequestTransition(float delay)
        {
            if (transitionRoutine != null ||
                SceneManager.GetActiveScene().name == Chapter3SceneName)
            {
                return true;
            }

            if (!Application.CanStreamedLevelBeLoaded(Chapter3SceneName))
            {
                Debug.LogError(
                    $"[Chapter2Transition] Scene '{Chapter3SceneName}' chưa được thêm vào Build Settings.",
                    this);
                return false;
            }

            EnsureOverlay();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = true;
            transitionRoutine = StartCoroutine(
                TransitionRoutine(delay));
            return true;
        }

        private IEnumerator TransitionRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            yield return Fade(0f, 1f);
            AsyncOperation load = SceneManager.LoadSceneAsync(
                Chapter3SceneName,
                LoadSceneMode.Single);
            if (load == null)
            {
                Debug.LogError(
                    $"[Chapter2Transition] Không thể load scene '{Chapter3SceneName}'.",
                    this);
                yield return Fade(1f, 0f);
                transitionRoutine = null;
                yield break;
            }

            while (!load.isDone)
            {
                yield return null;
            }

            // Give Chapter 3's carry-over bootstrap one frame to restore the
            // inventory and phone while the screen is still black.
            yield return null;
            yield return Fade(1f, 0f);
            transitionRoutine = null;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to)
        {
            EnsureOverlay();
            overlay.alpha = from;
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / FadeDuration));
                yield return null;
            }

            overlay.alpha = to;
        }

        private void EnsureOverlay()
        {
            if (overlay != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "Chapter2TransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject imageObject = new GameObject(
                "BlackFade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            overlay = canvasObject.GetComponent<CanvasGroup>();
            overlay.interactable = true;
            overlay.blocksRaycasts = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
