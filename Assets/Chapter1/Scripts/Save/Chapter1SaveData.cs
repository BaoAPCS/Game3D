using System;
using System.Collections.Generic;

namespace DormitoryMystery.Chapter1
{
    [Serializable]
    public class Chapter1SaveData
    {
        private const int CurrentSaveVersion = 6;

        public int SaveVersion;
        public Chapter1Step CurrentStep;
        public int NamTrust;
        public bool HasLanRecording;
        public bool HasFlashlight;
        public bool HasFuse;
        public bool HasHardDrive;
        public int ThrowableCanCount;
        public bool CopiedHardDrive;
        public bool HidMorisTracking;
        public bool ForcedNamToCooperate;
        public bool PowerRestored;
        public bool CCTVChecked;
        public bool FootprintsRevealed;
        public bool RaincoatEnemyActivated;
        public bool AudioPuzzleSolved;
        public bool ChapterCompleted;
        public string CurrentCheckpointId;
        public List<string> CollectedUniqueItemIds;
        public List<string> SeenTutorialIds;
        public int FirstMissionStateValue;
        public bool Mission01MinhIntroDialoguePlayed;
        public bool Mission01CompletionDialoguePlayed;
        public bool Mission01DungBorrowRequestSent;
        public bool Mission01DungBorrowReplyReceived;
        public bool Mission01DungPasswordQuestionSent;
        public bool Mission01DungPasswordHintReceived;
        public bool Mission01DungBirthdayQuestionSent;
        public bool Mission01DungBirthdayHintReceived;
        public bool Mission01DungHasUnread;
        public bool Mission01DungDoorDiscovered;
        public bool Mission01DungDoorUnlocked;
        public bool Mission01AudioSeparatorCollected;
        public bool Mission01LanRecordingSeparated;
        public bool Mission01AudioSeparatorMixerStarted;
        public bool Mission01AudioSeparatorMixerCompleted;
        public bool Mission01AudioSeparatorTutorialSeen;
        public bool Mission01LanVoiceRecordingListened;
        public List<string> SavedPhoneRecordingIds;
        public List<string> SavedLanAudioStemIds;
        public List<float> AudioSeparatorFaderValues;
        public bool Mission01Completed;
        public bool Mission01CalendarViewed;
        public bool Mission02Started;
        public bool Mission02HasPsu;
        public bool Mission02HasUps;
        public bool Mission02HasBrokenBattery;
        public bool Mission02HasHenryBattery;
        public bool Mission02EquipmentDelivered;
        public bool Mission03JamesIntroPlayed;
        public bool Mission03ChallengePassed;
        public bool Mission03GangHostile;
        public bool Mission03PoliceKeyReceived;
        public bool Mission03HenryConfrontationCompleted;
        public bool Mission03HenryDefeated;
        public bool Mission03PoliceArrestCompleted;

        public Chapter1SaveData()
        {
            SaveVersion = CurrentSaveVersion;
            CurrentStep = Chapter1Step.TalkToNam;
            NamTrust = 45;
            HasLanRecording = false;
            HasFlashlight = false;
            HasFuse = false;
            HasHardDrive = false;
            ThrowableCanCount = 0;
            CopiedHardDrive = false;
            HidMorisTracking = false;
            ForcedNamToCooperate = false;
            PowerRestored = false;
            CCTVChecked = false;
            FootprintsRevealed = false;
            RaincoatEnemyActivated = false;
            AudioPuzzleSolved = false;
            ChapterCompleted = false;
            CurrentCheckpointId = "ChapterStart";
            CollectedUniqueItemIds = new List<string>();
            SeenTutorialIds = new List<string>();
            SavedPhoneRecordingIds = new List<string>();
            SavedLanAudioStemIds = new List<string>();
            AudioSeparatorFaderValues = CreateDefaultFaderValues();
            FirstMissionStateValue = (int)FirstMissionState.None;
        }

