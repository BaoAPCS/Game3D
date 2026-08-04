using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class Mission01AudioSeparatorManager : MonoBehaviour
    {
        public const string AudioSeparatorPersistentId = "mission01.audio_separator_device";
        public const string CorrectDoorPassword = "2502";

        public static Mission01AudioSeparatorManager Instance { get; private set; }

        [SerializeField] private Chapter1Manager chapterManager;
        [SerializeField] private bool autoSaveOnChange = true;

        private bool initialized;

        public event Action<FirstMissionState, FirstMissionState> StateChanged;
        public event Action FirstMissionCompleted;

        public FirstMissionState State => GetStateFromSave();
        public string CurrentObjective => GetObjective(State);
        public Chapter1SaveData Data => ResolveData();
        public bool IsCompleted => State == FirstMissionState.Completed;
        public bool DoorUnlocked => Data.Mission01DungDoorUnlocked;
        public bool AudioSeparatorCollected => Data.Mission01AudioSeparatorCollected;
        public bool LanRecordingSeparated => Data.Mission01LanRecordingSeparated;
        public bool MixerCompleted => Data.Mission01AudioSeparatorMixerCompleted;
        public bool LanVoiceRecordingListened => Data.Mission01LanVoiceRecordingListened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[Mission01AudioSeparatorManager] Duplicate manager on '{gameObject.name}' disabled. Existing manager: '{Instance.gameObject.name}'.", this);
                enabled = false;
                return;
            }

            Instance = this;
            ResolveManager();
            initialized = true;
            PublishCurrentObjective();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetChapterManager(Chapter1Manager manager)
        {
            chapterManager = manager;
            PublishCurrentObjective();
        }

        public void NotifyLanRecordingSaved()
        {
            AdvanceTo(FirstMissionState.GoToMinhRoom);
        }

        public bool TryStartMinhIntroDialogue()
        {
            if (State != FirstMissionState.GoToMinhRoom)
            {
                return false;
            }

            AdvanceTo(FirstMissionState.TalkToMinh);
            return true;
        }

        public void CompleteMinhIntroDialogue()
        {
            Chapter1SaveData data = Data;
            data.Mission01MinhIntroDialoguePlayed = true;
            AdvanceTo(FirstMissionState.MessageDung);
        }

        public void SendDungBorrowRequest()
        {
            if (State != FirstMissionState.MessageDung || Data.Mission01DungBorrowRequestSent)
            {
                return;
            }

            Data.Mission01DungBorrowRequestSent = true;
            SaveAndLog("Dung borrow request sent.");
        }

        public void ReceiveDungBorrowReply()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission01DungBorrowRequestSent || data.Mission01DungBorrowReplyReceived)
            {
                return;
            }

            data.Mission01DungBorrowReplyReceived = true;
            data.Mission01DungHasUnread = true;
            AdvanceTo(FirstMissionState.GoToDungRoom);
        }

        public void DiscoverLockedDoor()
        {
            if (State < FirstMissionState.GoToDungRoom || State >= FirstMissionState.DiscoverLockedDoor)
            {
                return;
            }

            Data.Mission01DungDoorDiscovered = true;
            AdvanceTo(FirstMissionState.DiscoverLockedDoor);
        }

        public void SendDungPasswordQuestion()
        {
            if (State != FirstMissionState.DiscoverLockedDoor || Data.Mission01DungPasswordQuestionSent)
            {
                return;
            }

            Data.Mission01DungPasswordQuestionSent = true;
            SaveAndLog("Dung password question sent.");
        }

        public void ReceiveDungPasswordHint()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission01DungPasswordQuestionSent || data.Mission01DungPasswordHintReceived)
            {
                return;
            }

            data.Mission01DungPasswordHintReceived = true;
            data.Mission01DungHasUnread = true;
            AdvanceTo(FirstMissionState.AskDungPassword);
        }

        public void SendDungBirthdayQuestion()
        {
            if (State != FirstMissionState.AskDungPassword || Data.Mission01DungBirthdayQuestionSent)
            {
                return;
            }

            Data.Mission01DungBirthdayQuestionSent = true;
            SaveAndLog("Dung birthday question sent.");
        }

        public void ReceiveDungBirthdayHint()
        {
            Chapter1SaveData data = Data;
            if (!data.Mission01DungBirthdayQuestionSent || data.Mission01DungBirthdayHintReceived)
            {
                return;
            }

            data.Mission01DungBirthdayHintReceived = true;
            data.Mission01DungHasUnread = true;
            AdvanceTo(FirstMissionState.SolveBirthdayPassword);
        }

        public void ClearDungUnread()
        {
            if (!Data.Mission01DungHasUnread)
            {
                return;
            }

            Data.Mission01DungHasUnread = false;
            SaveAndLog("Dung unread badge cleared.");
        }

        public bool TryUnlockDungDoor(string password)
        {
            if (Data.Mission01DungDoorUnlocked)
            {
                return true;
            }

            if (!string.Equals(password, CorrectDoorPassword, StringComparison.Ordinal))
            {
                return false;
            }

            if (State < FirstMissionState.SolveBirthdayPassword)
            {
                return false;
            }

            Data.Mission01DungDoorUnlocked = true;
            AdvanceTo(FirstMissionState.EnterDungRoom);
            return true;
        }

        public bool StartAudioSeparatorProcessing()
        {
            if (State < FirstMissionState.EnterDungRoom)
            {
                return false;
            }

            Data.Mission01AudioSeparatorMixerStarted = true;
            if (State == FirstMissionState.EnterDungRoom || State == FirstMissionState.FindAudioSeparator)
            {
                return AdvanceTo(FirstMissionState.ProcessAudio);
            }

            SaveIfEnabled();
            return true;
        }

        public void MarkAudioSeparatorCollected()
        {
            MarkAudioSeparatorUsed();
        }

        public void MarkAudioSeparatorUsed()
        {
            NotifyAllLanAudioStemsSaved();
        }

        public void NotifyAllLanAudioStemsSaved()
        {
            if (Data.Mission01LanRecordingSeparated)
            {
                return;
            }

            if (State < FirstMissionState.ProcessAudio)
            {
                Debug.LogWarning($"[Mission01AudioSeparatorManager] Cannot finish audio separator before processing audio. Current state: {State}.", this);
                return;
            }

            Data.Mission01LanRecordingSeparated = true;
            Data.Mission01AudioSeparatorMixerCompleted = true;
            if (AdvanceTo(FirstMissionState.ListenToLanVoice))
            {
                Chapter1EventBus.RaiseAllLanAudioStemsSaved();
            }
        }

        public void NotifyLanVoiceRecordingListened()
        {
            if (Data.Mission01LanVoiceRecordingListened)
            {
                return;
            }

            Data.Mission01LanVoiceRecordingListened = true;
            Chapter1EventBus.RaiseLanVoiceRecordingListened();
            if (State == FirstMissionState.ListenToLanVoice)
            {
                AdvanceTo(FirstMissionState.ReturnToMinh);
            }
            else
            {
                SaveIfEnabled();
            }
        }

        public bool TryCompleteWithMinh(PlayerInventory inventory)
        {
            if (State != FirstMissionState.ReturnToMinh)
            {
                return false;
            }

            if (!Data.Mission01LanVoiceRecordingListened)
            {
                return false;
            }

            Chapter1SaveData data = Data;
            if (data.Mission01CompletionDialoguePlayed)
            {
                return false;
            }

            data.Mission01CompletionDialoguePlayed = true;
            AdvanceTo(FirstMissionState.Completed);
            FirstMissionCompleted?.Invoke();
            Chapter1EventBus.RaiseFirstMissionCompleted();
            return true;
        }

        public void SaveMission()
        {
            ResolveManager();
            chapterManager?.SaveChapter();
        }

        public static string GetObjective(FirstMissionState state)
        {
            switch (state)
            {
                case FirstMissionState.GoToMinhRoom:
                case FirstMissionState.TalkToMinh:
                    return "Qua phòng Minh để nhờ Minh kiểm tra đoạn ghi âm.";
                case FirstMissionState.MessageDung:
                    return "Mở điện thoại và nhắn tin cho Dũng để mượn máy tách âm.";
                case FirstMissionState.GoToDungRoom:
                    return "Lên phòng Dũng ở giữa lầu 2 để tìm máy tách âm.";
                case FirstMissionState.DiscoverLockedDoor:
                case FirstMissionState.AskDungPassword:
                    return "Hỏi Dũng về mật khẩu phòng.";
                case FirstMissionState.SolveBirthdayPassword:
                    return "Tìm ra mật khẩu phòng Dũng.";
                case FirstMissionState.EnterDungRoom:
                case FirstMissionState.FindAudioSeparator:
                case FirstMissionState.ProcessAudio:
                    return "Dùng máy tách âm để xử lý đoạn ghi âm của Chị Lan.";
                case FirstMissionState.ListenToLanVoice:
                    return "Mở điện thoại và nghe lại giọng chị Lan.";
                case FirstMissionState.ReturnToMinh:
                    return "Quay lại báo Minh về đoạn ghi âm đã được tách.";
                case FirstMissionState.Completed:
                    return "Đã dùng máy tách âm cho đoạn ghi âm của Chị Lan.";
                default:
                    return string.Empty;
            }
        }

        private bool AdvanceTo(FirstMissionState nextState)
        {
            Chapter1SaveData data = Data;
            FirstMissionState previous = GetStateFromSave();
            if (nextState < previous)
            {
                Debug.LogWarning($"[Mission01AudioSeparatorManager] Refused to move backward from {previous} to {nextState}.", this);
                return false;
            }

            if (nextState == previous)
            {
                return true;
            }

            data.FirstMissionStateValue = (int)nextState;
            if (nextState == FirstMissionState.Completed)
            {
                data.Mission01Completed = true;
            }

            Debug.Log($"[Mission01AudioSeparatorManager] State changed: {previous} -> {nextState}. Objective: {GetObjective(nextState)}", this);
            StateChanged?.Invoke(previous, nextState);
            Chapter1EventBus.RaiseFirstMissionStateChanged(nextState);
            PublishCurrentObjective();
            SaveIfEnabled();
            return true;
        }

        private FirstMissionState GetStateFromSave()
        {
            Chapter1SaveData data = ResolveData();
            int value = data.FirstMissionStateValue;
            if (value < (int)FirstMissionState.None || value > (int)FirstMissionState.Completed)
            {
                return FirstMissionState.None;
            }

            return (FirstMissionState)value;
        }

        private Chapter1SaveData ResolveData()
        {
            ResolveManager();
            return chapterManager != null ? chapterManager.CurrentData : Chapter1SaveData.CreateDefault();
        }

        private void ResolveManager()
        {
            if (chapterManager == null)
            {
                chapterManager = Chapter1Manager.Instance;
            }
        }

        private void PublishCurrentObjective()
        {
            if (!initialized && !Application.isPlaying)
            {
                return;
            }

            string objective = CurrentObjective;
            if (!string.IsNullOrWhiteSpace(objective))
            {
                Chapter1EventBus.RaiseObjectiveChanged(objective);
            }
        }

        private void SaveAndLog(string message)
        {
            Debug.Log($"[Mission01AudioSeparatorManager] {message}", this);
            SaveIfEnabled();
        }

        private void SaveIfEnabled()
        {
            if (!autoSaveOnChange)
            {
                return;
            }

            SaveMission();
        }
    }
}
