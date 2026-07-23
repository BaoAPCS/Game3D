using System;

namespace DormitoryMystery.Chapter1
{
    public static class Chapter1EventBus
    {
        public static event Action<Chapter1Step> StepChanged;
        public static event Action<string> ObjectiveChanged;
        public static event Action InventoryChanged;
        public static event Action<int> NamTrustChanged;
        public static event Action<bool> PowerStateChanged;
        public static event Action<bool> PlayerHiddenChanged;
        public static event Action PlayerCaught;
        public static event Action<string> CheckpointChanged;
        public static event Action<string> NotificationRequested;
        public static event Action ChapterCompleted;

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
        }

        public static void RaiseCheckpointChanged(string checkpointId)
        {
            CheckpointChanged?.Invoke(checkpointId ?? string.Empty);
        }

        public static void RaiseNotification(string message)
        {
            NotificationRequested?.Invoke(message ?? string.Empty);
        }

        public static void RaiseChapterCompleted()
        {
            ChapterCompleted?.Invoke();
        }
    }
}
