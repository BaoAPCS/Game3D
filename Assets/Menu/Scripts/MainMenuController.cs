using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DormitoryMystery.Menu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text statusText;
        [SerializeField] private EventSystem menuEventSystem;

        private bool busy;
        private bool listenersAttached;
        public bool IsBusy => busy;

        public void Configure(Button start, Button resume, Button quit, Text status, EventSystem eventSystem)
        {
            DetachListeners();
            startButton = start;
            continueButton = resume;
            quitButton = quit;
            statusText = status;
            menuEventSystem = eventSystem;
            if (Application.isPlaying && isActiveAndEnabled)
            {
                AttachListeners();
                RefreshAvailability();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            busy = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetStatus(string.Empty);
            AttachListeners();
            RefreshAvailability();
            StartCoroutine(FocusFirstButton());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            DetachListeners();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled && !busy && Application.isPlaying)
                RefreshAvailability();
        }

        public void RefreshAvailability()
        {
            SetButtonAvailable(startButton, !busy);
            SetButtonAvailable(continueButton, !busy && GameSessionFlow.CanContinue);
            SetButtonAvailable(quitButton, !busy);
        }

        public void StartNewGame()
        {
            if (busy)
                return;

            SetBusy("Đang bắt đầu...");
            if (!GameSessionFlow.TryStartNewGame(out string error))
                ShowFailure(error);
        }

        public void ContinueGame()
        {
            if (busy)
                return;

            if (!GameSessionFlow.CanContinue)
            {
                ShowFailure("Chưa có dữ liệu đã lưu để tiếp tục.");
                return;
            }

            SetBusy("Đang tải lần lưu gần nhất...");
            if (!GameSessionFlow.TryContinueGame(out string error))
                ShowFailure(error);
        }

        public void QuitGame()
        {
            if (busy)
                return;

            SetBusy("Đang thoát...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetBusy(string message)
        {
            busy = true;
            SetStatus(message);
            RefreshAvailability();
        }

        private void ShowFailure(string error)
        {
            busy = false;
            SetStatus(string.IsNullOrWhiteSpace(error) ? "Không thể mở màn chơi. Vui lòng thử lại." : error);
            RefreshAvailability();
            StartCoroutine(FocusFirstButton());
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private IEnumerator FocusFirstButton()
        {
            yield return null;
            if (busy)
                yield break;

            // The previous chapter restores its cursor/input state while it unloads.
            // Reapply menu state after that teardown has completed.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem target = menuEventSystem != null ? menuEventSystem : EventSystem.current;
            if (startButton != null && target != null)
            {
                target.SetSelectedGameObject(null);
                target.SetSelectedGameObject(startButton.gameObject);
            }
        }

        private static void SetButtonAvailable(Button button, bool available)
        {
            if (button == null)
                return;

            button.interactable = available;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.color = available ? new Color(0.93f, 0.92f, 0.9f) : new Color(0.43f, 0.42f, 0.41f);
        }

        private void AttachListeners()
        {
            if (listenersAttached)
                return;

            if (startButton != null) startButton.onClick.AddListener(StartNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            listenersAttached = true;
        }

        private void DetachListeners()
        {
            if (startButton != null) startButton.onClick.RemoveListener(StartNewGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
            listenersAttached = false;
        }
    }
}
