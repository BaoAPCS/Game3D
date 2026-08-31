using System;

namespace DormitoryMystery.Chapter2
{
    [Serializable]
    public sealed class Chapter2SaveData
    {
        public const int CurrentSaveVersion = 1;

        public int SaveVersion = CurrentSaveVersion;
        public bool HasPhone = true;
        public bool HasPoliceStationKey = true;
        public bool Mission01CrowbarCollected;
        public bool Mission01ToiletPried;
        public bool Mission01ServiceCardCollected;

        public static Chapter2SaveData CreateDefault()
        {
            Chapter2SaveData data = new Chapter2SaveData();
            data.EnsureValidDefaults();
            return data;
        }

        public void EnsureValidDefaults()
        {
            SaveVersion = CurrentSaveVersion;

            if (Mission01ServiceCardCollected)
            {
                Mission01ToiletPried = true;
            }

            if (Mission01ToiletPried)
            {
                Mission01CrowbarCollected = true;
            }
        }

        public Chapter2SaveData DeepCopy()
        {
            Chapter2SaveData copy = new Chapter2SaveData
            {
                SaveVersion = SaveVersion,
                HasPhone = HasPhone,
                HasPoliceStationKey = HasPoliceStationKey,
                Mission01CrowbarCollected =
                    Mission01CrowbarCollected,
                Mission01ToiletPried = Mission01ToiletPried,
                Mission01ServiceCardCollected =
                    Mission01ServiceCardCollected
            };
            copy.EnsureValidDefaults();
            return copy;
        }
    }
}
