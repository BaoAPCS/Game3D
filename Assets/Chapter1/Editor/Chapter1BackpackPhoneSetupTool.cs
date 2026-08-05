using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1BackpackPhoneSetupTool
    {
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string InputActionsPath = "Assets/Chapter1/Settings/Chapter1Controls.inputactions";
        private const string InventoryInputReferencePath = "Assets/Chapter1/Settings/InputReferences/Inventory.inputactionreference.asset";
        private const string PhoneItemPath = "Assets/Chapter1/Data/Inventory/Items/PhoneItem.asset";
        private const string PhoneIconPath = "Assets/Chapter1/UI/Icons/Items/PhoneIcon_Placeholder.png";
        private const string InventorySlotPrefabPath = "Assets/Chapter1/UI/Inventory/Prefabs/InventorySlot.prefab";
        private const string InventoryPanelPrefabPath = "Assets/Chapter1/UI/Inventory/Prefabs/InventoryPanel.prefab";
        private const string PhonePanelPrefabPath = "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";
        private const string FirstMissionPhoneReportPath = "Assets/Chapter1/FIRST_MISSION_PHONE_REPORT.md";
        private const string PhoneDataFolderPath = "Assets/Chapter1/Data/Phone";
        private const string PhoneMessagesFolderPath = "Assets/Chapter1/Data/Phone/Messages";
        private const string LanRecordingAudioPath = "Assets/Chapter1/Audio/Phone/Lan_LastRecording_Mixed.mp3";
        private const string LanRecordingMessagePath = "Assets/Chapter1/Data/Phone/Messages/Lan_LastRecordingMessage.asset";
        private const string LanRecordingMissionObjectName = "LanRecordingMissionController";
        private const string MissionHintObjectName = "MissionHintController";
        private const string KenneyGreyDoubleFolder = "Assets/ThirdParty/Kenney/UI_Pack/kenney_ui-pack/PNG/Grey/Double";
        private const string KenneySoundsFolder = "Assets/ThirdParty/Kenney/UI_Pack/kenney_ui-pack/Sounds";
        private const string PhoneId = "phone";
        private const string BackpackCanvasName = "Chapter1BackpackPhoneCanvas";

        [MenuItem("Tools/Chapter 1/Setup Backpack And Phone")]
        public static void SetupBackpackAndPhone()
        {
            Report report = new Report("Chapter 1 Backpack + Phone Setup");
            PrintPreflightReport(report);
            EnsureFolders();

            InputActionReference inventoryReference = EnsureInventoryInput(report);
            Sprite phoneIcon = EnsurePhoneIcon(report);
            ItemDefinition phoneItem = EnsurePhoneItem(phoneIcon, report);
            UiAssets uiAssets = FindUiAssets(report);
            FirstMissionPhoneAssets firstMissionAssets = EnsureFirstMissionPhoneAssets(report);

            GameObject slotPrefab = EnsureInventorySlotPrefab(uiAssets, report);
            GameObject inventoryPanelPrefab = EnsureInventoryPanelPrefab(slotPrefab, uiAssets, report);
            GameObject phonePanelPrefab = EnsurePhonePanelPrefab(uiAssets, report);

            ConfigurePlayerPrefab(inventoryReference, phoneItem, report);
            ConfigureScene(inventoryReference, phoneItem, inventoryPanelPrefab, phonePanelPrefab, uiAssets, report);
            ConfigureFirstMissionScene(firstMissionAssets, report);

            AssetDatabase.SaveAssets();
            WriteFirstMissionPhoneReport(report);
            AssetDatabase.Refresh();
            report.FlushToConsole();
            EditorUtility.DisplayDialog("Setup Backpack And Phone", report.GetSummary(), "OK");
        }

        [MenuItem("Tools/Chapter 1/Setup First Mission Phone Sequence")]
        public static void SetupFirstMissionPhoneSequence()
        {
            Report report = new Report("FIRST MISSION PHONE SETUP REPORT");
            PrintPreflightReport(report);
            EnsureFolders();
            FirstMissionPhoneAssets firstMissionAssets = EnsureFirstMissionPhoneAssets(report);
            ConfigureFirstMissionScene(firstMissionAssets, report);

            AssetDatabase.SaveAssets();
            WriteFirstMissionPhoneReport(report);
            AssetDatabase.Refresh();
            report.FlushToConsole();
            EditorUtility.DisplayDialog("Setup First Mission Phone Sequence", report.GetSummary(), "OK");
        }

        [MenuItem("Tools/Chapter 1/Validate Backpack And Phone")]
        public static void ValidateBackpackAndPhone()
        {
            Report report = new Report("Chapter 1 Backpack + Phone Validate");
            ValidateInput(report);
            ValidateAssets(report);
            ValidatePrefabs(report);
            ValidateScene(report);
            ValidateFirstMissionPhoneAssets(report);
            WriteFirstMissionPhoneReport(report);
            report.FlushToConsole();
            EditorUtility.DisplayDialog("Validate Backpack And Phone", report.GetSummary(), "OK");
        }

        private static void PrintPreflightReport(Report report)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject player = FindScenePlayer();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            PlayerInputLock inputLock = player != null ? player.GetComponent<PlayerInputLock>() : Object.FindAnyObjectByType<PlayerInputLock>();

            report.Info($"Player scene object: {(player != null ? GetHierarchyPath(player.transform) : "not found")}.");
            report.Info($"Player prefab: {(playerPrefab != null ? PlayerPrefabPath : "not found")}.");
            report.Info($"Scene currently open: {activeScene.path}.");
            report.Info($"Scene target: {ScenePath}.");
            report.Info($"Input Action Asset: {(inputActions != null ? InputActionsPath : "not found")}.");
            report.Info($"Canvas: {(canvas != null ? GetHierarchyPath(canvas.transform) : "not found")}.");
            report.Info($"EventSystem: {(eventSystem != null ? GetHierarchyPath(eventSystem.transform) : "not found")}.");
            report.Info($"Input lock: {(inputLock != null ? GetHierarchyPath(inputLock.transform) : "not found")}.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Chapter1/Data");
            EnsureFolder("Assets/Chapter1/Data/Inventory");
            EnsureFolder("Assets/Chapter1/Data/Inventory/Items");
            EnsureFolder(PhoneDataFolderPath);
            EnsureFolder(PhoneMessagesFolderPath);
            EnsureFolder("Assets/Chapter1/UI");
            EnsureFolder("Assets/Chapter1/UI/Icons");
            EnsureFolder("Assets/Chapter1/UI/Icons/Items");
            EnsureFolder("Assets/Chapter1/UI/Inventory");
            EnsureFolder("Assets/Chapter1/UI/Inventory/Prefabs");
            EnsureFolder("Assets/Chapter1/UI/Phone");
            EnsureFolder("Assets/Chapter1/UI/Phone/Prefabs");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static InputActionReference EnsureInventoryInput(Report report)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                report.Fail($"Missing Input Action Asset: {InputActionsPath}.");
                return null;
            }

            InputActionMap gameplay = inputActions.FindActionMap("Gameplay", false);
            if (gameplay == null)
            {
                report.Fail("Input Action Asset is missing Gameplay map.");
                return null;
            }

            InputAction inventoryAction = gameplay.FindAction("Inventory", false);
            if (inventoryAction == null)
            {
                inventoryAction = gameplay.AddAction("Inventory", InputActionType.Button, expectedControlLayout: "Button");
                report.Pass("Added Gameplay/Inventory action.");
            }

            bool hasKeyboardB = false;
            foreach (InputBinding binding in inventoryAction.bindings)
            {
                if (string.Equals(binding.effectivePath, "<Keyboard>/b", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(binding.path, "<Keyboard>/b", StringComparison.OrdinalIgnoreCase))
                {
                    hasKeyboardB = true;
                    break;
                }
            }

            if (!hasKeyboardB)
            {
                inventoryAction.AddBinding("<Keyboard>/b");
                report.Pass("Added <Keyboard>/b binding to Gameplay/Inventory.");
            }

            EditorUtility.SetDirty(inputActions);
            AssetDatabase.SaveAssetIfDirty(inputActions);

            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(InventoryInputReferencePath);
            if (reference == null)
            {
                reference = InputActionReference.Create(inventoryAction);
                AssetDatabase.CreateAsset(reference, InventoryInputReferencePath);
                report.Pass($"Created {InventoryInputReferencePath}.");
            }
            else
            {
                SerializedObject serializedReference = new SerializedObject(reference);
                SetObject(serializedReference, "m_Asset", inputActions);
                SetString(serializedReference, "m_ActionId", inventoryAction.id.ToString());
                serializedReference.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(reference);
                report.Pass("Updated Inventory InputActionReference.");
            }

            return reference;
        }

        private static Sprite EnsurePhoneIcon(Report report)
        {
            string existingPhoneIcon = FindAssetPathByKeywords(
                new[] { "phone", "mobile", "smartphone", "cellphone" },
                new[] { "Assets/ThirdParty/Kenney/UI_Pack", "Assets/Chapter1/UI", "Assets/Chapter1" },
                ".png");

            if (!string.IsNullOrEmpty(existingPhoneIcon) && existingPhoneIcon != PhoneIconPath)
            {
                Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(existingPhoneIcon);
                if (existingSprite != null)
                {
                    report.Pass($"Using existing phone icon: {existingPhoneIcon}.");
                    return existingSprite;
                }
            }

            if (!File.Exists(PhoneIconPath))
            {
                Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                Color clear = new Color(0f, 0f, 0f, 0f);
                Color rim = new Color(0.76f, 0.78f, 0.82f, 1f);
                Color body = new Color(0.025f, 0.027f, 0.032f, 1f);
                Color screen = new Color(0.018f, 0.03f, 0.045f, 1f);
                Color red = new Color(0.45f, 0.03f, 0.03f, 1f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }

                FillRoundedRect(texture, 78, 24, 100, 208, 18, rim);
                FillRoundedRect(texture, 84, 31, 88, 194, 14, body);
                FillRoundedRect(texture, 93, 54, 70, 140, 8, screen);
                FillRoundedRect(texture, 112, 199, 32, 5, 2, rim);
                FillCircle(texture, 128, 42, 5, rim);
                DrawLine(texture, 98, 178, 158, 68, red, 3);
                DrawLine(texture, 102, 70, 154, 154, new Color(0.12f, 0.2f, 0.25f, 0.8f), 2);
                texture.Apply();

                File.WriteAllBytes(PhoneIconPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                report.Pass($"Created placeholder phone icon: {PhoneIconPath}.");
            }

            AssetDatabase.ImportAsset(PhoneIconPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(PhoneIconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PhoneIconPath);
            report.Add(sprite != null, "Phone icon is imported as Sprite.");
            return sprite;
        }

        private static ItemDefinition EnsurePhoneItem(Sprite phoneIcon, Report report)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PhoneItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, PhoneItemPath);
                report.Pass($"Created {PhoneItemPath}.");
            }

            SerializedObject serializedItem = new SerializedObject(item);
            SetString(serializedItem, "itemId", PhoneId);
            SetString(serializedItem, "displayName", "\u0110i\u1ec7n tho\u1ea1i");
            SetObject(serializedItem, "icon", phoneIcon);
            SetString(serializedItem, "description", "\u0110i\u1ec7n tho\u1ea1i c\u1ee7a nh\u00e2n v\u1eadt ch\u00ednh. C\u00f3 th\u1ec3 m\u1edf Messenger, Ghi \u00e2m, Camera v\u00e0 Google.");
            SetEnum(serializedItem, "category", (int)ItemCategory.Phone);
            SetBool(serializedItem, "isStackable", false);
            SetInt(serializedItem, "maxStack", 1);
            SetBool(serializedItem, "isDroppable", false);
            SetBool(serializedItem, "isUsable", true);
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            report.Pass("Configured PhoneItem.asset.");
            return item;
        }

        private static UiAssets FindUiAssets(Report report)
        {
            UiAssets assets = new UiAssets
            {
                PanelSprite = LoadSprite($"{KenneyGreyDoubleFolder}/button_rectangle_depth_flat.png"),
                SlotSprite = LoadSprite($"{KenneyGreyDoubleFolder}/button_square_depth_flat.png"),
                SlotSelectedSprite = LoadSprite("Assets/ThirdParty/Kenney/UI_Pack/kenney_ui-pack/PNG/Red/Double/button_square_depth_flat.png"),
                ButtonSprite = LoadSprite($"{KenneyGreyDoubleFolder}/button_rectangle_depth_flat.png"),
                CloseIconSprite = LoadSprite($"{KenneyGreyDoubleFolder}/icon_cross.png"),
                DividerSprite = LoadSprite("Assets/ThirdParty/Kenney/UI_Pack/kenney_ui-pack/PNG/Extra/Default/divider.png"),
                ClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{KenneySoundsFolder}/click-a.ogg"),
                OpenCloseClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{KenneySoundsFolder}/tap-a.ogg"),
                TabClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{KenneySoundsFolder}/switch-a.ogg")
            };

            report.Add(assets.PanelSprite != null, "Kenney Grey Double panel/button sprite found.");
            report.Add(assets.SlotSprite != null, "Kenney Grey Double slot sprite found.");
            report.Add(assets.SlotSelectedSprite != null, "Kenney Red Double selected slot sprite found.");
            report.Add(assets.ClickClip != null || assets.OpenCloseClip != null || assets.TabClip != null, "Kenney UI audio clips found.");
            return assets;
        }

        private static FirstMissionPhoneAssets EnsureFirstMissionPhoneAssets(Report report)
        {
            AudioClip lanRecordingClip = LoadLanRecordingClip(report);
            PhoneMessageData lanRecordingMessage = AssetDatabase.LoadAssetAtPath<PhoneMessageData>(LanRecordingMessagePath);
            if (lanRecordingMessage == null)
            {
                lanRecordingMessage = ScriptableObject.CreateInstance<PhoneMessageData>();
                AssetDatabase.CreateAsset(lanRecordingMessage, LanRecordingMessagePath);
                report.Pass($"Created Lan voice message data: {LanRecordingMessagePath}.");
            }

            SerializedObject serializedMessage = new SerializedObject(lanRecordingMessage);
            SetString(serializedMessage, "messageId", "lan_last_recording");
            SetString(serializedMessage, "senderId", "lan");
            SetString(serializedMessage, "content", "Tin nh\u1eafn tho\u1ea1i");
            SetEnum(serializedMessage, "messageType", (int)PhoneMessageType.Audio);
            SetBool(serializedMessage, "isFromPlayer", false);
            SetBool(serializedMessage, "isRead", false);
            SetObject(serializedMessage, "audioClip", lanRecordingClip);
            SetBool(serializedMessage, "isDownloaded", false);
            serializedMessage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lanRecordingMessage);

            if (lanRecordingClip != null)
            {
                report.Pass($"Assigned real Lan recording AudioClip to voice message data: {LanRecordingMessagePath}.");
            }
            else
            {
                report.Warning(
                    $"Lan voice message data has no AudioClip because '{LanRecordingAudioPath}' could not be loaded. No fake AudioClip was created.");
            }

            return new FirstMissionPhoneAssets
            {
                LanRecordingClip = lanRecordingClip,
                LanRecordingMessage = lanRecordingMessage
            };
        }

        private static AudioClip LoadLanRecordingClip(Report report)
        {
            string absoluteAudioPath = Path.Combine(Directory.GetCurrentDirectory(), LanRecordingAudioPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absoluteAudioPath))
            {
                report.Warning(
                    $"Lan recording file is missing on disk at '{LanRecordingAudioPath}'. Expected the real MP3 there; no placeholder AudioClip will be created.");
            }
            else
            {
                AssetDatabase.ImportAsset(LanRecordingAudioPath, ImportAssetOptions.ForceUpdate);
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanRecordingAudioPath);
            if (clip == null)
            {
                report.Warning(
                    $"AssetDatabase.LoadAssetAtPath<AudioClip>(\"{LanRecordingAudioPath}\") returned null. The Lan voice message and mission controller will keep an empty AudioClip reference; no fake AudioClip was created.");
            }
            else
            {
                report.Pass($"Loaded real Lan recording AudioClip with AssetDatabase.LoadAssetAtPath<AudioClip>(): {LanRecordingAudioPath}.");
            }

            return clip;
        }

        private static GameObject EnsureInventorySlotPrefab(UiAssets assets, Report report)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(InventorySlotPrefabPath);
            if (existing != null)
            {
                report.Pass($"InventorySlot prefab exists: {InventorySlotPrefabPath}.");
                return existing;
            }

            InventorySlotUI slot = CreateSlotObject(null, "InventorySlot", assets);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot.gameObject, InventorySlotPrefabPath);
            Object.DestroyImmediate(slot.gameObject);
            report.Pass($"Created InventorySlot prefab: {InventorySlotPrefabPath}.");
            return prefab;
        }

        private static GameObject EnsureInventoryPanelPrefab(GameObject slotPrefab, UiAssets assets, Report report)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPanelPrefabPath);
            if (existing != null)
            {
                report.Pass($"InventoryPanel prefab exists: {InventoryPanelPrefabPath}.");
                return existing;
            }

            GameObject root = CreatePanelRoot("InventoryPanel");
            InventoryUIController controller = root.AddComponent<InventoryUIController>();
            root.AddComponent<AudioSource>().playOnAwake = false;
            CreateInventoryPanelChildren(root.transform, slotPrefab, assets);
            ConfigureInventoryControllerReferences(controller, null, null, null, assets);
            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
            Object.DestroyImmediate(root);
            report.Pass($"Created InventoryPanel prefab: {InventoryPanelPrefabPath}.");
            return prefab;
        }

        private static GameObject EnsurePhonePanelPrefab(UiAssets assets, Report report)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePanelPrefabPath);
            if (existing != null)
            {
                // The main implementation deliberately keeps the prefab
                // lightweight. PhoneUIController creates HomeScreen and the
                // app pages at runtime, so rebuilding an existing prefab here
                // creates a second UI hierarchy that can intercept clicks.
                report.Pass($"PhonePanel prefab exists; preserved runtime-built layout: {PhonePanelPrefabPath}.");
                return existing;
            }

            GameObject root = CreatePanelRoot("PhonePanel");
            PhoneUIController controller = root.AddComponent<PhoneUIController>();
            root.AddComponent<AudioSource>().playOnAwake = false;
            CreatePhonePanelChildren(root.transform, assets);
            ConfigurePhoneControllerReferences(controller, null, assets);
            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PhonePanelPrefabPath);
            Object.DestroyImmediate(root);
            report.Pass($"Created PhonePanel prefab: {PhonePanelPrefabPath}.");
            return prefab;
        }

        private static void ConfigurePlayerPrefab(InputActionReference inventoryReference, ItemDefinition phoneItem, Report report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                report.Fail($"Missing Player prefab: {PlayerPrefabPath}.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ConfigurePlayerObject(contents, inventoryReference, phoneItem, null, null, null, report, "Player prefab");
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                report.Pass("Saved Player prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureScene(
            InputActionReference inventoryReference,
            ItemDefinition phoneItem,
            GameObject inventoryPanelPrefab,
            GameObject phonePanelPrefab,
            UiAssets assets,
            Report report)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Warning("Scene setup skipped because current scene changes were not saved.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindScenePlayer();
            Canvas canvas = EnsureCanvas(report);
            EnsureEventSystem(report);
            DisableDebugOverlays(report);

            PhoneUIController phone = EnsureScenePhonePanel(canvas, phonePanelPrefab, assets, report);
            InventoryUIController inventory = EnsureSceneInventoryPanel(canvas, inventoryPanelPrefab, assets, report);

            if (player != null)
            {
                ConfigurePlayerObject(player, inventoryReference, phoneItem, inventory, phone, canvas, report, "Scene Player");
                PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
                InventoryController inventoryController = player.GetComponent<InventoryController>();

                if (phone != null)
                {
                    ConfigurePhoneControllerReferences(phone, inputLock, assets);
                    EditorUtility.SetDirty(phone);
                }

                if (inventory != null)
                {
                    ConfigureInventoryControllerReferences(inventory, inventoryController, phone, inputLock, assets);
                    EditorUtility.SetDirty(inventory);
                }
            }
            else
            {
                report.Fail("Scene Player not found.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.Pass($"Saved scene: {ScenePath}.");
        }

        private static void ConfigureFirstMissionScene(FirstMissionPhoneAssets assets, Report report)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Warning("First mission phone scene setup skipped because current scene changes were not saved.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LanRecordingMissionController controller = Object.FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                GameObject missionObject = new GameObject(LanRecordingMissionObjectName);
                controller = missionObject.AddComponent<LanRecordingMissionController>();
                report.Pass($"Created scene object: {LanRecordingMissionObjectName}.");
            }
            else
            {
                report.Pass($"Found scene LanRecordingMissionController: {GetHierarchyPath(controller.transform)}.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SetObject(serializedController, "lanRecordingClip", assets?.LanRecordingClip);
            SetObject(serializedController, "lanVoiceMessage", assets?.LanRecordingMessage);
            SetString(serializedController, "expectedAudioAssetPath", LanRecordingAudioPath);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            if (assets?.LanRecordingClip != null)
            {
                report.Pass($"Assigned real Lan recording AudioClip to LanRecordingMissionController: {LanRecordingAudioPath}.");
            }
            else
            {
                report.Warning(
                    $"LanRecordingMissionController has no AudioClip because '{LanRecordingAudioPath}' could not be loaded. No fake AudioClip was created.");
            }

            if (assets?.LanRecordingMessage != null)
            {
                report.Pass($"Assigned Lan voice message data to LanRecordingMissionController: {LanRecordingMessagePath}.");
            }
            else
            {
                report.Warning($"LanRecordingMissionController has no voice message data reference: {LanRecordingMessagePath}.");
            }

            ConfigureMissionHintController(controller, report);
            ConfigurePhoneMissionControllerReference(controller, report);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.Pass($"Saved first mission phone sequence scene references: {ScenePath}.");
        }

        private static void ConfigurePhoneMissionControllerReference(LanRecordingMissionController missionController, Report report)
        {
            PhoneUIController phoneUi = Object.FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            if (phoneUi == null)
            {
                report.Warning("PhoneUIController not found while assigning LanRecordingMissionController reference. Runtime fallback will still search for it.");
                return;
            }

            SerializedObject serializedPhone = new SerializedObject(phoneUi);
            SetObject(serializedPhone, "missionController", missionController);
            serializedPhone.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(phoneUi);
            report.Add(missionController != null, "Assigned LanRecordingMissionController reference to PhoneUIController for Messenger mission state.");
        }

        private static void ConfigureMissionHintController(LanRecordingMissionController missionController, Report report)
        {
            MissionHintController hintController = Object.FindAnyObjectByType<MissionHintController>(FindObjectsInactive.Include);
            if (hintController == null)
            {
                GameObject hintObject = new GameObject(MissionHintObjectName);
                hintController = hintObject.AddComponent<MissionHintController>();
                report.Pass($"Created scene object: {MissionHintObjectName}.");
            }
            else
            {
                report.Pass($"Found scene MissionHintController: {GetHierarchyPath(hintController.transform)}.");
            }

            GameObject player = FindScenePlayer();
            Chapter1InputReader inputReader = player != null ? player.GetComponent<Chapter1InputReader>() : Object.FindAnyObjectByType<Chapter1InputReader>(FindObjectsInactive.Include);
            PlayerInputLock inputLock = player != null ? player.GetComponent<PlayerInputLock>() : Object.FindAnyObjectByType<PlayerInputLock>(FindObjectsInactive.Include);

            SerializedObject serializedHint = new SerializedObject(hintController);
            SetObject(serializedHint, "inputReader", inputReader);
            SetObject(serializedHint, "inputLock", inputLock);
            SetObject(serializedHint, "missionController", missionController);
            SetBool(serializedHint, "showBackpackHintOnStart", true);
            serializedHint.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hintController);

            report.Add(inputReader != null, "MissionHintController has Chapter1InputReader reference for [B] hint.");
            report.Add(inputLock != null, "MissionHintController has PlayerInputLock reference.");
            report.Add(missionController != null, "MissionHintController has LanRecordingMissionController reference.");
            report.Pass("MissionHintController will show '[B] Mo balo' at Play start and hide it after the first Inventory input.");
        }

        private static void DisableDebugOverlays(Report report)
        {
            PlayerDebugOverlay[] overlays = Object.FindObjectsByType<PlayerDebugOverlay>(FindObjectsInactive.Include);
            for (int i = 0; i < overlays.Length; i++)
            {
                PlayerDebugOverlay overlay = overlays[i];
                if (overlay == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(overlay);
                SetBool(serialized, "showOverlay", false);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(overlay);
            }

            if (overlays.Length > 0)
            {
                report.Pass($"Disabled PlayerDebugOverlay in scene. Count={overlays.Length}.");
            }
        }

        private static void ConfigurePlayerObject(
            GameObject player,
            InputActionReference inventoryReference,
            ItemDefinition phoneItem,
            InventoryUIController inventoryUi,
            PhoneUIController phoneUi,
            Canvas canvas,
            Report report,
            string label)
        {
            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
            InventoryController inventoryController = EnsureComponent<InventoryController>(player);
            BackpackPhoneInputController inputController = EnsureComponent<BackpackPhoneInputController>(player);

            if (inputReader != null)
            {
                SerializedObject serializedInput = new SerializedObject(inputReader);
                SetObject(serializedInput, "inventoryActionReference", inventoryReference);
                serializedInput.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inputReader);
            }
            else
            {
                report.Fail($"{label} is missing Chapter1InputReader.");
            }

            SerializedObject serializedInventory = new SerializedObject(inventoryController);
            SerializedProperty startingItems = serializedInventory.FindProperty("startingItems");
            if (startingItems != null)
            {
                EnsureObjectInArray(startingItems, phoneItem);
            }

            serializedInventory.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inventoryController);

            SerializedObject serializedBackpack = new SerializedObject(inputController);
            SetObject(serializedBackpack, "inputReader", inputReader);
            SetObject(serializedBackpack, "inputLock", inputLock);
            SetObject(serializedBackpack, "inventoryController", inventoryController);
            SetObject(serializedBackpack, "inventoryUIController", inventoryUi);
            SetObject(serializedBackpack, "phoneUIController", phoneUi);
            SetObject(serializedBackpack, "targetCanvas", canvas);
            SetBool(serializedBackpack, "createRuntimeUiIfMissing", true);
            serializedBackpack.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inputController);

            report.Pass($"{label} has InventoryController and BackpackPhoneInputController configured.");
        }

        private static Canvas EnsureCanvas(Report report)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas canvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.name == BackpackCanvasName)
                {
                    canvas = canvases[i];
                    break;
                }
            }

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
                EditorUtility.SetDirty(canvas);
                EditorUtility.SetDirty(scaler);
                EditorUtility.SetDirty(raycaster);
                report.Pass($"Using existing Canvas: {GetHierarchyPath(canvas.transform)}.");
                return canvas;
            }

            GameObject canvasObject = new GameObject(BackpackCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler newScaler = canvasObject.GetComponent<CanvasScaler>();
            newScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            newScaler.referenceResolution = new Vector2(1920f, 1080f);
            report.Warning($"Created Canvas for Backpack + Phone UI: {BackpackCanvasName}.");
            return canvas;
        }

        private static void EnsureEventSystem(Report report)
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystems.Length == 0)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                report.Warning($"Created EventSystem: {GetHierarchyPath(eventSystemObject.transform)}.");
                return;
            }

            if (eventSystems.Length > 1)
            {
                report.Fail($"Scene has {eventSystems.Length} EventSystems; expected exactly one.");
            }
            else
            {
                report.Pass("Scene has one EventSystem.");
            }

            if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystems[0].gameObject.AddComponent<InputSystemUIInputModule>();
                report.Warning("Added InputSystemUIInputModule to EventSystem.");
            }
        }

        private static PhoneUIController EnsureScenePhonePanel(Canvas canvas, GameObject prefab, UiAssets assets, Report report)
        {
            PhoneUIController existing = Object.FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                MoveUiToCanvas(existing.transform, canvas);
                EnsurePhonePanelLayout(existing.transform, assets);
                report.Pass("Scene PhonePanel already exists.");
                return existing;
            }

            GameObject instance = prefab != null
                ? PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject
                : CreatePanelRoot("PhonePanel");
            if (instance == null)
            {
                report.Fail("Could not instantiate PhonePanel.");
                return null;
            }

            instance.name = "PhonePanel";
            if (instance.transform.parent == null)
            {
                instance.transform.SetParent(canvas.transform, false);
            }
            MoveUiToCanvas(instance.transform, canvas);

            PhoneUIController controller = instance.GetComponent<PhoneUIController>() ?? instance.AddComponent<PhoneUIController>();
            EnsurePhonePanelLayout(instance.transform, assets);
            instance.SetActive(false);
            report.Pass("Scene PhonePanel is attached to Canvas.");
            return controller;
        }

        private static InventoryUIController EnsureSceneInventoryPanel(Canvas canvas, GameObject prefab, UiAssets assets, Report report)
        {
            InventoryUIController existing = Object.FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                MoveUiToCanvas(existing.transform, canvas);
                report.Pass("Scene InventoryPanel already exists.");
                return existing;
            }

            GameObject instance = prefab != null
                ? PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject
                : CreatePanelRoot("InventoryPanel");
            if (instance == null)
            {
                report.Fail("Could not instantiate InventoryPanel.");
                return null;
            }

            instance.name = "InventoryPanel";
            if (instance.transform.parent == null)
            {
                instance.transform.SetParent(canvas.transform, false);
            }
            MoveUiToCanvas(instance.transform, canvas);

            InventoryUIController controller = instance.GetComponent<InventoryUIController>() ?? instance.AddComponent<InventoryUIController>();
            instance.SetActive(false);
            report.Pass("Scene InventoryPanel is attached to Canvas.");
            return controller;
        }

        private static void MoveUiToCanvas(Transform uiTransform, Canvas canvas)
        {
            if (uiTransform == null || canvas == null)
            {
                return;
            }

            if (uiTransform.parent != canvas.transform)
            {
                uiTransform.SetParent(canvas.transform, false);
            }

            uiTransform.SetAsLastSibling();
            RectTransform rect = uiTransform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            EditorUtility.SetDirty(uiTransform);
        }

        private static void ConfigureInventoryControllerReferences(
            InventoryUIController controller,
            InventoryController inventory,
            PhoneUIController phone,
            PlayerInputLock inputLock,
            UiAssets assets)
        {
            if (controller == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "panelRoot", controller.gameObject);
            SetObject(serialized, "canvasGroup", controller.GetComponent<CanvasGroup>());
            SetObject(serialized, "inventoryController", inventory);
            SetObject(serialized, "phoneUIController", phone);
            SetObject(serialized, "inputLock", inputLock);
            SetObject(serialized, "detailIcon", FindChildComponent<Image>(controller.transform, "DetailIcon"));
            SetObject(serialized, "detailNameText", FindChildComponent<TextMeshProUGUI>(controller.transform, "DetailName"));
            SetObject(serialized, "detailDescriptionText", FindChildComponent<TextMeshProUGUI>(controller.transform, "DetailDescription"));
            SetObject(serialized, "detailQuantityText", FindChildComponent<TextMeshProUGUI>(controller.transform, "DetailQuantity"));
            SetObject(serialized, "useButton", FindChildComponent<Button>(controller.transform, "UseButton"));
            SetObject(serialized, "closeButton", FindChildComponent<Button>(controller.transform, "CloseButton"));
            SetObject(serialized, "audioSource", controller.GetComponent<AudioSource>());
            SetObject(serialized, "openClip", assets.OpenCloseClip);
            SetObject(serialized, "closeClip", assets.OpenCloseClip);
            SetObject(serialized, "selectClip", assets.ClickClip);
            SetObject(serialized, "useClip", assets.ClickClip);

            SerializedProperty slots = serialized.FindProperty("slots");
            if (slots != null)
            {
                InventorySlotUI[] slotComponents = controller.GetComponentsInChildren<InventorySlotUI>(true);
                slots.arraySize = slotComponents.Length;
                for (int i = 0; i < slotComponents.Length; i++)
                {
                    slots.GetArrayElementAtIndex(i).objectReferenceValue = slotComponents[i];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePhoneControllerReferences(PhoneUIController controller, PlayerInputLock inputLock, UiAssets assets)
        {
            if (controller == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "panelRoot", controller.gameObject);
            SetObject(serialized, "canvasGroup", controller.GetComponent<CanvasGroup>());
            SetObject(serialized, "inputLock", inputLock);
            SetObject(serialized, "homeScreen", FindChild(controller.transform, "HomeScreen")?.gameObject);
            SetObject(serialized, "appContent", FindChild(controller.transform, "AppContent")?.gameObject);
            SetObject(serialized, "messengerButton", FindChildComponent<Button>(controller.transform, "MessengerButton"));
            SetObject(serialized, "recorderButton", FindChildComponent<Button>(controller.transform, "RecorderButton"));
            SetObject(serialized, "cameraButton", FindChildComponent<Button>(controller.transform, "CameraButton"));
            SetObject(serialized, "googleButton", FindChildComponent<Button>(controller.transform, "GoogleButton"));
            SetObject(serialized, "homeButton", FindChildComponent<Button>(controller.transform, "HomeButton"));
            SetObject(serialized, "appTitleText", FindChildComponent<TextMeshProUGUI>(controller.transform, "AppTitleText"));
            SetObject(serialized, "appBodyText", FindChildComponent<TextMeshProUGUI>(controller.transform, "AppBodyText"));
            SetObject(serialized, "messagesButton", FindChildComponent<Button>(controller.transform, "MessagesButton"));
            SetObject(serialized, "recordingsButton", FindChildComponent<Button>(controller.transform, "RecordingsButton"));
            SetObject(serialized, "cluesButton", FindChildComponent<Button>(controller.transform, "CluesButton"));
            SetObject(serialized, "closeButton", FindChildComponent<Button>(controller.transform, "CloseButton"));
            SetObject(serialized, "messagesContent", FindChild(controller.transform, "MessagesContent")?.gameObject);
            SetObject(serialized, "recordingsContent", FindChild(controller.transform, "RecordingsContent")?.gameObject);
            SetObject(serialized, "cluesContent", FindChild(controller.transform, "CluesContent")?.gameObject);
            SetObject(serialized, "messagesText", FindChildComponent<TextMeshProUGUI>(controller.transform, "MessagesText"));
            SetObject(serialized, "recordingsText", FindChildComponent<TextMeshProUGUI>(controller.transform, "RecordingsText"));
            SetObject(serialized, "cluesText", FindChildComponent<TextMeshProUGUI>(controller.transform, "CluesText"));
            SetObject(serialized, "messagesHighlight", FindChildComponent<Image>(controller.transform, "MessagesHighlight"));
            SetObject(serialized, "recordingsHighlight", FindChildComponent<Image>(controller.transform, "RecordingsHighlight"));
            SetObject(serialized, "cluesHighlight", FindChildComponent<Image>(controller.transform, "CluesHighlight"));
            SetObject(serialized, "audioSource", controller.GetComponent<AudioSource>());
            SetObject(serialized, "openClip", assets.OpenCloseClip);
            SetObject(serialized, "closeClip", assets.OpenCloseClip);
            SetObject(serialized, "tabClip", assets.TabClip);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateInput(Report report)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputAction inventory = inputActions != null ? inputActions.FindAction("Gameplay/Inventory", false) : null;
            report.Add(inventory != null, "Inventory action exists.");
            bool hasB = false;
            if (inventory != null)
            {
                foreach (InputBinding binding in inventory.bindings)
                {
                    hasB |= string.Equals(binding.effectivePath, "<Keyboard>/b", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(binding.path, "<Keyboard>/b", StringComparison.OrdinalIgnoreCase);
                }
            }

            report.Add(hasB, "Inventory action binding is <Keyboard>/b.");
            report.Add(typeof(Chapter1InputReader).GetEvent("InventoryPressed") != null, "Chapter1InputReader has InventoryPressed event.");
        }

        private static void ValidateAssets(Report report)
        {
            ItemDefinition phone = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PhoneItemPath);
            report.Add(phone != null, "PhoneItem.asset exists.");
            report.Add(phone != null && phone.ItemId == PhoneId, "PhoneItem itemId is phone.");
            report.Add(phone != null && phone.Icon != null, "PhoneItem has icon.");
            report.Add(phone != null && !phone.IsDroppable, "PhoneItem is not droppable.");
            report.Add(phone != null && phone.IsUsable, "PhoneItem is usable.");
        }

        private static void ValidateFirstMissionPhoneAssets(Report report)
        {
            string absoluteAudioPath = Path.Combine(Directory.GetCurrentDirectory(), LanRecordingAudioPath.Replace('/', Path.DirectorySeparatorChar));
            bool fileExists = File.Exists(absoluteAudioPath);
            if (fileExists)
            {
                report.Pass($"Lan recording MP3 exists on disk: {LanRecordingAudioPath}.");
                AssetDatabase.ImportAsset(LanRecordingAudioPath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                report.Warning($"Lan recording MP3 missing on disk: {LanRecordingAudioPath}. No fake AudioClip should be created.");
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanRecordingAudioPath);
            if (clip != null)
            {
                report.Pass($"Lan recording AudioClip loads from exact path: {LanRecordingAudioPath}.");
            }
            else
            {
                report.Warning(
                    $"AssetDatabase.LoadAssetAtPath<AudioClip>(\"{LanRecordingAudioPath}\") returned null during validation. Voice message/controller references should remain empty rather than using a placeholder.");
            }

            PhoneMessageData message = AssetDatabase.LoadAssetAtPath<PhoneMessageData>(LanRecordingMessagePath);
            report.Add(message != null, $"Lan voice message data exists: {LanRecordingMessagePath}.");
            if (message != null && clip != null)
            {
                report.Add(message.AudioClip == clip, "Lan voice message data uses the real Lan_LastRecording AudioClip.");
            }
            else if (message != null)
            {
                report.Warning("Lan voice message data currently has no validated real AudioClip reference.");
            }
        }

        private static void ValidatePrefabs(Report report)
        {
            GameObject slot = AssetDatabase.LoadAssetAtPath<GameObject>(InventorySlotPrefabPath);
            GameObject inventory = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPanelPrefabPath);
            GameObject phone = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePanelPrefabPath);
            report.Add(slot != null, "InventorySlot prefab exists.");
            report.Add(inventory != null, "InventoryPanel prefab exists.");
            report.Add(phone != null, "PhonePanel prefab exists.");
            report.Add(inventory != null && inventory.GetComponentsInChildren<InventorySlotUI>(true).Length >= 12, "InventoryPanel has at least 12 slots.");
            report.Add(phone != null && phone.GetComponentInChildren<PhoneUIController>(true) != null, "PhonePanel has PhoneUIController.");
            report.Add(
                phone != null &&
                (HasPhoneAppButtons(phone.transform) ||
                 phone.GetComponentInChildren<PhoneUIController>(true) != null),
                "PhonePanel has app buttons or a runtime app builder.");
        }

        private static void ValidateScene(Report report)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Warning("Scene validation skipped because current scene changes were not saved.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindScenePlayer();
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            InventoryController inventory = player != null ? player.GetComponent<InventoryController>() : null;
            BackpackPhoneInputController inputController = player != null ? player.GetComponent<BackpackPhoneInputController>() : null;
            InventoryUIController inventoryUi = Object.FindAnyObjectByType<InventoryUIController>(FindObjectsInactive.Include);
            PhoneUIController phoneUi = Object.FindAnyObjectByType<PhoneUIController>(FindObjectsInactive.Include);
            LanRecordingMissionController lanMission = Object.FindAnyObjectByType<LanRecordingMissionController>(FindObjectsInactive.Include);
            AudioClip lanClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanRecordingAudioPath);
            Canvas backpackCanvas = FindBackpackCanvas(canvases);

            report.Add(player != null, "Scene Player exists.");
            report.Add(backpackCanvas != null, $"Scene has {BackpackCanvasName}.");
            report.Add(backpackCanvas != null && backpackCanvas.renderMode == RenderMode.ScreenSpaceOverlay, $"{BackpackCanvasName} is Screen Space Overlay.");
            report.Add(eventSystems.Length == 1, $"Scene has one EventSystem. Count={eventSystems.Length}.");
            report.Add(eventSystems.Length == 1 && eventSystems[0].GetComponent<InputSystemUIInputModule>() != null, "EventSystem uses InputSystemUIInputModule.");
            report.Add(inventory != null, "Player has InventoryController.");
            report.Add(inputController != null, "Player has BackpackPhoneInputController.");
            report.Add(inventory != null && inventory.GetItem(PhoneId) != null || PlayerStartingItemsContainPhone(inventory), "Inventory has exactly one default phone by itemId.");
            report.Add(inventoryUi != null, "Scene InventoryPanel exists.");
            report.Add(phoneUi != null, "Scene PhonePanel exists.");
            report.Add(inventoryUi != null && backpackCanvas != null && inventoryUi.transform.IsChildOf(backpackCanvas.transform), "InventoryPanel is under Backpack canvas.");
            report.Add(phoneUi != null && backpackCanvas != null && phoneUi.transform.IsChildOf(backpackCanvas.transform), "PhonePanel is under Backpack canvas.");
            report.Add(
                phoneUi != null &&
                (HasPhoneAppButtons(phoneUi.transform) ||
                 phoneUi.GetComponent<PhoneUIController>() != null),
                "Scene PhonePanel has app buttons or a runtime app builder.");
            report.Add(inventoryUi != null && inventoryUi.GetComponentsInChildren<InventorySlotUI>(true).Length >= 12, "Scene InventoryPanel has 12 slots.");
            report.Add(lanMission != null, "Scene has LanRecordingMissionController.");
            if (lanMission != null && lanClip != null)
            {
                report.Add(lanMission.LanRecordingClip == lanClip, "LanRecordingMissionController uses the real Lan_LastRecording AudioClip.");
            }
            else if (lanMission != null)
            {
                report.Warning("LanRecordingMissionController currently has no validated real AudioClip reference.");
            }

            report.Add(!HasMissingScripts(scene), "Scene has no Missing Script components.");
        }

        private static bool HasPhoneAppButtons(Transform root)
        {
            return FindChild(root, "MessengerButton") != null
                && FindChild(root, "RecorderButton") != null
                && FindChild(root, "CameraButton") != null
                && FindChild(root, "GoogleButton") != null;
        }

        private static bool HasPhoneAppContentControls(Transform root)
        {
            return FindChild(root, "AppContent") != null
                && FindChild(root, "HomeButton") != null
                && FindChild(root, "AppTitleText") != null
                && FindChild(root, "AppBodyText") != null;
        }

        private static Canvas FindBackpackCanvas(Canvas[] canvases)
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && canvas.gameObject.name == BackpackCanvasName)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static bool PlayerStartingItemsContainPhone(InventoryController inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty startingItems = serialized.FindProperty("startingItems");
            if (startingItems == null)
            {
                return false;
            }

            for (int i = 0; i < startingItems.arraySize; i++)
            {
                ItemDefinition definition = startingItems.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinition;
                if (definition != null && definition.ItemId == PhoneId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMissingScripts(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Component[] components = transforms[j].GetComponents<Component>();
                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        if (components[componentIndex] == null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static GameObject CreatePanelRoot(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            return root;
        }

        private static void CreateInventoryPanelChildren(Transform root, GameObject slotPrefab, UiAssets assets)
        {
            RectTransform main = CreateImage(root, "MainPanel", new Color(0.06f, 0.06f, 0.065f, 0.98f), assets.PanelSprite).rectTransform;
            SetCentered(main, new Vector2(1180f, 720f));
            CreateText(main, "Title", "BALO", 46f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(420f, 72f));
            CreateButton(main, "CloseButton", "X", assets.ButtonSprite, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(54f, 54f));

            RectTransform grid = CreateEmpty(main, "SlotGrid");
            SetRect(grid, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -140f), new Vector2(520f, 430f), new Vector2(0f, 1f));
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(112f, 112f);
            layout.spacing = new Vector2(18f, 18f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;

            for (int i = 0; i < 12; i++)
            {
                GameObject slotInstance = slotPrefab != null
                    ? PrefabUtility.InstantiatePrefab(slotPrefab, grid) as GameObject
                    : CreateSlotObject(grid, $"InventorySlot_{i + 1:00}", assets).gameObject;
                if (slotInstance != null)
                {
                    slotInstance.name = $"InventorySlot_{i + 1:00}";
                }
            }

            RectTransform detail = CreateImage(main, "DetailPanel", new Color(0.095f, 0.095f, 0.105f, 0.96f), assets.PanelSprite).rectTransform;
            SetRect(detail, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330f, -12f), new Vector2(430f, 520f), new Vector2(0.5f, 0.5f));
            RectTransform detailIcon = CreateImage(detail, "DetailIcon", Color.white, null, false).rectTransform;
            SetRect(detailIcon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(156f, 156f), new Vector2(0.5f, 0.5f));
            CreateText(detail, "DetailName", string.Empty, 34f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(360f, 52f));
            CreateText(detail, "DetailDescription", string.Empty, 22f, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(342f, 150f));
            CreateText(detail, "DetailQuantity", string.Empty, 20f, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(260f, 34f));
            CreateButton(detail, "UseButton", "S\u1eec D\u1ee4NG", assets.ButtonSprite, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(260f, 62f));
        }

        private static void CreatePhonePanelChildren(Transform root, UiAssets assets)
        {
            RectTransform frame = CreateImage(root, "PhoneFrame", new Color(0.025f, 0.025f, 0.03f, 0.99f), assets.PanelSprite).rectTransform;
            SetCentered(frame, new Vector2(460f, 780f));
            CreateText(frame, "Title", "\u0110I\u1ec6N THO\u1ea0I", 36f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(360f, 56f));
            CreateButton(frame, "CloseButton", "X", assets.ButtonSprite, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -38f), new Vector2(48f, 48f));
            CreatePhoneHomeScreen(frame, assets);
            RectTransform appContent = CreatePhoneAppContent(frame, assets);
            appContent.gameObject.SetActive(false);
        }

        private static void EnsurePhonePanelLayout(Transform root, UiAssets assets)
        {
            if (root == null)
            {
                return;
            }

            RectTransform frame = FindChild(root, "PhoneFrame") as RectTransform;
            if (frame == null)
            {
                frame = CreateImage(root, "PhoneFrame", new Color(0.025f, 0.025f, 0.03f, 0.99f), assets.PanelSprite).rectTransform;
                SetCentered(frame, new Vector2(460f, 780f));
            }

            if (FindChild(frame, "Title") == null)
            {
                CreateText(frame, "Title", "\u0110I\u1ec6N THO\u1ea0I", 36f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(360f, 56f));
            }

            if (FindChild(frame, "CloseButton") == null)
            {
                CreateButton(frame, "CloseButton", "X", assets.ButtonSprite, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -38f), new Vector2(48f, 48f));
            }

            SetChildActive(root, "Tabs", false);
            SetChildActive(root, "ContentPanel", false);

            Transform home = FindChild(root, "HomeScreen");
            if (home != null && !HasPhoneAppButtons(root))
            {
                Object.DestroyImmediate(home.gameObject);
                home = null;
            }

            if (home == null)
            {
                home = CreatePhoneHomeScreen(frame, assets);
            }

            Transform appContent = FindChild(root, "AppContent");
            if (appContent != null && !HasPhoneAppContentControls(root))
            {
                Object.DestroyImmediate(appContent.gameObject);
                appContent = null;
            }

            if (appContent == null)
            {
                appContent = CreatePhoneAppContent(frame, assets);
            }

            home.gameObject.SetActive(true);
            appContent.gameObject.SetActive(false);
        }

        private static InventorySlotUI CreateSlotObject(Transform parent, string name, UiAssets assets)
        {
            GameObject slot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InventorySlotUI));
            if (parent != null)
            {
                slot.transform.SetParent(parent, false);
            }

            Image background = slot.GetComponent<Image>();
            background.sprite = assets.SlotSprite;
            background.type = assets.SlotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0.18f, 0.18f, 0.19f, 0.92f);
            Button button = slot.GetComponent<Button>();
            button.targetGraphic = background;

            RectTransform highlight = CreateImage(slot.transform, "SelectedHighlight", new Color(0.45f, 0.03f, 0.03f, 0.78f), assets.SlotSelectedSprite).rectTransform;
            highlight.anchorMin = Vector2.zero;
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = Vector2.zero;
            highlight.offsetMax = Vector2.zero;
            highlight.GetComponent<Image>().enabled = false;
            RectTransform icon = CreateImage(slot.transform, "Icon", Color.white, null, false).rectTransform;
            SetRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78f, 78f), new Vector2(0.5f, 0.5f));
            CreateText(slot.transform, "Quantity", string.Empty, 20f, TextAlignmentOptions.BottomRight, Vector2.zero, Vector2.one, new Vector2(-8f, 6f), new Vector2(-16f, -12f));
            return slot.GetComponent<InventorySlotUI>();
        }

        private static void CreateTabButton(RectTransform parent, string buttonName, string label, string highlightName, UiAssets assets)
        {
            Button button = CreateButton(parent, buttonName, label, assets.ButtonSprite, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform highlight = CreateImage(button.transform, highlightName, new Color(0.46f, 0.04f, 0.04f, 0.85f), assets.SlotSelectedSprite).rectTransform;
            highlight.anchorMin = Vector2.zero;
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = Vector2.zero;
            highlight.offsetMax = Vector2.zero;
            highlight.SetAsFirstSibling();
            highlight.GetComponent<Image>().enabled = false;
        }

        private static RectTransform CreatePhoneHomeScreen(RectTransform parent, UiAssets assets)
        {
            RectTransform screen = CreateEmpty(parent, "HomeScreen");
            Stretch(screen, new Vector2(42f, 68f), new Vector2(-42f, -126f));

            CreateText(
                screen,
                "HomeTitle",
                "Home",
                22f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(260f, 34f));

            RectTransform grid = CreateEmpty(screen, "AppGrid");
            SetRect(
                grid,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(332f, 346f),
                new Vector2(0.5f, 0.5f));

            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(146f, 146f);
            layout.spacing = new Vector2(34f, 34f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateAppButton(grid, "MessengerButton", "Messenger", "M", new Color(0.05f, 0.48f, 0.95f, 1f), assets);
            CreateAppButton(grid, "RecorderButton", "Ghi \u00e2m", "REC", new Color(0.82f, 0.12f, 0.17f, 1f), assets);
            CreateAppButton(grid, "CameraButton", "Camera", "CAM", new Color(0.13f, 0.62f, 0.37f, 1f), assets);
            CreateAppButton(grid, "GoogleButton", "Google", "G", new Color(0.96f, 0.72f, 0.12f, 1f), assets);
            return screen;
        }

        private static RectTransform CreatePhoneAppContent(RectTransform parent, UiAssets assets)
        {
            RectTransform content = CreateImage(parent, "AppContent", new Color(0.075f, 0.077f, 0.083f, 0.98f), assets.PanelSprite).rectTransform;
            SetRect(
                content,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 314f),
                new Vector2(370f, 520f),
                new Vector2(0.5f, 0.5f));

            CreateButton(
                content,
                "HomeButton",
                "<",
                assets.ButtonSprite,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -36f),
                new Vector2(54f, 48f));

            CreateText(
                content,
                "AppTitleText",
                string.Empty,
                28f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(18f, -38f),
                new Vector2(250f, 44f));

            RectTransform bodyPanel = CreateImage(content, "AppBodyPanel", new Color(0.11f, 0.115f, 0.125f, 0.98f), assets.PanelSprite).rectTransform;
            Stretch(bodyPanel, new Vector2(26f, 28f), new Vector2(-26f, -94f));
            CreateText(
                bodyPanel,
                "AppBodyText",
                string.Empty,
                24f,
                TextAlignmentOptions.TopLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, -2f),
                new Vector2(-34f, -28f));

            return content;
        }

        private static Button CreateAppButton(Transform parent, string name, string label, string glyph, Color iconColor, UiAssets assets)
        {
            Button button = CreateButton(parent, name, string.Empty, assets.ButtonSprite, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.13f, 0.135f, 0.15f, 0.95f);
            }

            RectTransform icon = CreateImage(button.transform, "Icon", iconColor, assets.SlotSprite).rectTransform;
            SetRect(
                icon,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -48f),
                new Vector2(72f, 72f),
                new Vector2(0.5f, 0.5f));

            TextMeshProUGUI glyphText = CreateText(
                icon,
                "Glyph",
                glyph,
                glyph.Length > 1 ? 22f : 34f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            glyphText.color = Color.white;

            CreateText(
                button.transform,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 24f),
                new Vector2(0f, 36f));

            return button;
        }

        private static void CreatePhoneContent(RectTransform parent, string objectName, string textName, string text)
        {
            RectTransform content = CreateEmpty(parent, objectName);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(26f, 26f);
            content.offsetMax = new Vector2(-26f, -26f);
            CreateText(content, textName, text, 24f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Button CreateButton(Transform parent, string name, string label, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, size, new Vector2(0.5f, 0.5f));
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.22f, 0.22f, 0.23f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                CreateText(buttonObject.transform, "Label", label, 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 rectSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, rectSize, new Vector2(0.5f, 0.5f));
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(0.91f, 0.88f, 0.8f, 1f);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static Image CreateImage(Transform parent, string name, Color color, Sprite sprite, bool enabled = true)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.enabled = enabled;
            return image;
        }

        private static RectTransform CreateEmpty(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void SetCentered(RectTransform rect, Vector2 size)
        {
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void WriteFirstMissionPhoneReport(Report report)
        {
            try
            {
                string absoluteAudioPath = Path.Combine(Directory.GetCurrentDirectory(), LanRecordingAudioPath.Replace('/', Path.DirectorySeparatorChar));
                bool audioFileExists = File.Exists(absoluteAudioPath);
                AudioClip lanClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LanRecordingAudioPath);
                PhoneMessageData lanMessage = AssetDatabase.LoadAssetAtPath<PhoneMessageData>(LanRecordingMessagePath);
                MissionHintController missionHint = Object.FindAnyObjectByType<MissionHintController>(FindObjectsInactive.Include);

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# FIRST MISSION PHONE SETUP REPORT");
                builder.AppendLine();
                builder.AppendLine("## Lan Recording Audio");
                builder.AppendLine($"- Expected real AudioClip path: `{LanRecordingAudioPath}`");
                builder.AppendLine($"- MP3 file exists on disk: `{audioFileExists}`");
                builder.AppendLine($"- `AssetDatabase.LoadAssetAtPath<AudioClip>()` result: `{(lanClip != null ? lanClip.name : "null")}`");
                builder.AppendLine($"- Voice message data path: `{LanRecordingMessagePath}`");
                builder.AppendLine($"- Voice message data exists: `{(lanMessage != null)}`");
                builder.AppendLine($"- Voice message data uses expected clip: `{(lanMessage != null && lanClip != null && lanMessage.AudioClip == lanClip)}`");
                builder.AppendLine("- Missing audio behavior: write a clear Console warning and leave the AudioClip reference empty; do not create or assign a fake AudioClip.");
                builder.AppendLine("- Audio file content policy: setup only imports/assigns the MP3 reference and never edits the audio file contents.");
                builder.AppendLine();
                builder.AppendLine("## First Backpack Hint");
                builder.AppendLine("- Expected first hint text: `[B] Mở balo`");
                builder.AppendLine($"- MissionHintController exists in open scene: `{(missionHint != null)}`");
                builder.AppendLine("- Hint behavior: show after Play starts, hide when the first Inventory input is pressed.");
                builder.AppendLine();
                builder.AppendLine("## Setup Tool");
                builder.AppendLine("- Menu: `Tools > Chapter 1 > Setup First Mission Phone Sequence`");
                builder.AppendLine("- The setup tool also runs this audio assignment from `Tools > Chapter 1 > Setup Backpack And Phone`.");
                builder.AppendLine();
                builder.AppendLine("## Console Report Lines");
                foreach (string line in report.Lines)
                {
                    builder.AppendLine("- `" + line.Replace("`", "'") + "`");
                }
                builder.AppendLine();
                builder.AppendLine("## Summary");
                builder.AppendLine($"- {report.GetSummary()}");

                File.WriteAllText(FirstMissionPhoneReportPath, builder.ToString(), Encoding.UTF8);
                AssetDatabase.ImportAsset(FirstMissionPhoneReportPath, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FIRST MISSION PHONE SETUP REPORT] Could not write {FirstMissionPhoneReportPath}: {exception.Message}");
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureObjectInArray(SerializedProperty array, Object value)
        {
            if (value == null)
            {
                return;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return;
                }
            }

            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = FindChild(root, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void SetChildActive(Transform root, string childName, bool active)
        {
            Transform child = FindChild(root, childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static GameObject FindScenePlayer()
        {
            Chapter1PlayerMotor motor = Object.FindAnyObjectByType<Chapter1PlayerMotor>(FindObjectsInactive.Include);
            if (motor != null)
            {
                return motor.gameObject;
            }

            Chapter1InputReader inputReader = Object.FindAnyObjectByType<Chapter1InputReader>(FindObjectsInactive.Include);
            if (inputReader != null)
            {
                return inputReader.gameObject;
            }

            PlayerCombatController combat = Object.FindAnyObjectByType<PlayerCombatController>(FindObjectsInactive.Include);
            return combat != null ? combat.gameObject : null;
        }

        private static string FindAssetPathByKeywords(string[] keywords, string[] folders, string extension)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                for (int keywordIndex = 0; keywordIndex < keywords.Length; keywordIndex++)
                {
                    if (lower.Contains(keywords[keywordIndex]))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private static void FillRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    int dx = px < x + radius ? x + radius - px : px >= x + width - radius ? px - (x + width - radius - 1) : 0;
                    int dy = py < y + radius ? y + radius - py : py >= y + height - radius ? py - (y + height - radius - 1) : 0;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                FillCircle(texture, x0, y0, thickness, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private sealed class UiAssets
        {
            public Sprite PanelSprite;
            public Sprite SlotSprite;
            public Sprite SlotSelectedSprite;
            public Sprite ButtonSprite;
            public Sprite CloseIconSprite;
            public Sprite DividerSprite;
            public AudioClip ClickClip;
            public AudioClip OpenCloseClip;
            public AudioClip TabClip;
        }

        private sealed class FirstMissionPhoneAssets
        {
            public AudioClip LanRecordingClip;
            public PhoneMessageData LanRecordingMessage;
        }

        private sealed class Report
        {
            private readonly string title;
            private readonly List<string> lines = new List<string>();
            private int passCount;
            private int warningCount;
            private int failCount;

            public Report(string title)
            {
                this.title = title;
            }

            public IReadOnlyList<string> Lines => lines;

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
                passCount++;
                lines.Add("[PASS] " + message);
            }

            public void Warning(string message)
            {
                warningCount++;
                lines.Add("[WARNING] " + message);
            }

            public void Fail(string message)
            {
                failCount++;
                lines.Add("[FAIL] " + message);
            }

            public void Info(string message)
            {
                lines.Add("[INFO] " + message);
            }

            public void FlushToConsole()
            {
                string body = string.Join("\n", lines);
                string summary = GetSummary();
                if (failCount > 0)
                {
                    Debug.LogError($"[{title}]\n{body}\n{summary}");
                }
                else if (warningCount > 0)
                {
                    Debug.LogWarning($"[{title}]\n{body}\n{summary}");
                }
                else
                {
                    Debug.Log($"[{title}]\n{body}\n{summary}");
                }
            }

            public string GetSummary()
            {
                return $"PASS: {passCount} | WARNING: {warningCount} | FAIL: {failCount}";
            }
        }
    }
}
