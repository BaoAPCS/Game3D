using System;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;

namespace DormitoryMystery.Chapter2
{
    /// <summary>
    /// A detached snapshot of the data that belongs to Nam's phone. Chapter 2
    /// imports this payload once and then owns it independently from the
    /// Chapter 1 save file.
    /// </summary>
    [Serializable]
    public sealed class Chapter2PhoneData
    {
        public int PhoneLanRecordingStateValue;
        public bool HasLanRecording;
        public bool Mission01LanVoiceRecordingListened;
        public bool Mission01DungBorrowRequestSent;
        public bool Mission01DungBorrowReplyReceived;
        public bool Mission01DungPasswordQuestionSent;
        public bool Mission01DungPasswordHintReceived;
        public bool Mission01DungBirthdayQuestionSent;
        public bool Mission01DungBirthdayHintReceived;
        public bool Mission01DungHasUnread;
        public List<string> SavedPhoneRecordingIds;
        public List<string> SavedLanAudioStemIds;
        public List<float> AudioSeparatorFaderValues;

        public Chapter2PhoneData()
        {
            SavedPhoneRecordingIds = new List<string>();
            SavedLanAudioStemIds = new List<string>();
            AudioSeparatorFaderValues = CreateDefaultFaderValues();
        }

        public static Chapter2PhoneData CreateDefault()
        {
            Chapter2PhoneData data = new Chapter2PhoneData();
            data.EnsureValidDefaults();
            return data;
        }

        public static Chapter2PhoneData FromChapter1(
            Chapter1SaveData chapter1Data)
        {
            if (chapter1Data == null)
            {
                return CreateDefault();
            }

            Chapter1SaveData source = chapter1Data.DeepCopy();
            source.EnsureValidDefaults();

            Chapter2PhoneData data = new Chapter2PhoneData
            {
                PhoneLanRecordingStateValue =
                    source.PhoneLanRecordingStateValue,
                HasLanRecording = source.HasLanRecording,
                Mission01LanVoiceRecordingListened =
                    source.Mission01LanVoiceRecordingListened,
                Mission01DungBorrowRequestSent =
                    source.Mission01DungBorrowRequestSent,
                Mission01DungBorrowReplyReceived =
                    source.Mission01DungBorrowReplyReceived,
                Mission01DungPasswordQuestionSent =
                    source.Mission01DungPasswordQuestionSent,
                Mission01DungPasswordHintReceived =
                    source.Mission01DungPasswordHintReceived,
                Mission01DungBirthdayQuestionSent =
                    source.Mission01DungBirthdayQuestionSent,
                Mission01DungBirthdayHintReceived =
                    source.Mission01DungBirthdayHintReceived,
                Mission01DungHasUnread =
                    source.Mission01DungHasUnread,
                SavedPhoneRecordingIds = CopyList(
                    source.SavedPhoneRecordingIds),
                SavedLanAudioStemIds = CopyList(
                    source.SavedLanAudioStemIds),
                AudioSeparatorFaderValues = CopyList(
                    source.AudioSeparatorFaderValues)
            };
            data.EnsureValidDefaults();
            return data;
        }

        public Chapter1SaveData ToChapter1SaveData()
        {
            Chapter2PhoneData source = DeepCopy();
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.PhoneLanRecordingStateValue =
                source.PhoneLanRecordingStateValue;
            data.HasLanRecording = source.HasLanRecording;
            data.Mission01LanVoiceRecordingListened =
                source.Mission01LanVoiceRecordingListened;
            data.Mission01DungBorrowRequestSent =
                source.Mission01DungBorrowRequestSent;
            data.Mission01DungBorrowReplyReceived =
                source.Mission01DungBorrowReplyReceived;
            data.Mission01DungPasswordQuestionSent =
                source.Mission01DungPasswordQuestionSent;
            data.Mission01DungPasswordHintReceived =
                source.Mission01DungPasswordHintReceived;
            data.Mission01DungBirthdayQuestionSent =
                source.Mission01DungBirthdayQuestionSent;
            data.Mission01DungBirthdayHintReceived =
                source.Mission01DungBirthdayHintReceived;
            data.Mission01DungHasUnread =
                source.Mission01DungHasUnread;
            data.SavedPhoneRecordingIds = CopyList(
                source.SavedPhoneRecordingIds);
            data.SavedLanAudioStemIds = CopyList(
                source.SavedLanAudioStemIds);
            data.AudioSeparatorFaderValues = CopyList(
                source.AudioSeparatorFaderValues);
            data.EnsureValidDefaults();
            return data;
        }

