using System;
using System.Collections.Generic;

namespace DormitoryMystery.Chapter1
{
    [Serializable]
    public class Chapter1SaveData
    {
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

        public Chapter1SaveData()
        {
            SaveVersion = 1;
            CurrentStep = Chapter1Step.TalkToNam;
            NamTrust = 45;
            HasLanRecording = true;
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
        }

        public static Chapter1SaveData CreateDefault()
        {
            return new Chapter1SaveData();
        }

        public void EnsureValidDefaults()
        {
            if (SaveVersion <= 0)
            {
                SaveVersion = 1;
            }

            NamTrust = NamTrustCalculator.ClampTrust(NamTrust);
            ThrowableCanCount = Math.Max(0, ThrowableCanCount);

            if (string.IsNullOrWhiteSpace(CurrentCheckpointId))
            {
                CurrentCheckpointId = "ChapterStart";
            }

            CollectedUniqueItemIds ??= new List<string>();
            SeenTutorialIds ??= new List<string>();
        }
    }
}
