using System.Collections;
using DormitoryMystery.Chapter2;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Owns the Chapter 1 -> Chapter 2 fade and routes a completed test save
    /// directly to the police station on the next Play session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChapterSceneTransitionController : MonoBehaviour
    {
        public const string Chapter1SceneName = "Chapter1_Dormitory";
        public const string Chapter2SceneName = "Police_Station";

        private const float FadeDuration = 0.65f;
        private const float CompletionMessageDelay = 0.8f;
        private const int OverlaySortingOrder = 32767;

        private static ChapterSceneTransitionController instance;

        private CanvasGroup overlay;
        private Coroutine transitionRoutine;
        private Coroutine startupRouteRoutine;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            HandleSceneLoaded(
                SceneManager.GetActiveScene(),
                LoadSceneMode.Single);
        }

        public static bool BeginChapter2Transition()
        {
            ChapterSceneTransitionController controller =
                GetOrCreateController();
            return controller != null &&
                   controller.RequestTransition(
                       false,
                       CompletionMessageDelay);
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != Chapter1SceneName)
            {
                return;
            }

            ChapterSceneTransitionController controller =
                GetOrCreateController();
            controller?.ScheduleCompletedSaveRoute();
        }

        private static ChapterSceneTransitionController
            GetOrCreateController()
        {
            if (instance != null)
            {
                return instance;
            }

            ChapterSceneTransitionController existing =
                FindAnyObjectByType<ChapterSceneTransitionController>(
                    FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject owner = new GameObject(
                "ChapterSceneTransitionController");
            return owner.AddComponent<
                ChapterSceneTransitionController>();
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

        private void ScheduleCompletedSaveRoute()
        {
            if (transitionRoutine != null)
            {
                return;
            }

            if (HasCompletedChapterSave())
            {
                RequestTransition(true, 0f);
                return;
            }

            if (startupRouteRoutine == null)
            {
                startupRouteRoutine = StartCoroutine(
                    RecheckCompletedSaveNextFrame());
            }
        }

        private IEnumerator RecheckCompletedSaveNextFrame()
        {
            yield return null;
            startupRouteRoutine = null;
            if (SceneManager.GetActiveScene().name ==
                    Chapter1SceneName &&
                HasCompletedChapterSave())
            {
                RequestTransition(true, 0f);
            }
        }

        private static bool HasCompletedChapterSave()
        {
            Chapter1Manager manager = Chapter1Manager.Instance;
            return manager != null &&
                   manager.CurrentData.ChapterCompleted &&
                   manager.CurrentData.Mission03PoliceArrestCompleted;
        }

        private bool RequestTransition(
            bool startBlack,
            float delay)
        {
            if (transitionRoutine != null)
            {
                return true;
            }

            if (SceneManager.GetActiveScene().name == Chapter2SceneName)
            {
                return true;
            }

            if (!Application.CanStreamedLevelBeLoaded(Chapter2SceneName))
            {
                Debug.LogError(
                    $"[ChapterTransition] Scene '{Chapter2SceneName}' " +
                    "chưa được thêm vào Build Settings.",
                    this);
                return false;
            }

            EnsureOverlay();
            overlay.alpha = startBlack ? 1f : 0f;
            overlay.blocksRaycasts = true;
            transitionRoutine = StartCoroutine(
                TransitionRoutine(startBlack, Mathf.Max(0f, delay)));
            return true;
        }

        private IEnumerator TransitionRoutine(
            bool startBlack,
            float delay)
        {
            if (delay > 0f)
            {
                float elapsedDelay = 0f;
                while (elapsedDelay < delay)
                {
                    elapsedDelay += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (!startBlack)
            {
                yield return Fade(0f, 1f);
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                Chapter2SceneName,
                LoadSceneMode.Single);
            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[ChapterTransition] Không thể load scene " +
                    $"'{Chapter2SceneName}'.",
                    this);
                yield return Fade(1f, 0f);
                transitionRoutine = null;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            // Let Chapter 2's runtime bootstrap restore save-backed items
            // while the screen is still fully black.
            yield return null;

            Chapter2JailIntroController jailIntro =
                Chapter2JailIntroController.PrepareForTransition(
                    SceneManager.GetActiveScene());
            yield return Fade(1f, 0f);

            if (jailIntro != null && jailIntro.IsPrepared)
            {
                yield return jailIntro.PlayShot();
                yield return Fade(0f, 1f);
                jailIntro.CompleteIntro();

                // Keep one fully black frame between the cinematic camera
                // and the restored third-person gameplay camera.
                yield return null;
                yield return Fade(1f, 0f);
            }

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
                "ChapterTransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject imageObject = new GameObject(
                "BlackFade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform imageRect =
                imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
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
