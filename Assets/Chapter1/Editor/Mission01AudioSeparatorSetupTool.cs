using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DormitoryMystery.Chapter1;
using NavKeypad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Mission01AudioSeparatorSetupTool
    {
        public const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        public const string ReportPath = "Assets/Chapter1/Documentation/MISSION_01_AUDIO_SEPARATOR_SETUP_REPORT.md";
        public const string AudioSeparatorItemPath = "Assets/Chapter1/Data/Inventory/Items/AudioSeparatorItem.asset";
        public const string AudioSeparatorPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/AudioSeparator_Device.prefab";

        private const string ManagerObjectName = "Mission01AudioSeparatorManager";
        private const string DungRoomName = "DungRoom";
        private const string DungRoomDoorName = "DungRoom_Door";
        private const string DungRoomKeypadName = "DungRoom_Keypad";
        private const string AudioSeparatorSpawnName = "AudioSeparator_Spawn";
        private const string DungRoomInteriorMarkerName = "DungRoom_InteriorMarker";
        private const string DungRoomMarkerName = "DungRoom_Marker";
        private const string SceneAudioSeparatorName = "AudioSeparator_Device";
        private const string CalendarObjectName = "WallCalendar_March25";
        private const string InteractableLayerName = "Interactable";
        private const string MaterialFolderPath = "Assets/Chapter1/Materials/Props";
        private const string AudioSeparatorMaterialPath = MaterialFolderPath + "/AudioSeparator_Device.mat";

        [MenuItem("Tools/Chapter 1/Setup Mission 01 Audio Separator")]
        public static void SetupMission01AudioSeparator()
        {
            SetupResult result = RunSetup(true);
            Debug.Log(result.ToConsoleString());
        }

        [MenuItem("Tools/Chapter 1/Validate Mission 01 Audio Separator")]
        public static void ValidateMission01AudioSeparator()
        {
            SetupResult result = RunValidation(true);
            Debug.Log(result.ToConsoleString());
        }

        public static void SetupMission01AudioSeparatorNoDialog()
        {
            SetupResult result = RunSetup(false);
            Debug.Log(result.ToConsoleString());
        }

        public static void ValidateMission01AudioSeparatorNoDialog()
        {
            SetupResult result = RunValidation(false);
            Debug.Log(result.ToConsoleString());
        }

        public static SetupResult RunSetup(bool showDialog)
        {
            SetupResult report = new SetupResult("MISSION 01 AUDIO SEPARATOR SETUP REPORT");
            EnsureFolders();

            Scene scene = OpenTargetScene(report, showDialog);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Fail($"Could not open target scene: {ScenePath}.");
                WriteReport(report);
                ShowDialogIfRequested(showDialog, "Setup Mission 01 Audio Separator", report);
                return report;
            }

            int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
            if (interactableLayer < 0)
            {
                report.Warning($"Layer '{InteractableLayerName}' does not exist. Interactable objects were created but may not be detected until the layer is added.");
            }

            Chapter1Manager chapterManager = EnsureChapter1Manager(scene, report);
            Mission01AudioSeparatorManager missionManager = EnsureMissionManager(scene, chapterManager, report);
            GameObject audioSeparatorPrefab = EnsureAudioSeparatorPrefab(interactableLayer, report);

            ConnectPhoneControllers(missionManager, report);
            ConnectMinh(missionManager, report);

            DungRoomSetup roomSetup = EnsureDungRoom(scene, missionManager, audioSeparatorPrefab, interactableLayer, report);
            EnsureCalendarInteractable(scene, interactableLayer, report);

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.Info("AudioSeparator device is configured as a world interaction, not an inventory pickup.");
            report.Info($"Dung room placement: {roomSetup.Description}");
            WriteReport(report);
            ShowDialogIfRequested(showDialog, "Setup Mission 01 Audio Separator", report);
            return report;
        }

        public static SetupResult RunValidation(bool showDialog)
        {
            SetupResult report = new SetupResult("MISSION 01 AUDIO SEPARATOR VALIDATION REPORT");
            Scene scene = OpenTargetScene(report, showDialog);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Fail($"Could not open target scene: {ScenePath}.");
                WriteReport(report);
                ShowDialogIfRequested(showDialog, "Validate Mission 01 Audio Separator", report);
                return report;
            }

            ValidateManagerCount<Chapter1Manager>(scene, "Chapter1Manager", report);
            ValidateManagerCount<Mission01AudioSeparatorManager>(scene, "Mission01AudioSeparatorManager", report);
            ValidatePhoneAndDungConversation(scene, report);
            ValidateNoForbiddenAnswerLeak(report);
            ValidateNoMissionAiOrApi(report);
            ValidateSceneObjectWithComponent<MinhDialogueInteractable>(scene, "Minh interaction", report);
            ValidateDungRoom(scene, report);
            ValidateCalendar(scene, report);
            ValidateAudioSeparator(scene, report);
            ValidateMissingReferences(scene, report);
            ValidateNoDuplicateNamedObjects(scene, ManagerObjectName, report);
            ValidateNoDuplicateNamedObjects(scene, DungRoomName, report);
            ValidateNoDuplicateNamedObjects(scene, SceneAudioSeparatorName, report);

            WriteReport(report);
            ShowDialogIfRequested(showDialog, "Validate Mission 01 Audio Separator", report);
            return report;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Chapter1/Documentation");
            EnsureFolder("Assets/Chapter1/Data");
            EnsureFolder("Assets/Chapter1/Data/Inventory");
            EnsureFolder("Assets/Chapter1/Data/Inventory/Items");
            EnsureFolder("Assets/Chapter1/Prefabs");
            EnsureFolder("Assets/Chapter1/Prefabs/Gameplay");
            EnsureFolder("Assets/Chapter1/Materials");
            EnsureFolder(MaterialFolderPath);
        }

        private static Scene OpenTargetScene(SetupResult report, bool allowUserPrompt)
        {
            Scene active = SceneManager.GetActiveScene();
            if (string.Equals(active.path, ScenePath, StringComparison.Ordinal))
            {
                report.Info($"Using active scene: {active.path}.");
                return active;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (string.Equals(loaded.path, ScenePath, StringComparison.Ordinal))
                {
                    SceneManager.SetActiveScene(loaded);
                    report.Info($"Using already loaded scene: {loaded.path}.");
                    return loaded;
                }
            }

            if (allowUserPrompt && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Fail("Canceled because current modified scenes were not saved.");
                return default;
            }

            Scene opened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            report.Info($"Opened scene: {ScenePath}.");
            return opened;
        }

        private static Chapter1Manager EnsureChapter1Manager(Scene scene, SetupResult report)
        {
            Chapter1Manager[] managers = FindSceneComponents<Chapter1Manager>(scene);
            if (managers.Length > 0)
            {
                if (managers.Length == 1)
                {
                    report.Pass($"Found Chapter1Manager: {GetHierarchyPath(managers[0].transform)}.");
                }
                else
                {
                    report.Warning($"Found {managers.Length} Chapter1Manager components. Setup will use the first one and will not delete the others.");
                }

                return managers[0];
            }

            GameObject managerRoot = EnsureManagersRoot(scene);
            Chapter1Manager manager = managerRoot.AddComponent<Chapter1Manager>();
            report.Pass($"Created Chapter1Manager on {GetHierarchyPath(manager.transform)}.");
            return manager;
        }

        private static Mission01AudioSeparatorManager EnsureMissionManager(Scene scene, Chapter1Manager chapterManager, SetupResult report)
        {
            Mission01AudioSeparatorManager[] managers = FindSceneComponents<Mission01AudioSeparatorManager>(scene);
            Mission01AudioSeparatorManager manager;
            if (managers.Length > 0)
            {
                manager = managers[0];
                if (managers.Length == 1)
                {
                    report.Pass($"Found Mission01AudioSeparatorManager: {GetHierarchyPath(manager.transform)}.");
                }
                else
                {
                    report.Warning($"Found {managers.Length} Mission01AudioSeparatorManager components. Setup did not create another duplicate.");
                }
            }
            else
            {
                GameObject root = EnsureManagersRoot(scene);
                GameObject managerObject = new GameObject(ManagerObjectName);
                managerObject.transform.SetParent(root.transform, false);
                manager = managerObject.AddComponent<Mission01AudioSeparatorManager>();
                report.Pass($"Created Mission01AudioSeparatorManager: {GetHierarchyPath(manager.transform)}.");
            }

            SerializedObject serialized = new SerializedObject(manager);
            SetObject(serialized, "chapterManager", chapterManager);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            return manager;
        }

        private static ItemDefinition EnsureAudioSeparatorItem(SetupResult report)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AudioSeparatorItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, AudioSeparatorItemPath);
                report.Pass($"Created item definition: {AudioSeparatorItemPath}.");
            }
            else
            {
                report.Pass($"Updated item definition: {AudioSeparatorItemPath}.");
            }

            SerializedObject serialized = new SerializedObject(item);
            SetString(serialized, "itemId", "audio_separator_device");
            SetString(serialized, "displayName", "Máy tách âm");
            SetString(serialized, "description", "Thiết bị dùng để lọc tạp âm và tách giọng nói khỏi bản ghi.");
            SetEnum(serialized, "category", ItemCategory.MissionItem);
            SetBool(serialized, "isStackable", false);
            SetInt(serialized, "maxStack", 1);
            SetBool(serialized, "isDroppable", false);
            SetBool(serialized, "isUsable", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static GameObject EnsureAudioSeparatorPrefab(int interactableLayer, SetupResult report)
        {
            Material material = EnsureMaterial(AudioSeparatorMaterialPath, new Color(0.16f, 0.18f, 0.20f, 1f));
            GameObject root = new GameObject(SceneAudioSeparatorName);
            try
            {
                if (interactableLayer >= 0)
                {
                    root.layer = interactableLayer;
                }

                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.56f, 0.28f, 0.36f);
                collider.center = new Vector3(0f, 0.14f, 0f);

                Mission01AudioSeparatorDeviceInteractable interactable =
                    root.AddComponent<Mission01AudioSeparatorDeviceInteractable>();
                ConfigureAudioSeparatorDevice(interactable, null, null);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "AudioSeparator_Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                body.transform.localScale = new Vector3(0.42f, 0.16f, 0.28f);
                SetRendererMaterial(body, material);
                RemoveCollider(body);

                GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                knob.name = "AudioSeparator_Dial";
                knob.transform.SetParent(root.transform, false);
                knob.transform.localPosition = new Vector3(0.11f, 0.23f, -0.02f);
                knob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                knob.transform.localScale = new Vector3(0.055f, 0.018f, 0.055f);
                SetRendererMaterial(knob, material);
                RemoveCollider(knob);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, AudioSeparatorPrefabPath);
                report.Pass($"{(saved != null ? "Created/updated" : "Failed to save")} AudioSeparator prefab: {AudioSeparatorPrefabPath}.");
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureAudioSeparatorDevice(
            Mission01AudioSeparatorDeviceInteractable interactable,
            Mission01AudioSeparatorManager manager,
            LanRecordingMissionController lanRecordingController)
        {
            if (interactable == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(interactable);
            SetObject(serialized, "missionManager", manager);
            SetObject(serialized, "lanRecordingController", lanRecordingController);
            SetString(serialized, "displayName", "máy tách âm");
            SetString(serialized, "interactionVerb", "Dùng");
            SetFloat(serialized, "interactionPriority", 100f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
        }

        private static void ConnectPhoneControllers(Mission01AudioSeparatorManager manager, SetupResult report)
        {
            PhoneUIController[] phones = Object.FindObjectsByType<PhoneUIController>(FindObjectsInactive.Include);
            if (phones.Length == 0)
            {
                report.Warning("No PhoneUIController found. Dũng contact is implemented in PhoneUIController but no scene phone UI was found.");
                return;
            }

            for (int i = 0; i < phones.Length; i++)
            {
                SerializedObject serialized = new SerializedObject(phones[i]);
                SetObject(serialized, "firstMissionManager", manager);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(phones[i]);
            }

            report.Pass($"Connected Mission 01 manager to {phones.Length} PhoneUIController component(s). Contact Dũng is hardcoded once in the existing Messenger list.");
        }

        private static void ConnectMinh(Mission01AudioSeparatorManager manager, SetupResult report)
        {
            MinhDialogueInteractable minh = Object.FindAnyObjectByType<MinhDialogueInteractable>(FindObjectsInactive.Include);
            if (minh == null)
            {
                report.Warning("MinhDialogueInteractable not found. Setup did not create a duplicate Minh.");
                return;
            }

            SerializedObject serialized = new SerializedObject(minh);
            SetObject(serialized, "mission01Manager", manager);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(minh);
            report.Pass($"Connected Minh dialogue: {GetHierarchyPath(minh.transform)}.");
        }

        private static DungRoomSetup EnsureDungRoom(Scene scene, Mission01AudioSeparatorManager manager, GameObject audioSeparatorPrefab, int interactableLayer, SetupResult report)
        {
            GameObject existingDoor = FindSceneObject(scene, "RoomDoor_Dung");
            Transform reference = existingDoor != null ? existingDoor.transform : null;
            bool placeholder = reference == null;

            GameObject room = FindSceneObject(scene, DungRoomName);
            if (room == null)
            {
                room = new GameObject(DungRoomName);
                MoveToScene(room, scene);
                room.transform.position = reference != null ? reference.position : Vector3.zero;
                room.transform.rotation = reference != null ? reference.rotation : Quaternion.identity;
                report.Pass($"Created {DungRoomName} {(reference != null ? "from RoomDoor_Dung reference" : "as placeholder marker at scene origin")}.");
            }
            else
            {
                report.Pass($"Found existing {DungRoomName}: {GetHierarchyPath(room.transform)}.");
            }

            if (placeholder)
            {
                GameObject marker = EnsureChild(room.transform, DungRoomMarkerName);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;
                report.Warning("RoomDoor_Dung was not found. Created DungRoom_Marker; move this marker to the real middle room on floor 2.");
                reference = marker.transform;
            }
            else
            {
                ConfigurePhysicalDungDoorKeypad(existingDoor, report);
            }

            GameObject doorMarker = EnsureChild(room.transform, DungRoomDoorName);
            if (reference != null)
            {
                doorMarker.transform.SetPositionAndRotation(reference.position, reference.rotation);
            }

            if (interactableLayer >= 0)
            {
                doorMarker.layer = interactableLayer;
            }

            BoxCollider doorCollider = EnsureComponent<BoxCollider>(doorMarker);
            doorCollider.isTrigger = true;
            doorCollider.size = new Vector3(1.3f, 2.1f, 0.45f);
            doorCollider.center = new Vector3(0f, 1.05f, 0f);

            GameObject keypadMarker = EnsureChild(room.transform, DungRoomKeypadName);
            keypadMarker.transform.position = reference.position + reference.right * 0.55f + Vector3.up * 1.15f;
            keypadMarker.transform.rotation = reference.rotation;
            Mission01KeypadUIController keypad = EnsureComponent<Mission01KeypadUIController>(keypadMarker);

            Transform doorToRotate = FindChildRecursive(existingDoor != null ? existingDoor.transform : doorMarker.transform, "Door_Room_Nam_Hinge");
            if (doorToRotate == null)
            {
                doorToRotate = existingDoor != null ? existingDoor.transform : doorMarker.transform;
            }

            Mission01DungDoorInteractable doorInteractable = EnsureComponent<Mission01DungDoorInteractable>(doorMarker);
            doorInteractable.Configure(manager, keypad, doorToRotate);
            SerializedObject doorSerialized = new SerializedObject(doorInteractable);
            SetObject(doorSerialized, "missionManager", manager);
            SetObject(doorSerialized, "keypadController", keypad);
            SetObject(doorSerialized, "doorToRotate", doorToRotate);
            SetString(doorSerialized, "displayName", "cửa phòng Dũng");
            SetString(doorSerialized, "interactionVerb", "Kiểm tra");
            SetFloat(doorSerialized, "interactionPriority", 120f);
            doorSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject interiorMarker = EnsureChild(room.transform, DungRoomInteriorMarkerName);
            interiorMarker.transform.position = reference.position + reference.forward * 1.4f;
            interiorMarker.transform.rotation = reference.rotation;

            GameObject spawn = EnsureChild(room.transform, AudioSeparatorSpawnName);
            Transform desk = FindBestDungDesk(scene);
            if (desk != null)
            {
                Bounds bounds = GetHierarchyBounds(desk);
                spawn.transform.position = bounds.center + Vector3.up * Mathf.Max(0.35f, bounds.extents.y + 0.12f);
                spawn.transform.rotation = desk.rotation;
                report.Pass($"Placed AudioSeparator_Spawn from desk reference: {GetHierarchyPath(desk)}.");
            }
            else if (spawn.transform.position == Vector3.zero || !Application.isPlaying)
            {
                spawn.transform.position = interiorMarker.transform.position + Vector3.up * 0.8f;
                spawn.transform.rotation = interiorMarker.transform.rotation;
                report.Warning("Could not identify Dũng's desk precisely. AudioSeparator_Spawn was derived from DungRoom_InteriorMarker; move it by hand if needed.");
            }

            EnsureSceneAudioSeparator(scene, spawn.transform, audioSeparatorPrefab, interactableLayer, manager, report);
            report.Pass("Ensured DungRoom structure: DungRoom_Door, DungRoom_Keypad, AudioSeparator_Spawn, DungRoom_InteriorMarker.");
            return new DungRoomSetup(placeholder, placeholder ? "DungRoom_Marker placeholder created; user should move it." : "RoomDoor_Dung used as reference.");
        }

        private static void ConfigurePhysicalDungDoorKeypad(GameObject existingDoor, SetupResult report)
        {
            if (existingDoor == null)
            {
                return;
            }

            RoomDoorKeypadController controller = EnsureComponent<RoomDoorKeypadController>(existingDoor);
            controller.ConfigureForMission01();
            EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

            Keypad physicalKeypad = existingDoor.GetComponentInChildren<Keypad>(true);
            if (physicalKeypad == null)
            {
                report.Warning("RoomDoor_Dung exists but no NavKeypad.Keypad was found under it; physical keypad password was not configured.");
                return;
            }

            SerializedObject serialized = new SerializedObject(physicalKeypad);
            SetInt(serialized, "keypadCombo", int.Parse(Mission01AudioSeparatorManager.CorrectDoorPassword));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(physicalKeypad);
            PrefabUtility.RecordPrefabInstancePropertyModifications(physicalKeypad);
            report.Pass("Configured physical RoomDoor_Dung NavKeypad keypadCombo to 2502.");
        }

        private static void EnsureSceneAudioSeparator(Scene scene, Transform spawn, GameObject prefab, int interactableLayer, Mission01AudioSeparatorManager manager, SetupResult report)
        {
            GameObject existing = FindSceneObject(scene, SceneAudioSeparatorName);
            if (existing != null)
            {
                report.Pass($"Found existing {SceneAudioSeparatorName}; preserved its current transform: {GetHierarchyPath(existing.transform)}.");
                EnsureAudioSeparatorComponents(scene, existing, interactableLayer, manager);
                return;
            }

            GameObject instance = null;
            if (prefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            }

            if (instance == null)
            {
                instance = new GameObject(SceneAudioSeparatorName);
                MoveToScene(instance, scene);
            }

            instance.name = SceneAudioSeparatorName;
            instance.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            instance.transform.SetParent(spawn, true);
            EnsureAudioSeparatorComponents(scene, instance, interactableLayer, manager);
            report.Pass($"Placed {SceneAudioSeparatorName} at AudioSeparator_Spawn.");
        }

        private static void EnsureAudioSeparatorComponents(Scene scene, GameObject target, int interactableLayer, Mission01AudioSeparatorManager manager)
        {
            if (interactableLayer >= 0)
            {
                SetLayerRecursive(target, interactableLayer);
            }

            BoxCollider collider = EnsureComponent<BoxCollider>(target);
            collider.isTrigger = true;
            if (collider.size == Vector3.zero)
            {
                collider.size = new Vector3(0.56f, 0.28f, 0.36f);
                collider.center = new Vector3(0f, 0.14f, 0f);
            }

            RemoveComponentIfPresent<ItemPickup>(target);
            RemoveComponentIfPresent<WorldPickupPersistence>(target);
            Mission01AudioSeparatorDeviceInteractable interactable =
                EnsureComponent<Mission01AudioSeparatorDeviceInteractable>(target);
            LanRecordingMissionController recordingController =
                FindFirstSceneComponent<LanRecordingMissionController>(scene);
            ConfigureAudioSeparatorDevice(interactable, manager, recordingController);
            EditorUtility.SetDirty(target);
        }

        private static void EnsureCalendarInteractable(Scene scene, int interactableLayer, SetupResult report)
        {
            GameObject calendar = FindSceneObject(scene, CalendarObjectName);
            if (calendar == null)
            {
                report.Warning($"{CalendarObjectName} not found. Calendar clue interaction was not connected; game will not crash, but the player should place the calendar.");
                return;
            }

            if (interactableLayer >= 0)
            {
                SetLayerRecursive(calendar, interactableLayer);
            }

            Mission01CalendarInteractable interactable = EnsureComponent<Mission01CalendarInteractable>(calendar);
            SerializedObject serialized = new SerializedObject(interactable);
            SetString(serialized, "displayName", "lịch");
            SetString(serialized, "interactionVerb", "Xem");
            SetFloat(serialized, "interactionPriority", 60f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            BoxCollider collider = calendar.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = calendar.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.42f, 0.48f, 0.08f);
                collider.center = Vector3.zero;
            }
            else
            {
                collider.isTrigger = true;
            }

            report.Pass($"Connected calendar clue interactable: {GetHierarchyPath(calendar.transform)}.");
        }

        private static void ValidateManagerCount<T>(Scene scene, string label, SetupResult report) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            if (components.Length == 1)
            {
                report.Pass($"{label} exists exactly once.");
            }
            else if (components.Length == 0)
            {
                report.Fail($"{label} missing.");
            }
            else
            {
                report.Fail($"{label} duplicated: {components.Length} components found.");
            }
        }

        private static void ValidatePhoneAndDungConversation(Scene scene, SetupResult report)
        {
            PhoneUIController[] phones = FindSceneComponents<PhoneUIController>(scene);
            if (phones.Length > 0)
            {
                report.Pass("PhoneUIController exists; Messenger includes fixed Dũng contact in PhoneUIController.");
            }
            else
            {
                report.Fail("PhoneUIController missing.");
            }

            List<Mission01DungChoice> choices = Mission01DungConversation.BuildChoices(FirstMissionState.MessageDung, Chapter1SaveData.CreateDefault());
            if (Mission01DungConversation.GetChoiceText(Mission01DungChoice.BorrowAudioSeparator).Length > 0 &&
                Mission01DungConversation.GetChoiceText(Mission01DungChoice.AskRoomPassword).Length > 0 &&
                Mission01DungConversation.GetChoiceText(Mission01DungChoice.AskBirthday).Length > 0)
            {
                report.Pass($"Dũng fixed dialogue choices exist. Initial choice count check: {choices.Count}.");
            }
            else
            {
                report.Fail("Dũng fixed dialogue choices missing.");
            }
        }

        private static void ValidateNoForbiddenAnswerLeak(SetupResult report)
        {
            string[] missionTexts =
            {
                Mission01DungConversation.BorrowQuestion,
                Mission01DungConversation.BorrowReplyYes,
                Mission01DungConversation.BorrowReplyRoom,
                Mission01DungConversation.PasswordQuestion,
                Mission01DungConversation.PasswordHint,
                Mission01DungConversation.BirthdayQuestion,
                Mission01DungConversation.BirthdayHint,
                Mission01DungConversation.BirthdayReminder,
                Mission01AudioSeparatorManager.GetObjective(FirstMissionState.SolveBirthdayPassword)
            };

            for (int i = 0; i < missionTexts.Length; i++)
            {
                if (Mission01DungConversation.ContainsForbiddenDirectAnswer(missionTexts[i]))
                {
                    report.Fail($"Forbidden direct answer leak in text: {missionTexts[i]}");
                    return;
                }
            }

            report.Pass("No message/objective reveals 2502 or 25/02 before the door solution.");
        }

        private static void ValidateNoMissionAiOrApi(SetupResult report)
        {
            string[] missionScriptPaths =
            {
                "Assets/Chapter1/Scripts/Missions/Mission01AudioSeparatorManager.cs",
                "Assets/Chapter1/Scripts/Missions/Mission01DungConversation.cs",
                "Assets/Chapter1/Scripts/UI/Phone/PhoneUIController.cs"
            };

            string[] forbidden = { "OpenAI", "API key", "Mock AI Provider", "Remote AI Provider", "HttpClient", "UnityWebRequest" };
            for (int i = 0; i < missionScriptPaths.Length; i++)
            {
                if (!File.Exists(missionScriptPaths[i]))
                {
                    continue;
                }

                string text = File.ReadAllText(missionScriptPaths[i], Encoding.UTF8);
                for (int j = 0; j < forbidden.Length; j++)
                {
                    if (text.IndexOf(forbidden[j], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.Fail($"Forbidden AI/API term '{forbidden[j]}' found in {missionScriptPaths[i]}.");
                        return;
                    }
                }
            }

            report.Pass("Mission 01 scripts do not use AI, API calls, backend, or API keys.");
        }

        private static void ValidateSceneObjectWithComponent<T>(Scene scene, string label, SetupResult report) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            if (components.Length > 0)
            {
                report.Pass($"{label} exists: {GetHierarchyPath(components[0].transform)}.");
            }
            else
            {
                report.Fail($"{label} missing.");
            }
        }

        private static void ValidateDungRoom(Scene scene, SetupResult report)
        {
            GameObject room = FindSceneObject(scene, DungRoomName);
            GameObject marker = FindSceneObject(scene, DungRoomMarkerName);
            if (room != null || marker != null)
            {
                report.Pass("DungRoom or DungRoom_Marker exists.");
            }
            else
            {
                report.Fail("DungRoom and DungRoom_Marker are missing.");
            }

            GameObject door = FindSceneObject(scene, DungRoomDoorName);
            GameObject keypad = FindSceneObject(scene, DungRoomKeypadName);
            if (door != null && door.GetComponent<Mission01DungDoorInteractable>() != null)
            {
                report.Pass("DungRoom_Door and Mission01DungDoorInteractable exist.");
            }
            else
            {
                report.Fail("DungRoom_Door or Mission01DungDoorInteractable missing.");
            }

            Mission01KeypadUIController keypadController = keypad != null ? keypad.GetComponent<Mission01KeypadUIController>() : null;
            if (keypadController != null && keypadController.CorrectPassword == Mission01AudioSeparatorManager.CorrectDoorPassword)
            {
                report.Pass("DungRoom_Keypad exists and password check is 2502.");
            }
            else
            {
                report.Fail("DungRoom_Keypad missing or password is not 2502.");
            }

            ValidatePhysicalDungDoorKeypad(scene, report);
        }

        private static void ValidatePhysicalDungDoorKeypad(Scene scene, SetupResult report)
        {
            GameObject physicalDoor = FindSceneObject(scene, "RoomDoor_Dung");
            if (physicalDoor == null)
            {
                report.Warning("RoomDoor_Dung missing; physical 3D keypad validation skipped.");
                return;
            }

            RoomDoorKeypadController controller =
                physicalDoor.GetComponent<RoomDoorKeypadController>();
            if (controller == null)
            {
                report.Fail("RoomDoor_Dung is missing RoomDoorKeypadController.");
            }
            else
            {
                report.Pass("RoomDoor_Dung has RoomDoorKeypadController for physical keypad flow.");
            }

            Keypad physicalKeypad = physicalDoor.GetComponentInChildren<Keypad>(true);
            if (physicalKeypad == null)
            {
                report.Fail("RoomDoor_Dung is missing NavKeypad.Keypad.");
                return;
            }

            SerializedObject serialized = new SerializedObject(physicalKeypad);
            SerializedProperty combo = serialized.FindProperty("keypadCombo");
            if (combo != null && combo.intValue == int.Parse(Mission01AudioSeparatorManager.CorrectDoorPassword))
            {
                report.Pass("Physical RoomDoor_Dung NavKeypad keypadCombo is 2502.");
            }
            else
            {
                report.Fail($"Physical RoomDoor_Dung NavKeypad keypadCombo is {(combo != null ? combo.intValue.ToString() : "unreadable")}, expected 2502.");
            }
        }

        private static void ValidateCalendar(Scene scene, SetupResult report)
        {
            GameObject calendar = FindSceneObject(scene, CalendarObjectName);
            if (calendar == null)
            {
                report.Warning($"{CalendarObjectName} missing. This is a clue warning, not a crash condition.");
                return;
            }

            if (calendar.GetComponent<Mission01CalendarInteractable>() != null)
            {
                report.Pass("WallCalendar_March25 exists and has clue interaction.");
            }
            else
            {
                report.Warning("WallCalendar_March25 exists but lacks Mission01CalendarInteractable.");
            }
        }

        private static void ValidateAudioSeparator(Scene scene, SetupResult report)
        {
            GameObject device = FindSceneObject(scene, SceneAudioSeparatorName);
            if (device != null && device.GetComponent<Mission01AudioSeparatorDeviceInteractable>() != null)
            {
                if (device.GetComponent<ItemPickup>() == null)
                {
                    report.Pass("AudioSeparator_Device exists as a world interaction, not an inventory pickup.");
                }
                else
                {
                    report.Fail("AudioSeparator_Device still has ItemPickup; setup should remove pickup behavior.");
                }
            }
            else
            {
                report.Fail("AudioSeparator_Device or Mission01AudioSeparatorDeviceInteractable missing.");
            }
        }

        private static void ValidateMissingReferences(Scene scene, SetupResult report)
        {
            List<GameObject> objects = GetSceneGameObjects(scene);
            for (int i = 0; i < objects.Count; i++)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]) > 0)
                {
                    report.Fail($"Missing script reference found on {GetHierarchyPath(objects[i].transform)}.");
                    return;
                }
            }

            report.Pass("No Missing Script components found in scene.");
        }

        private static void ValidateNoDuplicateNamedObjects(Scene scene, string name, SetupResult report)
        {
            int count = CountSceneObjects(scene, name);
            if (count <= 1)
            {
                report.Pass($"No duplicate object named {name}. Count: {count}.");
            }
            else
            {
                report.Fail($"Duplicate object named {name}: {count} found.");
            }
        }

        private static GameObject EnsureManagersRoot(Scene scene)
        {
            GameObject managers = FindSceneObject(scene, "Managers");
            if (managers != null)
            {
                return managers;
            }

            managers = new GameObject("Managers");
            MoveToScene(managers, scene);
            return managers;
        }

        private static Transform FindBestDungDesk(Scene scene)
        {
            string[] names = { "Dung_desktop", "desk_set_Dung", "Desk_Dung", "DungDesk" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject match = FindSceneObject(scene, names[i]);
                if (match != null)
                {
                    return match.transform;
                }
            }

            return null;
        }

        private static Bounds GetHierarchyBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one * 0.5f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetRendererMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RemoveComponentIfPresent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent))
            {
                return;
            }

            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void MoveToScene(GameObject gameObject, Scene scene)
        {
            if (scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> matches = new List<T>();
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                matches.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return matches.ToArray();
        }

        private static T FindFirstSceneComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                Transform match = FindChildRecursive(roots[i].transform, objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (string.Equals(parent.name, objectName, StringComparison.Ordinal))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform match = FindChildRecursive(parent.GetChild(i), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static List<GameObject> GetSceneGameObjects(Scene scene)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    gameObjects.Add(transforms[j].gameObject);
                }
            }

            return gameObjects;
        }

        private static int CountSceneObjects(Scene scene, string objectName)
        {
            int count = 0;
            List<GameObject> objects = GetSceneGameObjects(scene);
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null && string.Equals(objects[i].name, objectName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            StringBuilder builder = new StringBuilder(transform.name);
            Transform parent = transform.parent;
            while (parent != null)
            {
                builder.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return builder.ToString();
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
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

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
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

        private static void SetEnum<T>(SerializedObject serialized, string propertyName, T value) where T : Enum
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = Convert.ToInt32(value);
            }
        }

        private static void WriteReport(SetupResult report)
        {
            EnsureFolder("Assets/Chapter1/Documentation");
            File.WriteAllText(ReportPath, report.ToMarkdown(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
        }

        private static void ShowDialogIfRequested(bool showDialog, string title, SetupResult report)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(title, report.GetSummary(), "OK");
            }
        }

        private readonly struct DungRoomSetup
        {
            public DungRoomSetup(bool placeholder, string description)
            {
                Placeholder = placeholder;
                Description = description ?? string.Empty;
            }

            public bool Placeholder { get; }
            public string Description { get; }
        }

        public sealed class SetupResult
        {
            private readonly List<string> lines = new List<string>();

            public SetupResult(string title)
            {
                Title = title;
                lines.Add("# " + title);
                lines.Add(string.Empty);
            }

            public string Title { get; }
            public int PassCount { get; private set; }
            public int WarningCount { get; private set; }
            public int FailCount { get; private set; }

            public void Info(string message)
            {
                lines.Add("- INFO: " + message);
            }

            public void Pass(string message)
            {
                PassCount++;
                lines.Add("- PASS: " + message);
            }

            public void Warning(string message)
            {
                WarningCount++;
                lines.Add("- WARNING: " + message);
            }

            public void Fail(string message)
            {
                FailCount++;
                lines.Add("- FAIL: " + message);
            }

            public string GetSummary()
            {
                return $"{Title}\nPASS: {PassCount} | WARNING: {WarningCount} | FAIL: {FailCount}";
            }

            public string ToConsoleString()
            {
                return ToMarkdown();
            }

            public string ToMarkdown()
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < lines.Count; i++)
                {
                    builder.AppendLine(lines[i]);
                }

                builder.AppendLine();
                builder.AppendLine("## Required Notes");
                builder.AppendLine("- Created/updated code and scene wiring for Mission 01 – Borrow the Audio Separator.");
                builder.AppendLine("- Reused existing systems: Chapter1Manager, save data, PhoneUIController/Messenger, MinhDialogueInteractable, PlayerInventory, ItemPickup, interaction prompt flow, and PlayerInputLock.");
                builder.AppendLine("- Setup menu: Tools > Chapter 1 > Setup Mission 01 Audio Separator.");
                builder.AppendLine("- Validation menu: Tools > Chapter 1 > Validate Mission 01 Audio Separator.");
                builder.AppendLine("- Test flow: save Lan's recording, talk to Minh, message Dũng, inspect locked door, ask Dũng for hints, infer 2502 from the March 25 calendar, unlock door, pick up the audio separator, return to Minh.");
                builder.AppendLine("- Manual placement may still be needed for DungRoom_Marker or AudioSeparator_Spawn if the scene could not identify Dũng's exact desk/room.");
                builder.AppendLine("- This mission does not use AI, API calls, backend services, or API keys.");
                builder.AppendLine();
                builder.AppendLine($"PASS: {PassCount} | WARNING: {WarningCount} | FAIL: {FailCount}");
                return builder.ToString();
            }
        }
    }
}
