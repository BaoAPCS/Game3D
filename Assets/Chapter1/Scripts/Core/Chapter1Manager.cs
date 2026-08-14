using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class Chapter1Manager : MonoBehaviour
    {
        public static Chapter1Manager Instance { get; private set; }

        [SerializeField] private bool autoLoadOnAwake = true;
        [SerializeField] private bool autoSaveOnMilestones = true;

        private IChapter1SaveService saveService;
        private Chapter1SaveData currentData;

        public Chapter1Step CurrentStep => CurrentData.CurrentStep;
        public int NamTrust => CurrentData.NamTrust;
        public Chapter1SaveData CurrentData => currentData ??= Chapter1SaveData.CreateDefault();
        public bool IsChapterCompleted => CurrentData.ChapterCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[Chapter1Manager] Phát hiện instance trùng trên GameObject '{gameObject.name}'. Component trùng sẽ bị disable.", this);
                enabled = false;
                return;
            }

            Instance = this;
            saveService = new JsonChapter1SaveService();

            if (autoLoadOnAwake)
            {
                LoadChapter();
            }
            else
            {
                InitializeNewChapter();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitializeNewChapter()
        {
            currentData = Chapter1SaveData.CreateDefault();
            PublishCurrentState();
            SaveIfAutoSaveEnabled();
        }

        public void LoadChapter()
        {
            EnsureSaveService();
            currentData = saveService.Load();
            currentData.EnsureValidDefaults();
            PublishCurrentState();
        }

        public void SaveChapter()
        {
            EnsureSaveService();
            saveService.Save(CurrentData);
        }

        public void ResetChapter()
        {
            EnsureSaveService();
            saveService.DeleteSave();
            InitializeNewChapter();
        }

        public bool CanPerformStep(Chapter1Step step)
        {
            return !CurrentData.ChapterCompleted && CurrentData.CurrentStep == step;
        }

        public bool AdvanceTo(Chapter1Step nextStep)
        {
            Chapter1Step previousStep = CurrentData.CurrentStep;

            if ((int)nextStep < (int)previousStep)
            {
                Debug.LogWarning($"[Chapter1Manager] Không thể lùi nhiệm vụ từ '{previousStep}' về '{nextStep}' trên GameObject '{gameObject.name}'.", this);
                return false;
            }

            if (nextStep == previousStep)
            {
                return true;
            }

            bool wasChapterCompleted = CurrentData.ChapterCompleted;
            CurrentData.CurrentStep = nextStep;

            if (nextStep == Chapter1Step.ChapterCompleted)
            {
                CurrentData.ChapterCompleted = true;
            }

            CurrentData.EnsureValidDefaults();
            Chapter1EventBus.RaiseStepChanged(CurrentData.CurrentStep);
            Chapter1EventBus.RaiseObjectiveChanged(GetCurrentObjective());

            if (CurrentData.ChapterCompleted && !wasChapterCompleted)
            {
                Chapter1EventBus.RaiseChapterCompleted();
            }

            SaveIfAutoSaveEnabled();
            return true;
        }

        public void SetCheckpoint(string checkpointId)
        {
            if (string.IsNullOrWhiteSpace(checkpointId))
            {
                Debug.LogWarning($"[Chapter1Manager] Checkpoint không hợp lệ trên GameObject '{gameObject.name}'.", this);
                return;
            }

            if (CurrentData.CurrentCheckpointId == checkpointId)
            {
                return;
            }

            CurrentData.CurrentCheckpointId = checkpointId;
            Chapter1EventBus.RaiseCheckpointChanged(CurrentData.CurrentCheckpointId);
            SaveIfAutoSaveEnabled();
        }

        public void SetPowerRestored(bool powerRestored)
        {
            if (CurrentData.PowerRestored == powerRestored)
            {
                return;
            }

            CurrentData.PowerRestored = powerRestored;
            Chapter1EventBus.RaisePowerStateChanged(CurrentData.PowerRestored);
            SaveIfAutoSaveEnabled();
        }

        public void SetAudioPuzzleSolved(bool audioPuzzleSolved)
        {
            if (CurrentData.AudioPuzzleSolved == audioPuzzleSolved)
            {
                return;
            }

            CurrentData.AudioPuzzleSolved = audioPuzzleSolved;
            SaveIfAutoSaveEnabled();
        }

        public void SetCCTVChecked(bool cctvChecked)
        {
            if (CurrentData.CCTVChecked == cctvChecked)
            {
                return;
            }

            CurrentData.CCTVChecked = cctvChecked;
            SaveIfAutoSaveEnabled();
        }

        public void SetFootprintsRevealed(bool footprintsRevealed)
        {
            if (CurrentData.FootprintsRevealed == footprintsRevealed)
            {
                return;
            }

            CurrentData.FootprintsRevealed = footprintsRevealed;
            SaveIfAutoSaveEnabled();
        }

        public void SetRaincoatEnemyActivated(bool raincoatEnemyActivated)
        {
            if (CurrentData.RaincoatEnemyActivated == raincoatEnemyActivated)
            {
                return;
            }

            CurrentData.RaincoatEnemyActivated = raincoatEnemyActivated;
            SaveIfAutoSaveEnabled();
        }

        public bool ApplyHardDriveChoice(HardDriveChoice choice)
        {
            if (CurrentData.CurrentStep < Chapter1Step.MakeFinalChoice)
            {
                Debug.LogWarning($"[Chapter1Manager] Chưa đến bước lựa chọn cuối chương trên GameObject '{gameObject.name}'.", this);
                return false;
            }

            if (HasFinalChoiceBeenApplied())
            {
                Debug.LogWarning($"[Chapter1Manager] Lựa chọn cuối chương đã được áp dụng, không thể áp dụng lần nữa trên GameObject '{gameObject.name}'.", this);
                return false;
            }

            CurrentData.NamTrust = NamTrustCalculator.ApplyChoice(CurrentData.NamTrust, choice);

            switch (choice)
            {
                case HardDriveChoice.ReturnIntact:
                    break;
                case HardDriveChoice.CopyBeforeReturning:
                    CurrentData.CopiedHardDrive = true;
                    break;
                case HardDriveChoice.HideMorisTracking:
                    CurrentData.HidMorisTracking = true;
                    break;
                case HardDriveChoice.ForceNamCooperation:
                    CurrentData.ForcedNamToCooperate = true;
                    break;
                default:
                    Debug.LogWarning($"[Chapter1Manager] Lựa chọn ổ cứng không hợp lệ trên GameObject '{gameObject.name}'.", this);
                    return false;
            }

            Chapter1EventBus.RaiseNamTrustChanged(CurrentData.NamTrust);
            return AdvanceTo(Chapter1Step.EndingSequence);
        }

        public bool CompleteChapter()
        {
            if (CurrentData.ChapterCompleted)
            {
                return true;
            }

            if (CurrentData.CurrentStep < Chapter1Step.EndingSequence)
            {
                Debug.LogWarning($"[Chapter1Manager] Chưa đủ điều kiện hoàn thành Chương 1 trên GameObject '{gameObject.name}'.", this);
                return false;
            }

            return AdvanceTo(Chapter1Step.ChapterCompleted);
        }

        public static string GetObjective(Chapter1Step step)
        {
            switch (step)
            {
                case Chapter1Step.TalkToNam:
                    return "Nói chuyện với Nam.";
                case Chapter1Step.GiveLanRecording:
                    return "Đưa đoạn ghi âm của Lan cho Nam.";
                case Chapter1Step.UseNamComputer:
                    return "Sử dụng máy tính của Nam.";
                case Chapter1Step.SolveAudioPuzzle:
                    return "So sánh đoạn ghi âm của Lan với ba mẫu âm thanh.";
                case Chapter1Step.AudioPuzzleCompleted:
                    return "Trao đổi kết quả với Nam.";
                case Chapter1Step.Blackout:
                    return "Kiểm tra chuyện gì vừa xảy ra.";
                case Chapter1Step.FindFlashlight:
                    return "Tìm đèn pin trong phòng Minh.";
                case Chapter1Step.FindFuse:
                    return "Tìm cầu chì dự phòng trong phòng máy tính.";
                case Chapter1Step.RestorePower:
                    return "Lắp cầu chì vào tủ điện.";
                case Chapter1Step.CheckCCTV:
                    return "Kiểm tra camera an ninh.";
                case Chapter1Step.FollowFootprints:
                    return "Theo dấu chân ướt.";
                case Chapter1Step.EnterDarkHallway:
                    return "Đi vào hành lang mất điện.";
                case Chapter1Step.RetrieveHardDrive:
                    return "Tìm lại ổ cứng của Nam.";
                case Chapter1Step.EscapeRaincoatEnemy:
                    return "Thoát khỏi người áo mưa.";
                case Chapter1Step.ReturnToNam:
                    return "Mang ổ cứng về cho Nam.";
                case Chapter1Step.MakeFinalChoice:
                    return "Quyết định cách xử lý ổ cứng.";
                case Chapter1Step.EndingSequence:
                    return "Xem dữ liệu trong Hồ sơ số 17.";
                case Chapter1Step.ChapterCompleted:
                    return "Chương 1 hoàn thành.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns the objective that matches the persisted mission state.
        /// Mission 2 and Mission 3 extend beyond the original Chapter1Step
        /// enum, so they must take precedence once their milestones exist.
        /// </summary>
        public string GetCurrentObjective()
        {
            Chapter1SaveData data = CurrentData;
            data.EnsureValidDefaults();

            if (data.Mission03GangHostile)
            {
                return "Chạy thoát khỏi James, David và Lewis.";
            }

            if (data.Mission03ChallengePassed)
            {
                return "Bạn đã vượt qua thử thách của băng nhóm.";
            }

            if (data.Mission02EquipmentDelivered)
            {
                return data.Mission03JamesIntroPlayed
                    ? "Quay lại nói chuyện với James để bắt đầu thử thách."
                    : "Qua nói chuyện với James ở băng nhóm đối diện.";
            }

            if (data.Mission02Started)
            {
                if (data.Mission02HasPsu && data.Mission02HasUps)
                {
                    return "Mang PSU và UPS về cho Minh.";
                }

                if (!data.Mission02HasHenryBattery)
                {
                    return data.Mission02HasBrokenBattery
                        ? "Đánh lạc hướng Henry rồi đánh tráo ắc quy."
                        : "Tìm ắc quy hỏng để đánh tráo ắc quy của Henry.";
                }

                if (!data.Mission02HasUps)
                {
                    return "Quay lại nhặt UPS.";
                }

                return "Tìm và nhặt PSU.";
            }

            return GetObjective(data.CurrentStep);
        }

        private bool HasFinalChoiceBeenApplied()
        {
            return CurrentData.CurrentStep >= Chapter1Step.EndingSequence
                || CurrentData.CopiedHardDrive
                || CurrentData.HidMorisTracking
                || CurrentData.ForcedNamToCooperate;
        }

        private void PublishCurrentState()
        {
            CurrentData.EnsureValidDefaults();
            Chapter1EventBus.RaiseStepChanged(CurrentData.CurrentStep);
            Chapter1EventBus.RaiseObjectiveChanged(GetCurrentObjective());
            Chapter1EventBus.RaiseInventoryChanged();
            Chapter1EventBus.RaiseNamTrustChanged(CurrentData.NamTrust);
            Chapter1EventBus.RaisePowerStateChanged(CurrentData.PowerRestored);
            Chapter1EventBus.RaiseCheckpointChanged(CurrentData.CurrentCheckpointId);
        }

        private void SaveIfAutoSaveEnabled()
        {
            if (autoSaveOnMilestones)
            {
                SaveChapter();
            }
        }

        private void EnsureSaveService()
        {
            saveService ??= new JsonChapter1SaveService();
        }
    }
}
