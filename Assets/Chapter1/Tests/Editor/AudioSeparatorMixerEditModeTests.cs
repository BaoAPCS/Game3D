using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class AudioSeparatorMixerEditModeTests
    {
        [Test]
        public void CatalogUsesMixedRecordingAndSixStemOutputs()
        {
            Assert.AreEqual("Lan_LastRecording_Mixed", LanAudioRecordingCatalog.MixedRecordingId);
            Assert.AreEqual("Assets/Chapter1/Audio/Phone/Lan_LastRecording_Mixed.mp3", LanAudioRecordingCatalog.MixedPath);
            Assert.AreEqual(6, LanAudioRecordingCatalog.StemCount);

            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                string id = LanAudioRecordingCatalog.GetOutputRecordingId(LanAudioRecordingCatalog.StemOrder[i]);
                Assert.IsTrue(ids.Add(id), "Duplicate recording id: " + id);
                Assert.IsTrue(LanAudioRecordingCatalog.IsKnownRecordingId(id));
            }
        }

        [Test]
        public void SaveDataAddsMixedAndStemRecordingsWithoutDuplicates()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();

            Assert.IsTrue(data.AddPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId));
            Assert.IsFalse(data.AddPhoneRecording(LanAudioRecordingCatalog.MixedRecordingId));
            Assert.IsTrue(data.AddSavedStem(LanAudioStemId.Voice));
            Assert.IsFalse(data.AddSavedStem(LanAudioStemId.Voice));

            Assert.AreEqual(2, data.SavedPhoneRecordingIds.Count);
            Assert.AreEqual(1, data.GetSavedStemCount());
            Assert.IsTrue(data.HasPhoneRecording(LanAudioRecordingCatalog.GetOutputRecordingId(LanAudioStemId.Voice)));
        }

        [Test]
        public void FaderValueControlsAudioSourceVolume()
        {
            GameObject root = new GameObject("FaderVolumeTest");
            try
            {
                AudioSource source = root.AddComponent<AudioSource>();
                AudioStemFader fader = root.AddComponent<AudioStemFader>();
                fader.Configure(null, LanAudioStemId.Rain, "Rain", source, false);

                fader.SetNormalizedValue(0.35f);

                Assert.AreEqual(0.35f, fader.NormalizedValue, 0.001f);
                Assert.AreEqual(0.35f, source.volume, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void IsolationRuleAcceptsExactlyOneLoudStem()
        {
            using MixerRig rig = new MixerRig();
            rig.SetValues(LanAudioStemId.Voice, 0.86f, 0.05f);

            Assert.IsTrue(rig.Mixer.TryGetIsolatedStem(out LanAudioStemId stem, out _));
            Assert.AreEqual(LanAudioStemId.Voice, stem);
        }

        [Test]
        public void IsolationRuleAcceptsExactlyOneMutedStemForRemovalPreview()
        {
            using MixerRig rig = new MixerRig();
            rig.SetValues(LanAudioStemId.Rain, 0.05f, 0.9f);

            Assert.IsTrue(rig.Mixer.TryGetIsolatedStem(out LanAudioStemId stem, out _));
            Assert.AreEqual(LanAudioStemId.Rain, stem);
        }

        [Test]
        public void IsolationRuleRejectsMultipleLoudStems()
        {
            using MixerRig rig = new MixerRig();
            rig.SetAllValues(0.05f);
            rig.SetValue(LanAudioStemId.Voice, 0.8f);
            rig.SetValue(LanAudioStemId.Rain, 0.82f);

            Assert.IsFalse(rig.Mixer.TryGetIsolatedStem(out _, out string message), message);
        }

        [Test]
        public void ResetFadersDoesNotDeleteSavedStemProgress()
        {
            using MixerRig rig = new MixerRig();
            rig.Data.AddSavedStem(LanAudioStemId.Voice);
            rig.SetAllValues(0.1f);

            rig.Mixer.ResetFaders();

            Assert.IsTrue(rig.Data.HasSavedStem(LanAudioStemId.Voice));
            Assert.AreEqual(1, rig.Data.GetSavedStemCount());
            for (int i = 0; i < rig.Faders.Length; i++)
            {
                Assert.AreEqual(1f, rig.Faders[i].NormalizedValue, 0.001f);
            }
        }

        [Test]
        public void SavingAllIsolatedStemsMovesMissionBackToMinh()
        {
            using MixerRig rig = new MixerRig();
            MoveToProcessing(rig.Mission);
            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                rig.Data.AddSavedStem(LanAudioRecordingCatalog.StemOrder[i]);
            }

            rig.Mission.NotifyAllLanAudioStemsSaved();
            Assert.AreEqual(FirstMissionState.ReturnToMinh, rig.Mission.State);
        }

        private static void MoveToProcessing(Mission01AudioSeparatorManager mission)
        {
            mission.NotifyLanRecordingSaved();
            mission.TryStartMinhIntroDialogue();
            mission.CompleteMinhIntroDialogue();
            mission.SendDungBorrowRequest();
            mission.ReceiveDungBorrowReply();
            mission.DiscoverLockedDoor();
            mission.SendDungPasswordQuestion();
            mission.ReceiveDungPasswordHint();
            mission.SendDungBirthdayQuestion();
            mission.ReceiveDungBirthdayHint();
            Assert.IsTrue(mission.TryUnlockDungDoor(Mission01AudioSeparatorManager.CorrectDoorPassword));
            Assert.IsTrue(mission.StartAudioSeparatorProcessing());
        }

        private sealed class MixerRig : System.IDisposable
        {
            private readonly GameObject root;

            public MixerRig()
            {
                root = new GameObject("AudioSeparatorMixer_TestRig");
                ChapterManager = root.AddComponent<Chapter1Manager>();
                Mission = root.AddComponent<Mission01AudioSeparatorManager>();
                Mixer = root.AddComponent<AudioSeparatorMixerController>();
                Playback = root.AddComponent<AudioStemPlaybackController>();

                SetBool(ChapterManager, "autoLoadOnAwake", false);
                SetBool(ChapterManager, "autoSaveOnMilestones", false);
                SetBool(Mission, "autoSaveOnChange", false);
                Mission.SetChapterManager(ChapterManager);

                List<AudioStemFader> faders = new List<AudioStemFader>();
                for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
                {
                    LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                    GameObject faderObject = new GameObject(stem + "_Fader");
                    faderObject.transform.SetParent(root.transform, false);
                    AudioSource source = faderObject.AddComponent<AudioSource>();
                    AudioStemFader fader = faderObject.AddComponent<AudioStemFader>();
                    fader.Configure(Mixer, stem, LanAudioRecordingCatalog.GetStemDisplayName(stem), source, stem == LanAudioStemId.Voice);
                    fader.SetNormalizedValue(1f);
                    faders.Add(fader);
                }

                Faders = faders.ToArray();
                SerializedObject serialized = new SerializedObject(Mixer);
                SetObject(serialized, "missionManager", Mission);
                SetObject(serialized, "playbackController", Playback);
                SetBool(serialized, "requireMixedRecordingSaved", false);
                SetArray(serialized, "faders", Faders);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            public Chapter1Manager ChapterManager { get; }
            public Mission01AudioSeparatorManager Mission { get; }
            public AudioSeparatorMixerController Mixer { get; }
            public AudioStemPlaybackController Playback { get; }
            public AudioStemFader[] Faders { get; }
            public Chapter1SaveData Data => ChapterManager.CurrentData;

            public void SetValues(LanAudioStemId loudStem, float loudValue, float quietValue)
            {
                for (int i = 0; i < Faders.Length; i++)
                {
                    Faders[i].SetNormalizedValue(Faders[i].StemId == loudStem ? loudValue : quietValue);
                }
            }

            public void SetAllValues(float value)
            {
                for (int i = 0; i < Faders.Length; i++)
                {
                    Faders[i].SetNormalizedValue(value);
                }
            }

            public void SetValue(LanAudioStemId stem, float value)
            {
                for (int i = 0; i < Faders.Length; i++)
                {
                    if (Faders[i].StemId == stem)
                    {
                        Faders[i].SetNormalizedValue(value);
                    }
                }
            }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
            }

            private static void SetBool(Object target, string propertyName, bool value)
            {
                SerializedObject serialized = new SerializedObject(target);
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null)
                {
                    property.boolValue = value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            private static void SetObject(SerializedObject serialized, string propertyName, Object value)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null)
                {
                    property.objectReferenceValue = value;
                }
            }

            private static void SetBool(SerializedObject serialized, string propertyName, bool value)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null)
                {
                    property.boolValue = value;
                }
            }

            private static void SetArray(SerializedObject serialized, string propertyName, AudioStemFader[] values)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null || !property.isArray)
                {
                    return;
                }

                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }
        }
    }
}
