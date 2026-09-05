using System;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Menu
{
    public static class GameSessionFlow
    {
        public const string MenuScenePath = "Assets/Menu/MenuScene.unity";
        public const string Chapter1ScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        public const string Chapter2ScenePath = "Assets/Chapter2/Scenes/Police_Station.unity";
        public const string Chapter3ScenePath = "Assets/Chapter3/Scene/Abandoned Hospital.unity";
        public const float EndingMenuDelay = 1f;

        public static bool IsLoading { get; private set; }
        public static bool CanContinue =>
            !IsLoading && new GameProgressStore().TryGetContinueScene(out _);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsLoading = false;
        }

        public static bool TryStartNewGame(out string error)
        {
            if (!RequireScene(MenuScenePath, out error)) return false;
            return TryLoadScene(Chapter1ScenePath, true, out error);
        }

        public static bool TryContinueGame(out string error)
        {
            if (!RequireScene(MenuScenePath, out error)) return false;
            if (!new GameProgressStore().TryGetContinueScene(out string target))
            {
                error = "Chưa có dữ liệu hợp lệ để tiếp tục. Hãy chọn Bắt đầu.";
                return false;
            }
            return TryLoadScene(target, false, out error);
        }

        public static bool TryReturnToMenuAfterCompletion(out string error)
        {
            if (!RequireScene(Chapter3ScenePath, out error)) return false;
            return TryLoadScene(MenuScenePath, true, out error);
        }

        private static bool RequireScene(string expected, out string error)
        {
            error = null;
            if (!Application.isPlaying || SceneManager.GetActiveScene().path != expected)
            {
                error = "Thao tác này không khả dụng trong scene hiện tại.";
                return false;
            }
            return true;
        }

        private static bool TryLoadScene(string target, bool resetProgress, out string error)
        {
            error = null;
            if (IsLoading)
            {
                error = "Đang tải màn chơi...";
                return false;
            }
            // Check before resetting: a missing Build Settings entry must not erase a save.
            if (!CanLoadScene(target))
            {
                error = "Không tìm thấy scene trong Build Settings: " + target;
                return false;
            }
            if (resetProgress && !new GameProgressStore().TryResetProgress(out error))
                return false;

            try
            {
                IsLoading = true;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                if (target == MenuScenePath) RemoveOldTransitionOverlays();
                AsyncOperation operation = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Scene load did not start.");
                operation.completed += _ => IsLoading = false;
                return true;
            }
            catch (Exception exception)
            {
                IsLoading = false;
                error = "Không thể tải màn chơi. Vui lòng thử lại.";
                Debug.LogError($"[GameSessionFlow] {exception.Message}");
                return false;
            }
        }

        private static bool CanLoadScene(string target)
        {
            if (Application.CanStreamedLevelBeLoaded(target))
            {
                return true;
            }

            for (int i = 0;
                 i < SceneManager.sceneCountInBuildSettings;
                 i++)
            {
                if (string.Equals(
                        SceneUtility.GetScenePathByBuildIndex(i),
                        target,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveOldTransitionOverlays()
        {
            foreach (ChapterSceneTransitionController transition in
                     UnityEngine.Object.FindObjectsByType<ChapterSceneTransitionController>(FindObjectsInactive.Include))
            {
                transition.StopAllCoroutines();
                UnityEngine.Object.Destroy(transition.gameObject);
            }
            foreach (Chapter2SceneTransitionController transition in
                     UnityEngine.Object.FindObjectsByType<Chapter2SceneTransitionController>(FindObjectsInactive.Include))
            {
                transition.StopAllCoroutines();
                UnityEngine.Object.Destroy(transition.gameObject);
            }
        }
    }
}
