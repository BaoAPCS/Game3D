using System;
using System.Collections.Generic;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Converts mutable play-session state into one of the three stable test
    /// checkpoints. The source is never repaired or otherwise mutated.
    /// </summary>
    public static class Chapter1CheckpointPolicy
    {
        public static bool IsValidCheckpoint(
            Chapter1MissionCheckpoint checkpoint)
        {
            return Enum.IsDefined(
                typeof(Chapter1MissionCheckpoint),
                checkpoint);
        }

        public static Chapter1SaveData CreateSnapshot(
            Chapter1SaveData runtimeData)
        {
            Chapter1SaveData source =
                runtimeData?.DeepCopy() ?? Chapter1SaveData.CreateDefault();

            // Chapter completion is the only state allowed to survive past
            // the Mission 3 rollback checkpoint. Both combat outcomes reach
            // this state only after Police finishes the arrest dialogue.
            if (source.ChapterCompleted &&
                source.Mission03PoliceArrestCompleted)
            {
                return CreateChapter2Start(source);
            }

            Chapter1MissionCheckpoint checkpoint =
                IsValidCheckpoint(source.CurrentCheckpointId)
                    ? source.CurrentCheckpointId
                    : Chapter1MissionCheckpoint.Mission1Start;

            switch (checkpoint)
            {
                case Chapter1MissionCheckpoint.Mission2Start:
                    return CreateMission2Start(source);
                case Chapter1MissionCheckpoint.Mission3Start:
                    return CreateMission3Start(source);
                default:
                    return CreateMission1Start(source);
            }
        }

        private static Chapter1SaveData CreateMission1Start(
            Chapter1SaveData source)
        {
            Chapter1SaveData snapshot = Chapter1SaveData.CreateDefault();
            snapshot.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission1Start;
            CopyPhoneData(source, snapshot);
            snapshot.EnsureValidDefaults();
            return snapshot;
        }

        private static Chapter1SaveData CreateMission2Start(
            Chapter1SaveData source)
        {
            Chapter1SaveData snapshot = Chapter1SaveData.CreateDefault();
            snapshot.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission2Start;
            CopyPhoneData(source, snapshot);

            // Task 1's completed world baseline remains intact so its door,
            // dialogue and separator interactions do not respawn. Only the
            // final Minh hand-off is replayed to open Task 2 again.
            snapshot.Mission01MinhIntroDialoguePlayed =
                source.Mission01MinhIntroDialoguePlayed;
            snapshot.Mission01DungDoorDiscovered =
                source.Mission01DungDoorDiscovered;
            snapshot.Mission01DungDoorUnlocked =
                source.Mission01DungDoorUnlocked;
            snapshot.Mission01AudioSeparatorCollected =
                source.Mission01AudioSeparatorCollected;
            snapshot.Mission01AudioSeparatorMixerStarted =
                source.Mission01AudioSeparatorMixerStarted;
            snapshot.Mission01AudioSeparatorTutorialSeen =
                source.Mission01AudioSeparatorTutorialSeen;
            snapshot.Mission01CalendarViewed =
                source.Mission01CalendarViewed;

            // Rebuild any missing part of the committed six-stem result.
            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemOrder.Length;
                 i++)
            {
                snapshot.AddSavedStem(
                    LanAudioRecordingCatalog.StemOrder[i]);
            }

            snapshot.FirstMissionStateValue =
                (int)FirstMissionState.ReturnToMinh;
            snapshot.Mission01LanRecordingSeparated = true;
            snapshot.Mission01AudioSeparatorMixerCompleted = true;
            snapshot.Mission01CompletionDialoguePlayed = false;
            snapshot.Mission01Completed = false;
            snapshot.EnsureValidDefaults();
            return snapshot;
        }

        /// <summary>
        /// Phone contents belong to Nam rather than to a replayable world
        /// checkpoint. Preserve them even when a test save rolls the current
        /// mission back to its stable starting state.
        /// </summary>
        private static void CopyPhoneData(
            Chapter1SaveData source,
            Chapter1SaveData destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            destination.PhoneLanRecordingStateValue =
                source.PhoneLanRecordingStateValue;
            destination.HasLanRecording = source.HasLanRecording;
            destination.Mission01LanVoiceRecordingListened =
                source.Mission01LanVoiceRecordingListened;

            destination.Mission01DungBorrowRequestSent =
                source.Mission01DungBorrowRequestSent;
            destination.Mission01DungBorrowReplyReceived =
                source.Mission01DungBorrowReplyReceived;
            destination.Mission01DungPasswordQuestionSent =
                source.Mission01DungPasswordQuestionSent;
            destination.Mission01DungPasswordHintReceived =
                source.Mission01DungPasswordHintReceived;
            destination.Mission01DungBirthdayQuestionSent =
                source.Mission01DungBirthdayQuestionSent;
            destination.Mission01DungBirthdayHintReceived =
                source.Mission01DungBirthdayHintReceived;
            destination.Mission01DungHasUnread =
                source.Mission01DungHasUnread;

            destination.SavedPhoneRecordingIds = new List<string>(
                source.SavedPhoneRecordingIds ?? new List<string>());
            destination.SavedLanAudioStemIds = new List<string>(
                source.SavedLanAudioStemIds ?? new List<string>());
            destination.AudioSeparatorFaderValues = new List<float>(
                source.AudioSeparatorFaderValues ?? new List<float>());
        }

        private static Chapter1SaveData CreateMission3Start(
            Chapter1SaveData source)
        {
            Chapter1SaveData snapshot = CreateMission2Start(source);
            snapshot.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission3Start;

            snapshot.FirstMissionStateValue =
                (int)FirstMissionState.Completed;
            snapshot.Mission01CompletionDialoguePlayed = true;
            snapshot.Mission01Completed = true;

            snapshot.Mission02Started = true;
            snapshot.Mission02HasPsu = true;
            snapshot.Mission02HasUps = true;
            snapshot.Mission02HasBrokenBattery = false;
            snapshot.Mission02HasHenryBattery = true;
            snapshot.Mission02EquipmentDelivered = false;

            // Mission 3/4 progress, combat results and the police key all
            // remain at their default values in this fresh snapshot.
            snapshot.EnsureValidDefaults();
            return snapshot;
        }

        private static Chapter1SaveData CreateChapter2Start(
            Chapter1SaveData source)
        {
            Chapter1SaveData snapshot = source.DeepCopy();
            snapshot.SaveVersion = Chapter1SaveData.CurrentSaveVersion;
            snapshot.CurrentCheckpointId =
                Chapter1MissionCheckpoint.Mission3Start;
            snapshot.CurrentStep = Chapter1Step.ChapterCompleted;
            snapshot.ChapterCompleted = true;
            snapshot.Mission03PoliceArrestCompleted = true;
            snapshot.Mission03PoliceKeyReceived = true;
            snapshot.AddChapterCarryOverItem(
                Chapter1SaveData.PhoneCarryOverItemId);
            snapshot.AddChapterCarryOverItem(
                Chapter1SaveData.PoliceKeyCarryOverItemId);
            snapshot.EnsureValidDefaults();
            return snapshot;
        }
    }
}