        public static Chapter1SaveData CreateDefault()
        {
            return new Chapter1SaveData();
        }

        public void EnsureValidDefaults()
        {
            // In version 5 this flag meant only that police_car had reached
            // Nam after Henry was defeated. Version 6 moves the flag to the
            // end of the Police dialogue and shares it between both combat
            // outcomes. Preserve the old victory, but replay the new pursuit
            // and dialogue for a legacy save.
            bool migrateLegacyPoliceArrival =
                SaveVersion < 6 && Mission03PoliceArrestCompleted;
            if (migrateLegacyPoliceArrival)
            {
                Mission03HenryDefeated = true;
                Mission03PoliceArrestCompleted = false;
            }

            if (SaveVersion < CurrentSaveVersion)
            {
                SaveVersion = CurrentSaveVersion;
            }

            NamTrust = NamTrustCalculator.ClampTrust(NamTrust);
            ThrowableCanCount = Math.Max(0, ThrowableCanCount);

            if (string.IsNullOrWhiteSpace(CurrentCheckpointId))
            {
                CurrentCheckpointId = "ChapterStart";
            }

            CollectedUniqueItemIds ??= new List<string>();
            SeenTutorialIds ??= new List<string>();
            SavedPhoneRecordingIds ??= new List<string>();
            SavedLanAudioStemIds ??= new List<string>();
            AudioSeparatorFaderValues ??= CreateDefaultFaderValues();
            while (AudioSeparatorFaderValues.Count < LanAudioRecordingCatalog.StemCount)
            {
                AudioSeparatorFaderValues.Add(1f);
            }

            for (int i = AudioSeparatorFaderValues.Count - 1; i >= LanAudioRecordingCatalog.StemCount; i--)
            {
                AudioSeparatorFaderValues.RemoveAt(i);
            }

            for (int i = 0; i < AudioSeparatorFaderValues.Count; i++)
            {
                AudioSeparatorFaderValues[i] = Math.Max(0f, Math.Min(1f, AudioSeparatorFaderValues[i]));
            }

            if (HasLanRecording)
            {
                AddPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId);
            }

            SyncStemRecordings();

            if (FirstMissionStateValue < (int)FirstMissionState.None || FirstMissionStateValue > (int)FirstMissionState.Completed)
            {
                FirstMissionStateValue = (int)FirstMissionState.None;
            }

            // Keep the newer story flags internally consistent when loading
            // an older save or a save written part-way through a milestone.
            if (Mission02EquipmentDelivered)
            {
                Mission02Started = true;
                Mission02HasPsu = true;
                Mission02HasUps = true;
                Mission02HasHenryBattery = true;
                Mission02HasBrokenBattery = false;
            }

            if (Mission02HasPsu ||
                Mission02HasUps ||
                Mission02HasBrokenBattery ||
                Mission02HasHenryBattery)
            {
                Mission02Started = true;
            }

            if (Mission02HasUps)
            {
                Mission02HasHenryBattery = true;
            }

            if (Mission02HasHenryBattery)
            {
                Mission02HasBrokenBattery = false;
            }

            // Repair partially-written/newer saves from the most advanced
            // Mission 3 flag backwards so every prerequisite remains one-way.
            if (Mission03HenryDefeated)
            {
                Mission03HenryConfrontationCompleted = true;
            }

            // The final arrest dialogue does not reveal who won the fight.
            // It does close Chapter 1 and repair the shared prerequisite
            // chain for either outcome.
            if (Mission03PoliceArrestCompleted)
            {
                Mission03HenryConfrontationCompleted = true;
                ChapterCompleted = true;
                CurrentStep = Chapter1Step.ChapterCompleted;
            }

            if (Mission03JamesIntroPlayed ||
                Mission03ChallengePassed ||
                Mission03GangHostile ||
                Mission03PoliceKeyReceived ||
                Mission03HenryConfrontationCompleted ||
                Mission03HenryDefeated ||
                Mission03PoliceArrestCompleted)
            {
                Mission02EquipmentDelivered = true;
                Mission02Started = true;
                Mission02HasPsu = true;
                Mission02HasUps = true;
                Mission02HasHenryBattery = true;
                Mission02HasBrokenBattery = false;
            }

