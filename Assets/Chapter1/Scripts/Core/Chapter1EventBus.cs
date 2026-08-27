using System;

namespace DormitoryMystery.Chapter1
{
    public enum GameOverRestartPolicy
    {
        ReloadScene,
        ResetChapterThenReload
    }

    public readonly struct GameOverRequest
    {
        public GameOverRequest(
            string reason,
            GameOverRestartPolicy restartPolicy)
        {
            Reason = string.IsNullOrWhiteSpace(reason)
                ? "Bạn đã thua"
                : reason;
            RestartPolicy = restartPolicy;
        }

        public string Reason { get; }
        public GameOverRestartPolicy RestartPolicy { get; }
    }

    public static class Chapter1EventBus
    {
        public static event Action<Chapter1Step> StepChanged;
        public static event Action<string> ObjectiveChanged;
        public static event Action InventoryChanged;
        public static event Action<int> NamTrustChanged;
        public static event Action<bool> PowerStateChanged;
        public static event Action<bool> PlayerHiddenChanged;
        public static event Action PlayerCaught;
        public static event Action<GameOverRequest> GameOverRequested;
        public static event Action<string> CheckpointChanged;
        public static event Action<string> NotificationRequested;
        public static event Action<string> UrgentNotificationRequested;
        public static event Action ChapterCompleted;
        public static event Action<FirstMissionState> FirstMissionStateChanged;
        public static event Action OnFirstMissionCompleted;
        public static event Action OnAllLanAudioStemsSaved;
        public static event Action OnLanVoiceRecordingListened;
        /// <summary>
        /// Raised after Henry's Task-3 warning has finished and the story is
        /// ready to hand control to the Henry combat encounter.
        /// </summary>
        public static event Action HenryCombatReady;

        public static void RaiseStepChanged(Chapter1Step step)
        {
            StepChanged?.Invoke(step);
        }

        public static void RaiseObjectiveChanged(string objective)
        {
            ObjectiveChanged?.Invoke(objective ?? string.Empty);
        }

        public static void RaiseInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }

        public static void RaiseNamTrustChanged(int namTrust)
        {
            NamTrustChanged?.Invoke(NamTrustCalculator.ClampTrust(namTrust));
        }

        public static void RaisePowerStateChanged(bool powerRestored)
        {
            PowerStateChanged?.Invoke(powerRestored);
        }

        public static void RaisePlayerHiddenChanged(bool isHidden)
        {
            PlayerHiddenChanged?.Invoke(isHidden);
        }

        public static void RaisePlayerCaught()
        {
            PlayerCaught?.Invoke();
            RaiseGameOver(
                "Henry đã bắt được bạn",
                GameOverRestartPolicy.ReloadScene);
        }

        public static void RaiseGameOver(
            string reason,
            GameOverRestartPolicy restartPolicy =
                GameOverRestartPolicy.ReloadScene)
        {
            GameOverRequested?.Invoke(
                new GameOverRequest(reason, restartPolicy));
        }

        public static void RaiseCheckpointChanged(string checkpointId)
        {
            CheckpointChanged?.Invoke(checkpointId ?? string.Empty);
        }

        public static void RaiseNotification(string message)
        {
            NotificationRequested?.Invoke(message ?? string.Empty);
        }

        public static void RaiseUrgentNotification(string message)
        {
            UrgentNotificationRequested?.Invoke(message ?? string.Empty);
        }

        public static void RaiseChapterCompleted()
        {
            ChapterCompleted?.Invoke();
        }

        public static void RaiseFirstMissionStateChanged(FirstMissionState state)
        {
            FirstMissionStateChanged?.Invoke(state);
        }

        public static void RaiseFirstMissionCompleted()
        {
            OnFirstMissionCompleted?.Invoke();
        }

        public static void RaiseAllLanAudioStemsSaved()
        {
            OnAllLanAudioStemsSaved?.Invoke();
        }

        public static void RaiseLanVoiceRecordingListened()
        {
            OnLanVoiceRecordingListened?.Invoke();
        }

        public static void RaiseHenryCombatReady()
        {
            HenryCombatReady?.Invoke();
        }
    }
}
