using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class AudioSeparatorMixerSetupTool
    {
        public const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        public const string ReportPath = "Assets/Chapter1/Documentation/AUDIO_SEPARATOR_MIXER_SETUP_REPORT.md";
        public const string MessageAssetPath = "Assets/Chapter1/Data/Phone/Messages/Lan_LastRecordingMessage.asset";
        public const string DeviceObjectName = "AudioSeparator_Device";
        public const string LegacyBackupFolder = "Assets/Chapter1/Audio/Phone/_LegacyBackup";

        private const string MaterialFolderPath = "Assets/Chapter1/Materials/Props";
        private const string PrefabPath = "Assets/Chapter1/Prefabs/Gameplay/AudioSeparator_Device.prefab";
        private const string ManagerObjectName = "Mission01AudioSeparatorManager";

        [MenuItem("Tools/Chapter 1/Setup Audio Separator Mixer")]
        public static void SetupAudioSeparatorMixer()
        {
            MixerSetupReport report = RunSetup(true);
            Debug.Log(report.ToConsoleString());
        }

        [MenuItem("Tools/Chapter 1/Validate Audio Separator Mixer")]
        public static void ValidateAudioSeparatorMixer()
        {
            MixerSetupReport report = RunValidation(true);
            Debug.Log(report.ToConsoleString());
        }

        public static void SetupAudioSeparatorMixerNoDialog()
        {
            MixerSetupReport report = RunSetup(false);
            Debug.Log(report.ToConsoleString());
        }

        public static void ValidateAudioSeparatorMixerNoDialog()
        {
            MixerSetupReport report = RunValidation(false);
            Debug.Log(report.ToConsoleString());
        }

        public static MixerSetupReport RunSetup(bool showDialog)
        {
            MixerSetupReport report = new MixerSetupReport("AUDIO SEPARATOR MIXER SETUP REPORT");
            EnsureFolders();

            Mission01AudioSeparatorSetupTool.RunSetup(false);
            report.Info("Ran base Mission 01 Audio Separator setup first.");

            Scene scene = OpenTargetScene(report, showDialog);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Fail($"Could not open target scene: {ScenePath}.");
                Finish(report, showDialog, "Setup Audio Separator Mixer");
                return report;
            }

            Dictionary<LanAudioStemId, AudioClip> stemClips = new Dictionary<LanAudioStemId, AudioClip>();
            AudioClip mixedClip = PrepareAudioAssets(stemClips, report);
            PhoneMessageData voiceMessage = ConfigureVoiceMessage(mixedClip, report);
            Chapter1Manager chapterManager = FindFirstSceneComponent<Chapter1Manager>(scene);
            Mission01AudioSeparatorManager missionManager = EnsureMissionManager(scene, chapterManager, report);
            LanRecordingMissionController lanController = ConfigureLanRecordingController(scene, mixedClip, voiceMessage, report);

            GameObject device = FindSceneObject(scene, DeviceObjectName);
            if (device == null)
            {
                device = new GameObject(DeviceObjectName);
                SceneManager.MoveGameObjectToScene(device, scene);
                report.Warning($"{DeviceObjectName} was missing. Created an empty world device marker; position it on Dũng's table if needed.");
            }
            else
            {
                report.Pass($"Found existing {DeviceObjectName}; preserved root transform/model/material.");
            }

            AudioSeparatorMixerController mixer = ConfigureDeviceMixer(device, missionManager, lanController, stemClips, report);
            ConnectPhoneControllers(scene, missionManager, mixer, report);
            SaveAudioSeparatorPrefab(device, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            MoveLegacyAssets(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.SetDirty(device);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrWhiteSpace(scene.path))
            {
                EditorSceneManager.SaveScene(scene);
                report.Pass($"Saved scene references: {scene.path}.");
            }
            else
            {
                report.Warning("Scene has not been saved before, so setup did not auto-save it.");
            }

            Selection.activeGameObject = device;
            Finish(report, showDialog, "Setup Audio Separator Mixer");
            return report;
        }

        public static MixerSetupReport RunValidation(bool showDialog)
        {
            MixerSetupReport report = new MixerSetupReport("AUDIO SEPARATOR MIXER VALIDATION REPORT");
            Scene scene = OpenTargetScene(report, showDialog);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Fail($"Could not open target scene: {ScenePath}.");
                Finish(report, showDialog, "Validate Audio Separator Mixer");
                return report;
            }

            ValidateAudioAssets(report);
            ValidateVoiceMessage(report);
            ValidateScene(scene, report);
            Finish(report, showDialog, "Validate Audio Separator Mixer");
            return report;
        }

        private static AudioClip PrepareAudioAssets(Dictionary<LanAudioStemId, AudioClip> stemClips, MixerSetupReport report)
        {
            string mixedPath = EnsureCanonicalAudioPath(
                LanAudioRecordingCatalog.MixedFileName,
                LanAudioRecordingCatalog.LegacyMixedFileName,
                report);
            ConfigureAudioImporter(mixedPath, report);
            AudioClip mixedClip = LoadAudioClip(mixedPath, "mixed recording", report);

            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                string stemPath = EnsureCanonicalAudioPath(LanAudioRecordingCatalog.GetStemFileName(stem), null, report);
                ConfigureAudioImporter(stemPath, report);
                AudioClip stemClip = LoadAudioClip(stemPath, LanAudioRecordingCatalog.GetStemDisplayName(stem), report);
                stemClips[stem] = stemClip;

                if (mixedClip != null && stemClip != null)
                {
                    float durationDelta = Mathf.Abs(stemClip.length - mixedClip.length);
                    if (durationDelta > 0.1f)
                    {
                        report.Warning($"{stemPath} duration differs from mixed clip by {durationDelta:0.000}s.");
                    }

                    if (stemClip.channels != mixedClip.channels)
                    {
                        report.Warning($"{stemPath} channels ({stemClip.channels}) differ from mixed clip ({mixedClip.channels}).");
                    }

                    if (stemClip.frequency != mixedClip.frequency)
                    {
                        report.Warning($"{stemPath} sample rate ({stemClip.frequency}Hz) differs from mixed clip ({mixedClip.frequency}Hz).");
                    }
                }
            }

            return mixedClip;
        }

        private static string EnsureCanonicalAudioPath(string expectedFileName, string legacyFileName, MixerSetupReport report)
        {
            string expectedPath = LanAudioRecordingCatalog.AudioFolder + "/" + expectedFileName;
            if (AssetDatabase.LoadAssetAtPath<Object>(expectedPath) != null)
            {
                return expectedPath;
            }

            string legacyPath = !string.IsNullOrWhiteSpace(legacyFileName)
                ? FindAudioPathByFileName(legacyFileName)
                : null;
            if (!string.IsNullOrWhiteSpace(legacyPath))
            {
                string moveError = AssetDatabase.MoveAsset(legacyPath, expectedPath);
                if (string.IsNullOrWhiteSpace(moveError))
                {
                    report.Pass($"Renamed audio asset with AssetDatabase.MoveAsset: {legacyPath} -> {expectedPath}.");
                    return expectedPath;
                }

                report.Fail($"Could not rename {legacyPath} to {expectedPath}: {moveError}");
                return expectedPath;
            }

            string caseInsensitivePath = FindAudioPathByFileName(expectedFileName);
            if (!string.IsNullOrWhiteSpace(caseInsensitivePath) &&
                !string.Equals(caseInsensitivePath, expectedPath, StringComparison.Ordinal))
            {
                string moveError = AssetDatabase.MoveAsset(caseInsensitivePath, expectedPath);
                if (string.IsNullOrWhiteSpace(moveError))
                {
                    report.Pass($"Normalized audio asset path with AssetDatabase.MoveAsset: {caseInsensitivePath} -> {expectedPath}.");
                    return expectedPath;
                }

                report.Fail($"Could not normalize {caseInsensitivePath} to {expectedPath}: {moveError}");
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(expectedPath) == null)
            {
                report.Fail($"Missing required audio asset: {expectedPath}.");
            }

            return expectedPath;
        }

        private static void ConfigureAudioImporter(string path, MixerSetupReport report)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                report.Fail($"No AudioImporter found for {path}.");
                return;
            }

            bool changed = false;
            if (importer.forceToMono)
            {
                importer.forceToMono = false;
                changed = true;
            }

            if (importer.loadInBackground)
            {
                importer.loadInBackground = false;
                changed = true;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (!settings.preloadAudioData)
            {
                settings.preloadAudioData = true;
                changed = true;
            }

            if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                changed = true;
            }

            if (settings.compressionFormat != AudioCompressionFormat.PCM)
            {
                settings.compressionFormat = AudioCompressionFormat.PCM;
                changed = true;
            }

            if (changed)
            {
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                report.Pass($"Configured audio import settings: {path} (Stereo allowed, Preload, DecompressOnLoad, PCM).");
            }
            else
            {
                report.Pass($"Audio import settings already correct: {path}.");
            }
        }

        private static AudioClip LoadAudioClip(string path, string label, MixerSetupReport report)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                report.Fail($"Could not load {label} AudioClip with AssetDatabase.LoadAssetAtPath<AudioClip>(): {path}.");
                return null;
            }

            report.Pass($"Loaded {label}: {path} ({clip.length:0.000}s, {clip.channels}ch, {clip.frequency}Hz).");
            return clip;
        }

        private static PhoneMessageData ConfigureVoiceMessage(AudioClip mixedClip, MixerSetupReport report)
        {
            PhoneMessageData message = AssetDatabase.LoadAssetAtPath<PhoneMessageData>(MessageAssetPath);
            if (message == null)
            {
                message = ScriptableObject.CreateInstance<PhoneMessageData>();
                AssetDatabase.CreateAsset(message, MessageAssetPath);
                report.Warning($"Created missing phone message data asset: {MessageAssetPath}.");
            }

            SerializedObject serialized = new SerializedObject(message);
            SetString(serialized, "messageId", LanAudioRecordingCatalog.MixedRecordingId);
            SetString(serialized, "senderId", "lan");
            SetString(serialized, "content", "Tin nhắn thoại");
            SetBool(serialized, "isFromPlayer", false);
            SetBool(serialized, "isRead", false);
            SetBool(serialized, "isDownloaded", false);
            SetEnum(serialized, "messageType", (int)PhoneMessageType.Audio);
            SetObject(serialized, "audioClip", mixedClip);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(message);

            if (mixedClip != null)
            {
                report.Pass($"Assigned mixed AudioClip to Chị Lan voice message data: {MessageAssetPath}.");
            }

            return message;
        }

        private static LanRecordingMissionController ConfigureLanRecordingController(
            Scene scene,
            AudioClip mixedClip,
            PhoneMessageData voiceMessage,
            MixerSetupReport report)
        {
            LanRecordingMissionController controller = FindFirstSceneComponent<LanRecordingMissionController>(scene);
            if (controller == null)
            {
                GameObject controllerObject = new GameObject("LanRecordingMissionController");
                SceneManager.MoveGameObjectToScene(controllerObject, scene);
                controller = controllerObject.AddComponent<LanRecordingMissionController>();
                report.Warning("LanRecordingMissionController was missing. Created a scene controller.");
            }

            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "lanRecordingClip", mixedClip);
            SetObject(serialized, "lanVoiceMessage", voiceMessage);
            SetString(serialized, "expectedAudioAssetPath", LanAudioRecordingCatalog.MixedPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            report.Pass($"Configured LanRecordingMissionController to use mixed recording: {LanAudioRecordingCatalog.MixedPath}.");
            return controller;
        }

        private static Mission01AudioSeparatorManager EnsureMissionManager(
            Scene scene,
            Chapter1Manager chapterManager,
            MixerSetupReport report)
        {
            Mission01AudioSeparatorManager manager = FindFirstSceneComponent<Mission01AudioSeparatorManager>(scene);
            if (manager == null)
            {
                GameObject managerObject = new GameObject(ManagerObjectName);
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                manager = managerObject.AddComponent<Mission01AudioSeparatorManager>();
                report.Warning("Mission01AudioSeparatorManager was missing. Created one.");
            }

            SerializedObject serialized = new SerializedObject(manager);
            SetObject(serialized, "chapterManager", chapterManager);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            return manager;
        }

        private static AudioSeparatorMixerController ConfigureDeviceMixer(
            GameObject device,
            Mission01AudioSeparatorManager missionManager,
            LanRecordingMissionController lanController,
            Dictionary<LanAudioStemId, AudioClip> stemClips,
            MixerSetupReport report)
        {
            RemoveComponentIfPresent<ItemPickup>(device, report);
            RemoveComponentIfPresent<WorldPickupPersistence>(device, report);
            EnsureTriggerCollider(device, report);

            AudioSeparatorMixerController mixer = EnsureComponent<AudioSeparatorMixerController>(device);
            AudioStemPlaybackController playback = EnsureComponent<AudioStemPlaybackController>(device);
            Mission01AudioSeparatorDeviceInteractable interactable = EnsureComponent<Mission01AudioSeparatorDeviceInteractable>(device);

            Transform interactionPoint = EnsureChild(device.transform, "InteractionPoint");
            interactionPoint.localPosition = new Vector3(0f, 0.22f, 0.48f);
            interactionPoint.localRotation = Quaternion.identity;
            interactionPoint.localScale = Vector3.one;

            Transform cameraFocusPoint = EnsureChild(device.transform, "CameraFocusPoint");
            cameraFocusPoint.localPosition = new Vector3(0f, 0.62f, -0.95f);
            Vector3 lookTarget = device.transform.TransformPoint(new Vector3(0f, 0.12f, 0f));
            Vector3 lookDirection = lookTarget - cameraFocusPoint.position;
            cameraFocusPoint.rotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : device.transform.rotation;
            cameraFocusPoint.localScale = Vector3.one;

            Transform audioSourcesRoot = EnsureChild(device.transform, "AudioSources");
            Transform controlsRoot = EnsureChild(device.transform, "Controls");

            List<AudioStemFader> faders = new List<AudioStemFader>();
            Material railMaterial = EnsureMaterial(MaterialFolderPath + "/AudioSeparator_FaderRail.mat", new Color(0.05f, 0.055f, 0.06f, 1f));
            Material handleMaterial = EnsureMaterial(MaterialFolderPath + "/AudioSeparator_FaderHandle.mat", new Color(0.22f, 0.28f, 0.34f, 1f));
            Material voiceHandleMaterial = EnsureMaterial(MaterialFolderPath + "/AudioSeparator_VoiceFaderHandle.mat", new Color(0.75f, 0.10f, 0.12f, 1f));

            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                AudioSource source = EnsureAudioSource(audioSourcesRoot, stem, stemClips.TryGetValue(stem, out AudioClip clip) ? clip : null);
                AudioStemFader fader = EnsureFader(
                    controlsRoot,
                    stem,
                    i,
                    source,
                    mixer,
                    railMaterial,
                    stem == LanAudioStemId.Voice ? voiceHandleMaterial : handleMaterial);
                faders.Add(fader);
            }

            AudioSeparatorMixerButton playButton = EnsureButton(
                controlsRoot,
                "PlayStopButton",
                AudioSeparatorMixerButtonType.PlayStop,
                mixer,
                new Vector3(-0.25f, 0.15f, -0.26f),
                EnsureMaterial(MaterialFolderPath + "/AudioSeparator_Button_Play.mat", new Color(0.08f, 0.22f, 0.62f, 1f)));
            AudioSeparatorMixerButton resetButton = EnsureButton(
                controlsRoot,
                "ResetButton",
                AudioSeparatorMixerButtonType.Reset,
                mixer,
                new Vector3(0f, 0.15f, -0.26f),
                EnsureMaterial(MaterialFolderPath + "/AudioSeparator_Button_Reset.mat", new Color(0.22f, 0.23f, 0.25f, 1f)));
            AudioSeparatorMixerButton saveButton = EnsureButton(
                controlsRoot,
                "SaveButton",
                AudioSeparatorMixerButtonType.Save,
                mixer,
                new Vector3(0.25f, 0.15f, -0.26f),
                EnsureMaterial(MaterialFolderPath + "/AudioSeparator_Button_Save.mat", new Color(0.75f, 0.03f, 0.04f, 1f)));

            MixerUiRefs uiRefs = EnsureTutorialCanvas(device.transform);

            SerializedObject serialized = new SerializedObject(mixer);
            SetObject(serialized, "missionManager", missionManager);
            SetObject(serialized, "lanRecordingController", lanController);
            SetBool(serialized, "requireMixedRecordingSaved", true);
            SetObject(serialized, "playbackController", playback);
            SetObject(serialized, "interactionPoint", interactionPoint);
            SetObject(serialized, "cameraFocusPoint", cameraFocusPoint);
            SetObject(serialized, "saveButton", saveButton);
            SetObject(serialized, "playStopButton", playButton);
            SetObject(serialized, "resetButton", resetButton);
            SetObject(serialized, "tutorialCanvas", uiRefs.Canvas);
            SetObject(serialized, "statusText", uiRefs.StatusText);
            SetObject(serialized, "progressText", uiRefs.ProgressText);
            SetObject(serialized, "hoverText", uiRefs.HoverText);
            SetObject(serialized, "tutorialText", uiRefs.TutorialText);
            SetFloat(serialized, "targetMinimum", 0.75f);
            SetFloat(serialized, "otherMaximum", 0.15f);
            SetArray(serialized, "faders", faders.ConvertAll(f => (Object)f));
            SetArray(serialized, "controlButtons", new Object[] { playButton, resetButton, saveButton });

            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                LanAudioStemId stem = LanAudioRecordingCatalog.StemOrder[i];
                AudioClip clip = stemClips.TryGetValue(stem, out AudioClip found) ? found : null;
                switch (stem)
                {
                    case LanAudioStemId.Voice:
                        SetObject(serialized, "voiceClip", clip);
                        break;
                    case LanAudioStemId.Police:
                        SetObject(serialized, "policeSirenClip", clip);
                        break;
                    case LanAudioStemId.Rain:
                        SetObject(serialized, "rainClip", clip);
                        break;
                    case LanAudioStemId.Horns:
                        SetObject(serialized, "trafficHornClip", clip);
                        break;
                    case LanAudioStemId.Wind:
                        SetObject(serialized, "windClip", clip);
                        break;
                    case LanAudioStemId.Thunder:
                        SetObject(serialized, "thunderClip", clip);
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mixer);
            EditorUtility.SetDirty(playback);

            interactable.Configure(missionManager, lanController, mixer);
            EditorUtility.SetDirty(interactable);
            report.Pass("Configured AudioSeparator_Device as an F-interactable mixer, not an inventory pickup.");
            report.Pass("Created/updated 6 AudioSources, 6 faders, Play/Reset/Save buttons, interaction point, camera focus, and mixer tutorial UI.");
            return mixer;
        }

        private static AudioSource EnsureAudioSource(Transform parent, LanAudioStemId stem, AudioClip clip)
        {
            Transform child = EnsureChild(parent, stem + "_Source");
            AudioSource source = EnsureComponent<AudioSource>(child.gameObject);
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 64;
            source.clip = clip;
            source.volume = 1f;
            EditorUtility.SetDirty(source);
            return source;
        }

        private static AudioStemFader EnsureFader(
            Transform parent,
            LanAudioStemId stem,
            int index,
            AudioSource source,
            AudioSeparatorMixerController mixer,
            Material railMaterial,
            Material handleMaterial)
        {
            Transform faderRoot = EnsureChild(parent, stem + "_Fader");
            float x = Mathf.Lerp(-0.36f, 0.36f, index / 5f);
            Vector3 faderLocalPosition = new Vector3(x, 0.16f, 0.02f);
            faderRoot.localPosition = faderLocalPosition;
            faderRoot.localRotation = Quaternion.identity;
            faderRoot.localScale = Vector3.one;

            GameObject rail = EnsureMarkerCube(faderRoot, "Rail", railMaterial);
            rail.transform.localPosition = Vector3.zero;
            rail.transform.localRotation = Quaternion.identity;
            rail.transform.localScale = new Vector3(0.035f, 0.012f, 0.20f);

            GameObject handle = EnsureMarkerCube(faderRoot, "Handle", handleMaterial);
            handle.transform.localPosition = new Vector3(0f, 0.028f, 0.075f);
            handle.transform.localRotation = Quaternion.identity;
            handle.transform.localScale = new Vector3(0.055f, 0.035f, 0.045f);

            BoxCollider collider = EnsureComponent<BoxCollider>(faderRoot.gameObject);
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.02f, 0f);
            collider.size = new Vector3(0.09f, 0.08f, 0.24f);

            AudioStemFader fader = EnsureComponent<AudioStemFader>(faderRoot.gameObject);
            SerializedObject serialized = new SerializedObject(fader);
            SetObject(serialized, "handleTransform", handle.transform);
            SetObject(serialized, "interactionCollider", collider);
            SetObject(serialized, "highlightRenderer", handle.GetComponent<Renderer>());
            SetObject(serialized, "audioSource", source);
            SetObject(serialized, "mixerController", mixer);
            SetEnum(serialized, "stemId", (int)stem);
            SetString(serialized, "displayName", LanAudioRecordingCatalog.GetStemDisplayName(stem));
            SetBool(serialized, "isVoiceFader", stem == LanAudioStemId.Voice);
            SetVector3(serialized, "localMinPosition", new Vector3(0f, 0.028f, -0.075f));
            SetVector3(serialized, "localMaxPosition", new Vector3(0f, 0.028f, 0.075f));
            SetFloat(serialized, "normalizedValue", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            faderRoot.localPosition = faderLocalPosition;
            fader.Configure(mixer, stem, LanAudioRecordingCatalog.GetStemDisplayName(stem), source, stem == LanAudioStemId.Voice);
            fader.SetNormalizedValue(1f);
            fader.SetClip(source != null ? source.clip : null);
            EditorUtility.SetDirty(fader);
            return fader;
        }

        private static AudioSeparatorMixerButton EnsureButton(
            Transform parent,
            string name,
            AudioSeparatorMixerButtonType type,
            AudioSeparatorMixerController mixer,
            Vector3 localPosition,
            Material material)
        {
            GameObject buttonObject = EnsureMarkerCube(parent, name, material);
            buttonObject.transform.localPosition = localPosition;
            buttonObject.transform.localRotation = Quaternion.identity;
            buttonObject.transform.localScale = new Vector3(0.13f, 0.045f, 0.10f);

            BoxCollider collider = EnsureComponent<BoxCollider>(buttonObject);
            collider.isTrigger = true;
            AudioSeparatorMixerButton button = EnsureComponent<AudioSeparatorMixerButton>(buttonObject);
            button.Configure(mixer, type);

            SerializedObject serialized = new SerializedObject(button);
            SetEnum(serialized, "buttonType", (int)type);
            SetObject(serialized, "mixerController", mixer);
            SetObject(serialized, "buttonTransform", buttonObject.transform);
            SetObject(serialized, "highlightRenderer", buttonObject.GetComponent<Renderer>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(button);
            return button;
        }

        private static MixerUiRefs EnsureTutorialCanvas(Transform parent)
        {
            RectTransform canvasRect = EnsureRectTransform(EnsureChild(parent, "MixerTutorialPanel").gameObject);
            Transform canvasTransform = canvasRect.transform;
            Canvas canvas = EnsureComponent<Canvas>(canvasTransform.gameObject);
            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasTransform.gameObject);
            EnsureComponent<GraphicRaycaster>(canvasTransform.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 650;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            RectTransform panel = EnsureUiPanel(canvasTransform, "Panel", new Vector2(620f, 300f), new Vector2(0f, -330f));
            TextMeshProUGUI status = EnsureUiText(panel, "StatusText", 26f, TextAlignmentOptions.TopLeft);
            SetInset(status.rectTransform, 22f, 190f, 22f, 20f);
            TextMeshProUGUI progress = EnsureUiText(panel, "ProgressText", 19f, TextAlignmentOptions.TopLeft);
            SetInset(progress.rectTransform, 22f, 18f, 332f, 90f);
            TextMeshProUGUI tutorial = EnsureUiText(panel, "TutorialText", 18f, TextAlignmentOptions.TopLeft);
            SetInset(tutorial.rectTransform, 310f, 18f, 22f, 90f);
            TextMeshProUGUI hover = EnsureUiText(panel, "HoverText", 20f, TextAlignmentOptions.Center);
            SetInset(hover.rectTransform, 22f, 150f, 22f, 118f);

            canvas.gameObject.SetActive(false);
            return new MixerUiRefs(canvas, status, progress, hover, tutorial);
        }

        private static void ConnectPhoneControllers(
            Scene scene,
            Mission01AudioSeparatorManager missionManager,
            AudioSeparatorMixerController mixer,
            MixerSetupReport report)
        {
            PhoneUIController[] phones = FindSceneComponents<PhoneUIController>(scene);
            for (int i = 0; i < phones.Length; i++)
            {
                SerializedObject serialized = new SerializedObject(phones[i]);
                SetObject(serialized, "firstMissionManager", missionManager);
                SetObject(serialized, "mixerController", mixer);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(phones[i]);
            }

            if (phones.Length > 0)
            {
                report.Pass($"Connected {phones.Length} PhoneUIController instance(s) to Mission 01 manager and mixer.");
            }
            else
            {
                report.Warning("No PhoneUIController found in scene.");
            }
        }

        private static void MoveLegacyAssets(MixerSetupReport report)
        {
            EnsureFolder(LegacyBackupFolder);
            MoveLegacyAsset(LanAudioRecordingCatalog.OldLanRecordingPath, report);
            MoveLegacyAsset(LanAudioRecordingCatalog.OldLanVoicePath, report);
            MoveLegacyAsset(LanAudioRecordingCatalog.OldPoliceSirenPath, report);
        }

        private static void MoveLegacyAsset(string oldPath, MixerSetupReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) == null)
            {
                return;
            }

            string destinationPath = LegacyBackupFolder + "/" + Path.GetFileName(oldPath);
            if (AssetDatabase.LoadAssetAtPath<Object>(destinationPath) != null)
            {
                report.Warning($"Legacy asset already exists in backup folder, leaving original in place: {oldPath}.");
                return;
            }

            string error = AssetDatabase.MoveAsset(oldPath, destinationPath);
            if (string.IsNullOrWhiteSpace(error))
            {
                report.Pass($"Moved legacy audio asset to backup with AssetDatabase.MoveAsset: {oldPath} -> {destinationPath}.");
            }
            else
            {
                report.Warning($"Could not move legacy audio asset {oldPath}: {error}");
            }
        }

        private static void ValidateAudioAssets(MixerSetupReport report)
        {
            AudioClip mixedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanAudioRecordingCatalog.MixedPath);
            if (mixedClip == null)
            {
                report.Fail($"Missing mixed AudioClip: {LanAudioRecordingCatalog.MixedPath}.");
            }
            else
            {
                report.Pass($"Found mixed AudioClip: {LanAudioRecordingCatalog.MixedPath}.");
            }

            for (int i = 0; i < LanAudioRecordingCatalog.StemOrder.Length; i++)
            {
                string path = LanAudioRecordingCatalog.GetStemPath(LanAudioRecordingCatalog.StemOrder[i]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    report.Fail($"Missing stem AudioClip: {path}.");
                    continue;
                }

                report.Pass($"Found stem AudioClip: {path}.");
                ValidateAudioImporter(path, report);
                if (mixedClip != null && Mathf.Abs(clip.length - mixedClip.length) > 0.1f)
                {
                    report.Warning($"{path} duration differs from mixed clip by {Mathf.Abs(clip.length - mixedClip.length):0.000}s.");
                }
            }
        }

        private static void ValidateAudioImporter(string path, MixerSetupReport report)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                report.Fail($"No AudioImporter found for {path}.");
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool valid = !importer.forceToMono &&
                settings.preloadAudioData &&
                !importer.loadInBackground &&
                settings.loadType == AudioClipLoadType.DecompressOnLoad &&
                settings.compressionFormat == AudioCompressionFormat.PCM;
            if (valid)
            {
                report.Pass($"Audio import settings valid: {path}.");
            }
            else
            {
                report.Fail($"Audio import settings invalid: {path}. Expected stereo allowed, preload, no background load, DecompressOnLoad, PCM.");
            }
        }

        private static void ValidateVoiceMessage(MixerSetupReport report)
        {
            PhoneMessageData message = AssetDatabase.LoadAssetAtPath<PhoneMessageData>(MessageAssetPath);
            AudioClip mixedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanAudioRecordingCatalog.MixedPath);
            if (message == null)
            {
                report.Fail($"Missing voice message data: {MessageAssetPath}.");
                return;
            }

            if (message.AudioClip == mixedClip &&
                string.Equals(message.MessageId, LanAudioRecordingCatalog.MixedRecordingId, StringComparison.Ordinal) &&
                message.MessageType == PhoneMessageType.Audio)
            {
                report.Pass("Chị Lan Messenger voice message is configured to play the mixed recording only.");
            }
            else
            {
                report.Fail("Chị Lan Messenger voice message is not configured to use Lan_LastRecording_Mixed.");
            }
        }

        private static void ValidateScene(Scene scene, MixerSetupReport report)
        {
            GameObject device = FindSceneObject(scene, DeviceObjectName);
            if (device == null)
            {
                report.Fail($"{DeviceObjectName} missing from scene.");
                return;
            }

            report.Pass($"{DeviceObjectName} exists in scene.");
            if (device.GetComponent<ItemPickup>() == null && device.GetComponent<WorldPickupPersistence>() == null)
            {
                report.Pass("AudioSeparator_Device is not configured as an inventory pickup.");
            }
            else
            {
                report.Fail("AudioSeparator_Device still has pickup components.");
            }

            AudioSeparatorMixerController mixer = device.GetComponent<AudioSeparatorMixerController>();
            Mission01AudioSeparatorDeviceInteractable interactable = device.GetComponent<Mission01AudioSeparatorDeviceInteractable>();
            report.Add(mixer != null, "AudioSeparatorMixerController exists on device.");
            report.Add(interactable != null, "Mission01AudioSeparatorDeviceInteractable exists on device.");

            if (mixer != null)
            {
                AudioStemFader[] faders = device.GetComponentsInChildren<AudioStemFader>(true);
                AudioSource[] sources = device.GetComponentsInChildren<AudioSource>(true);
                AudioSeparatorMixerButton[] buttons = device.GetComponentsInChildren<AudioSeparatorMixerButton>(true);
                report.Add(faders.Length >= LanAudioRecordingCatalog.StemCount, $"Mixer has at least 6 faders ({faders.Length}).");
                report.Add(sources.Length >= LanAudioRecordingCatalog.StemCount, $"Mixer has at least 6 AudioSources ({sources.Length}).");
                report.Add(buttons.Length >= 3, $"Mixer has Play/Reset/Save buttons ({buttons.Length}).");
                report.Add(mixer.TargetMinimum >= 0.74f && mixer.OtherMaximum <= 0.16f, "Mixer isolation thresholds are configured.");
            }

            int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(device);
            report.Add(missingScripts == 0, $"Device has no missing scripts ({missingScripts}).");
        }

        private static void SaveAudioSeparatorPrefab(GameObject device, MixerSetupReport report)
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace("\\", "/"));
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(device, PrefabPath);
            if (saved != null)
            {
                report.Pass($"Created/updated prefab: {PrefabPath}.");
            }
            else
            {
                report.Warning($"Could not save prefab: {PrefabPath}.");
            }
        }

        private static void EnsureTriggerCollider(GameObject device, MixerSetupReport report)
        {
            Collider collider = device.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider box = device.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, 0.12f, 0f);
                box.size = new Vector3(0.9f, 0.35f, 0.7f);
                report.Warning("AudioSeparator_Device had no Collider. Added a trigger BoxCollider for F interaction.");
                return;
            }

            collider.isTrigger = true;
            report.Pass("AudioSeparator_Device Collider is trigger-only.");
        }

        private static GameObject EnsureMarkerCube(Transform parent, string name, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = name;
            target.transform.SetParent(parent, false);
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            return target;
        }

        private static RectTransform EnsureUiPanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            RectTransform rect = EnsureRectTransform(EnsureChild(parent, name).gameObject);
            Image image = EnsureComponent<Image>(rect.gameObject);
            image.color = new Color(0.045f, 0.047f, 0.055f, 0.88f);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        private static TextMeshProUGUI EnsureUiText(Transform parent, string name, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = EnsureRectTransform(EnsureChild(parent, name).gameObject);
            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
            text.fontSize = size;
            text.color = new Color(0.92f, 0.90f, 0.84f, 1f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                return rect;
            }

            Transform parent = target.transform.parent;
            int siblingIndex = target.transform.GetSiblingIndex();
            string name = target.name;
            Object.DestroyImmediate(target);
            GameObject replacement = new GameObject(name, typeof(RectTransform));
            replacement.transform.SetParent(parent, false);
            replacement.transform.SetSiblingIndex(siblingIndex);
            return replacement.GetComponent<RectTransform>();
        }

        private static void SetInset(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemoveComponentIfPresent<T>(GameObject target, MixerSetupReport report) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                return;
            }

            Object.DestroyImmediate(component);
            report.Pass($"Removed legacy pickup component: {typeof(T).Name}.");
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static string FindAudioPathByFileName(string fileName)
        {
            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName), new[] { LanAudioRecordingCatalog.AudioFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");
                if (string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        private static Scene OpenTargetScene(MixerSetupReport report, bool showDialog)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && string.Equals(active.path, ScenePath, StringComparison.Ordinal))
            {
                return active;
            }

            if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Warning("Scene switch cancelled by user.");
                return active;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T FindFirstSceneComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Chapter1/Documentation");
            EnsureFolder(MaterialFolderPath);
            EnsureFolder(LegacyBackupFolder);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
            }

            string name = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
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

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static void SetEnum(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
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

        private static void SetArray(SerializedObject serialized, string propertyName, IList<Object> values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void Finish(MixerSetupReport report, bool showDialog, string title)
        {
            WriteReport(report);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(title, report.Summary, "OK");
            }
        }

        private static void WriteReport(MixerSetupReport report)
        {
            EnsureFolder("Assets/Chapter1/Documentation");
            File.WriteAllText(ReportPath, report.ToMarkdown(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
        }

        private readonly struct MixerUiRefs
        {
            public MixerUiRefs(Canvas canvas, TextMeshProUGUI statusText, TextMeshProUGUI progressText, TextMeshProUGUI hoverText, TextMeshProUGUI tutorialText)
            {
                Canvas = canvas;
                StatusText = statusText;
                ProgressText = progressText;
                HoverText = hoverText;
                TutorialText = tutorialText;
            }

            public Canvas Canvas { get; }
            public TextMeshProUGUI StatusText { get; }
            public TextMeshProUGUI ProgressText { get; }
            public TextMeshProUGUI HoverText { get; }
            public TextMeshProUGUI TutorialText { get; }
        }

        public sealed class MixerSetupReport
        {
            private readonly string title;
            private readonly List<string> lines = new List<string>();

            public MixerSetupReport(string title)
            {
                this.title = title;
            }

            public int PassCount { get; private set; }
            public int WarningCount { get; private set; }
            public int FailCount { get; private set; }
            public string Summary => $"PASS: {PassCount} | WARNING: {WarningCount} | FAIL: {FailCount}";

            public void Add(bool condition, string message)
            {
                if (condition)
                {
                    Pass(message);
                }
                else
                {
                    Fail(message);
                }
            }

            public void Pass(string message)
            {
                PassCount++;
                lines.Add("[PASS] " + message);
            }

            public void Warning(string message)
            {
                WarningCount++;
                lines.Add("[WARNING] " + message);
            }

            public void Fail(string message)
            {
                FailCount++;
                lines.Add("[FAIL] " + message);
            }

            public void Info(string message)
            {
                lines.Add("[INFO] " + message);
            }

            public string ToMarkdown()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# " + title);
                builder.AppendLine();
                for (int i = 0; i < lines.Count; i++)
                {
                    builder.AppendLine(lines[i]);
                }

                builder.AppendLine(Summary);
                return builder.ToString();
            }

            public string ToConsoleString()
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("[" + title + "]");
                for (int i = 0; i < lines.Count; i++)
                {
                    builder.AppendLine(lines[i]);
                }

                builder.AppendLine(Summary);
                return builder.ToString();
            }
        }
    }
}