            if (Mission03ChallengePassed)
            {
                Mission03JamesIntroPlayed = true;
                Mission03GangHostile = false;
            }

            if (Mission03GangHostile)
            {
                Mission03JamesIntroPlayed = true;
                Mission03ChallengePassed = false;
            }

            // The new Mission 3 milestones are intentionally one-way. A
            // legacy v2 save with ChallengePassed remains at the James reward
            // step; it is not silently upgraded to owning the key.
            if (Mission03HenryConfrontationCompleted)
            {
                Mission03PoliceKeyReceived = true;
            }

            if (Mission03PoliceKeyReceived)
            {
                Mission03JamesIntroPlayed = true;
                Mission03ChallengePassed = true;
                Mission03GangHostile = false;
            }
        }

        public bool HasPhoneRecording(string recordingId)
        {
            EnsureValidCollectionsOnly();
            return !string.IsNullOrWhiteSpace(recordingId) && SavedPhoneRecordingIds.Contains(recordingId);
        }

        public bool AddPhoneRecording(string recordingId)
        {
            EnsureValidCollectionsOnly();
            if (string.IsNullOrWhiteSpace(recordingId) || SavedPhoneRecordingIds.Contains(recordingId))
            {
                return false;
            }

            SavedPhoneRecordingIds.Add(recordingId);
            if (recordingId == LanAudioRecordingCatalog.MixedRecordingId)
            {
                HasLanRecording = true;
            }

            return true;
        }

        public bool HasSavedStem(LanAudioStemId stem)
        {
            EnsureValidCollectionsOnly();
            return SavedLanAudioStemIds.Contains(stem.ToString());
        }

        public bool AddSavedStem(LanAudioStemId stem)
        {
            EnsureValidCollectionsOnly();
            string stemId = stem.ToString();
            bool changed = false;
            if (!SavedLanAudioStemIds.Contains(stemId))
            {
                SavedLanAudioStemIds.Add(stemId);
                changed = true;
            }

            string recordingId = LanAudioRecordingCatalog.GetOutputRecordingId(stem);
            return AddPhoneRecording(recordingId) || changed;
        }

        public int GetSavedStemCount()
        {
            EnsureValidCollectionsOnly();
            int count = 0;
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                if (HasSavedStem(LanAudioRecordingCatalog.StemOrder[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public bool AreAllLanAudioStemsSaved()
        {
            return GetSavedStemCount() >= LanAudioRecordingCatalog.StemCount;
        }

        private void SyncStemRecordings()
        {
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                string recordingId = LanAudioRecordingCatalog.GetOutputRecordingId(stem);
                if (SavedPhoneRecordingIds.Contains(recordingId) && !SavedLanAudioStemIds.Contains(stem.ToString()))
                {
                    SavedLanAudioStemIds.Add(stem.ToString());
                }
                else if (SavedLanAudioStemIds.Contains(stem.ToString()) && !SavedPhoneRecordingIds.Contains(recordingId))
                {
                    SavedPhoneRecordingIds.Add(recordingId);
                }
            }

            Mission01AudioSeparatorMixerCompleted = AreAllLanAudioStemsSaved();
        }

        private void EnsureValidCollectionsOnly()
        {
            SavedPhoneRecordingIds ??= new List<string>();
            SavedLanAudioStemIds ??= new List<string>();
            AudioSeparatorFaderValues ??= CreateDefaultFaderValues();
        }

        private static List<float> CreateDefaultFaderValues()
        {
            List<float> values = new List<float>(LanAudioRecordingCatalog.StemCount);
            for (int i = 0; i < LanAudioRecordingCatalog.StemCount; i++)
            {
                values.Add(1f);
            }

            return values;
        }
    }
}
