using System;

namespace DormitoryMystery.Chapter2
{
    [Serializable]
    public sealed class Chapter2SaveData
    {
        public const int CurrentSaveVersion = 1;

        public int SaveVersion = CurrentSaveVersion;
        public bool HasPhone;
        public bool HasPoliceStationKey;
        public bool Mission01CrowbarCollected;
        public bool Mission01ToiletPried;
        public bool Mission01ServiceCardCollected;
        public bool Mission02JailObstacleDisabled;
        public bool Mission03PhoneRecovered;
        public bool Mission03PoliceKeyRecovered;
        public bool Mission04ComputerUnlocked;
        public bool Mission04WifiPasswordDiscovered;
        public bool Mission04PoliceWifiConnected;
        public bool Mission04MinhMessagesRead;
        public bool Chapter1PhoneDataImported;
        public Chapter2PhoneData PhoneData;

        public bool Mission03Completed =>
            Mission03PhoneRecovered &&
            Mission03PoliceKeyRecovered;

        public bool Mission04Completed =>
            Mission04PoliceWifiConnected;

        public static Chapter2SaveData CreateDefault()
        {
            Chapter2SaveData data = new Chapter2SaveData();
            data.EnsureValidDefaults();
            return data;
        }

        public void EnsureValidDefaults()
        {
            SaveVersion = CurrentSaveVersion;
            PhoneData ??= Chapter2PhoneData.CreateDefault();
            PhoneData.EnsureValidDefaults();

            if (Mission04MinhMessagesRead)
            {
                Mission04PoliceWifiConnected = true;
            }

            if (Mission04PoliceWifiConnected)
            {
                Mission04WifiPasswordDiscovered = true;
            }

            if (Mission04WifiPasswordDiscovered)
            {
                Mission04ComputerUnlocked = true;
            }

            if (Mission04ComputerUnlocked)
            {
                Mission03PhoneRecovered = true;
                Mission03PoliceKeyRecovered = true;
            }

            // Legacy version-1 saves incorrectly marked these confiscated
            // items as owned. The new recovery flags are absent (false) in
            // those files, so they are the authoritative state from now on.
            HasPhone = Mission03PhoneRecovered;
            HasPoliceStationKey = Mission03PoliceKeyRecovered;

            if (Mission03PhoneRecovered ||
                Mission03PoliceKeyRecovered)
            {
                Mission02JailObstacleDisabled = true;
            }

            if (Mission02JailObstacleDisabled)
            {
                Mission01ServiceCardCollected = true;
            }

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
                    Mission01ServiceCardCollected,
                Mission02JailObstacleDisabled =
                    Mission02JailObstacleDisabled,
                Mission03PhoneRecovered =
                    Mission03PhoneRecovered,
                Mission03PoliceKeyRecovered =
                    Mission03PoliceKeyRecovered,
                Mission04ComputerUnlocked =
                    Mission04ComputerUnlocked,
                Mission04WifiPasswordDiscovered =
                    Mission04WifiPasswordDiscovered,
                Mission04PoliceWifiConnected =
                    Mission04PoliceWifiConnected,
                Mission04MinhMessagesRead =
                    Mission04MinhMessagesRead,
                Chapter1PhoneDataImported =
                    Chapter1PhoneDataImported,
                PhoneData = PhoneData?.DeepCopy() ??
                    Chapter2PhoneData.CreateDefault()
            };
            copy.EnsureValidDefaults();
            return copy;
        }
    }
}