        public void EnsureValidDefaults()
        {
            SavedPhoneRecordingIds ??= new List<string>();
            SavedLanAudioStemIds ??= new List<string>();
            AudioSeparatorFaderValues ??=
                CreateDefaultFaderValues();

            NormalizeIds(SavedPhoneRecordingIds);
            NormalizeIds(SavedLanAudioStemIds);

            int minimumState = (int)LanRecordingMissionState.NotStarted;
            int maximumState = (int)LanRecordingMissionState.Completed;
            PhoneLanRecordingStateValue = Math.Max(
                minimumState,
                Math.Min(maximumState, PhoneLanRecordingStateValue));

            bool hasMixedRecording = SavedPhoneRecordingIds.Contains(
                LanAudioRecordingCatalog.MixedRecordingId);
            if (HasLanRecording || hasMixedRecording)
            {
                HasLanRecording = true;
                AddUnique(
                    SavedPhoneRecordingIds,
                    LanAudioRecordingCatalog.MixedRecordingId);
                PhoneLanRecordingStateValue = Math.Max(
                    PhoneLanRecordingStateValue,
                    (int)LanRecordingMissionState.RecordingDownloaded);
            }

            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemOrder.Length;
                 i++)
            {
                LanAudioStemId stem =
                    LanAudioRecordingCatalog.StemOrder[i];
                string stemId = stem.ToString();
                string recordingId =
                    LanAudioRecordingCatalog.GetOutputRecordingId(stem);
                if (!SavedLanAudioStemIds.Contains(stemId) &&
                    !SavedPhoneRecordingIds.Contains(recordingId))
                {
                    continue;
                }

                AddUnique(SavedLanAudioStemIds, stemId);
                AddUnique(SavedPhoneRecordingIds, recordingId);
            }

            while (AudioSeparatorFaderValues.Count <
                   LanAudioRecordingCatalog.StemCount)
            {
                AudioSeparatorFaderValues.Add(1f);
            }

            while (AudioSeparatorFaderValues.Count >
                   LanAudioRecordingCatalog.StemCount)
            {
                AudioSeparatorFaderValues.RemoveAt(
                    AudioSeparatorFaderValues.Count - 1);
            }

            for (int i = 0;
                 i < AudioSeparatorFaderValues.Count;
                 i++)
            {
                AudioSeparatorFaderValues[i] = Math.Max(
                    0f,
                    Math.Min(1f, AudioSeparatorFaderValues[i]));
            }
        }

        public Chapter2PhoneData DeepCopy()
        {
            Chapter2PhoneData copy = new Chapter2PhoneData
            {
                PhoneLanRecordingStateValue =
                    PhoneLanRecordingStateValue,
                HasLanRecording = HasLanRecording,
                Mission01LanVoiceRecordingListened =
                    Mission01LanVoiceRecordingListened,
                Mission01DungBorrowRequestSent =
                    Mission01DungBorrowRequestSent,
                Mission01DungBorrowReplyReceived =
                    Mission01DungBorrowReplyReceived,
                Mission01DungPasswordQuestionSent =
                    Mission01DungPasswordQuestionSent,
                Mission01DungPasswordHintReceived =
                    Mission01DungPasswordHintReceived,
                Mission01DungBirthdayQuestionSent =
                    Mission01DungBirthdayQuestionSent,
                Mission01DungBirthdayHintReceived =
                    Mission01DungBirthdayHintReceived,
                Mission01DungHasUnread = Mission01DungHasUnread,
                SavedPhoneRecordingIds = CopyList(
                    SavedPhoneRecordingIds),
                SavedLanAudioStemIds = CopyList(
                    SavedLanAudioStemIds),
                AudioSeparatorFaderValues = CopyList(
                    AudioSeparatorFaderValues)
            };
            copy.EnsureValidDefaults();
            return copy;
        }

        private static void NormalizeIds(List<string> ids)
        {
            HashSet<string> seen = new HashSet<string>(
                StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id) ||
                    !seen.Add(id))
                {
                    ids.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void AddUnique(
            List<string> values,
            string value)
        {
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }

        private static List<T> CopyList<T>(List<T> source)
        {
            return source != null
                ? new List<T>(source)
                : new List<T>();
        }

        private static List<float> CreateDefaultFaderValues()
        {
            List<float> values = new List<float>(
                LanAudioRecordingCatalog.StemCount);
            for (int i = 0;
                 i < LanAudioRecordingCatalog.StemCount;
                 i++)
            {
                values.Add(1f);
            }

            return values;
        }
    }
}
