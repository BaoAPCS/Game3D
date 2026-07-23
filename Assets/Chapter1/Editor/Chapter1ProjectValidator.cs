using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1ProjectValidator
    {
        private const string ExpectedUnityVersion = "6000.5.0f1";
        private const string InputActionsPath = "Assets/Chapter1/Settings/Chapter1Controls.inputactions";
        private const string InputReferencesFolderPath = "Assets/Chapter1/Settings/InputReferences";
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player_Minh_Prototype.prefab";
        private const string CameraPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/ThirdPersonCameraRig.prefab";
        private const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string GameplayMapName = "Gameplay";
        private const string FlashlightPickupPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/Pickup_Flashlight.prefab";
        private const string FusePickupPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/Pickup_Fuse.prefab";
        private const string CanPickupPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/Pickup_ThrowableCan.prefab";
        private const string TestInspectablePrefabPath = "Assets/Chapter1/Prefabs/Gameplay/TestInspectableTable.prefab";

        private static readonly string[] RequiredFolders =
        {
            "Assets/Chapter1",
            "Assets/Chapter1/Scenes",
            "Assets/Chapter1/Scripts",
            "Assets/Chapter1/Scripts/Core",
            "Assets/Chapter1/Scripts/Player",
            "Assets/Chapter1/Scripts/Camera",
            "Assets/Chapter1/Scripts/Interaction",
            "Assets/Chapter1/Scripts/Inventory",
            "Assets/Chapter1/Scripts/Items",
            "Assets/Chapter1/Scripts/UI",
            "Assets/Chapter1/Scripts/Dialogue",
            "Assets/Chapter1/Scripts/Puzzles",
            "Assets/Chapter1/Scripts/Enemy",
            "Assets/Chapter1/Scripts/World",
            "Assets/Chapter1/Scripts/Save",
            "Assets/Chapter1/Scripts/Ending",
            "Assets/Chapter1/Editor",
            "Assets/Chapter1/Settings",
            "Assets/Chapter1/Settings/InputReferences",
            "Assets/Chapter1/Prefabs",
            "Assets/Chapter1/Prefabs/Characters",
            "Assets/Chapter1/Prefabs/Environment",
            "Assets/Chapter1/Prefabs/Gameplay",
            "Assets/Chapter1/Materials",
            "Assets/Chapter1/Audio",
            "Assets/Chapter1/Textures",
            "Assets/Chapter1/Models",
            "Assets/Chapter1/Animations",
            "Assets/Chapter1/ScriptableObjects",
            "Assets/Chapter1/Tests"
        };

        private static readonly string[] RequiredActions =
        {
            "Move",
            "Look",
            "Sprint",
            "Crouch",
            "Interact",
            "ToggleFlashlight",
            "ThrowCan",
            "Pause"
        };

        private static readonly InputReferenceValidationEntry[] RequiredInputReferences =
        {
            new InputReferenceValidationEntry("Move", "moveActionReference"),
            new InputReferenceValidationEntry("Look", "lookActionReference"),
            new InputReferenceValidationEntry("Sprint", "sprintActionReference"),
            new InputReferenceValidationEntry("Crouch", "crouchActionReference"),
            new InputReferenceValidationEntry("Interact", "interactActionReference"),
            new InputReferenceValidationEntry("ToggleFlashlight", "toggleFlashlightActionReference"),
            new InputReferenceValidationEntry("ThrowCan", "throwCanActionReference"),
            new InputReferenceValidationEntry("Pause", "pauseActionReference")
        };

        private static readonly string[] RequiredLayers =
        {
            "Player",
            "Environment",
            "Interactable",
            "Enemy",
            "HideSpot"
        };

        private readonly struct InputReferenceValidationEntry
        {
            public InputReferenceValidationEntry(string actionName, string fieldName)
            {
                ActionName = actionName;
                FieldName = fieldName;
            }

            public string ActionName { get; }
            public string FieldName { get; }
            public string ActionPath => $"{GameplayMapName}/{ActionName}";
            public string AssetPath => $"{InputReferencesFolderPath}/{ActionName}.inputactionreference.asset";
        }

        [MenuItem("Tools/Chapter 1/Validate Project Setup")]
        public static void ValidateProjectSetup()
        {
            LogPass("Bắt đầu kiểm tra cấu hình Chương 1.");
            ValidateUnityVersion();
            ValidateRenderPipeline();
            ValidateInputSystem();
            ValidatePackage("AI Navigation", "com.unity.ai.navigation");
            ValidateTextMeshPro();
            ValidateCinemachineStatus();
            ValidateDefaultScene();
            ValidateFolders();
            ValidateCoreTypes();
            ValidatePrototypeTypes();
            ValidateScriptFiles();
            ValidateTagsAndLayers();
            ValidateInputActionAsset();
            ValidatePlayerPrefab();
            ValidateCameraPrefab();
            ValidatePrototypeScene();
            ValidateInteractionInventoryScripts();
            ValidateInteractionInventoryPlayerPrefab();
            ValidateInteractionInventoryPrefabs();
            ValidateInteractionInventoryScene();
            LogPass("Hoàn tất kiểm tra cấu hình Chương 1.");
        }

        private static void ValidateUnityVersion()
        {
            if (Application.unityVersion == ExpectedUnityVersion)
            {
                LogPass($"Unity version đúng: {Application.unityVersion}.");
                return;
            }

            LogWarning($"Unity version hiện tại là {Application.unityVersion}, yêu cầu mục tiêu là {ExpectedUnityVersion}.");
        }

        private static void ValidateRenderPipeline()
        {
            RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null)
            {
                LogWarning("Render pipeline hiện tại là Built-in Render Pipeline.");
                return;
            }

            string pipelineTypeName = pipelineAsset.GetType().FullName ?? pipelineAsset.GetType().Name;
            if (pipelineTypeName.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LogPass($"Render pipeline hiện tại là Universal Render Pipeline: {pipelineAsset.name}.");
                return;
            }

            LogWarning($"Render pipeline hiện tại là '{pipelineTypeName}' với asset '{pipelineAsset.name}'.");
        }

        private static void ValidateInputSystem()
        {
            ValidatePackage("Input System", "com.unity.inputsystem");

            string projectSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "ProjectSettings.asset");
            if (!File.Exists(projectSettingsPath))
            {
                LogWarning("Không tìm thấy ProjectSettings.asset để kiểm tra Active Input Handling.");
                return;
            }

            string settingsText = File.ReadAllText(projectSettingsPath);
            if (settingsText.Contains("activeInputHandler: 1"))
            {
                LogPass("Active Input Handling đang dùng Input System Package (New).");
            }
            else if (settingsText.Contains("activeInputHandler: 2"))
            {
                LogPass("Active Input Handling đang dùng Both.");
            }
            else if (settingsText.Contains("activeInputHandler: 0"))
            {
                LogWarning("Active Input Handling đang dùng Input Manager (Old).");
            }
            else
            {
                LogWarning("Không xác định được Active Input Handling từ ProjectSettings.asset.");
            }
        }

        private static void ValidateTextMeshPro()
        {
            PackageManagerPackageInfo directPackage = PackageManagerPackageInfo.FindForPackageName("com.unity.textmeshpro");
            PackageManagerPackageInfo uguiPackage = PackageManagerPackageInfo.FindForPackageName("com.unity.ugui");
            Type tmpType = FindLoadedType("TMPro.TMP_Text");

            if (directPackage != null)
            {
                LogPass($"TextMeshPro có package trực tiếp: {directPackage.name} {directPackage.version}.");
                return;
            }

            if (tmpType != null || uguiPackage != null)
            {
                string version = uguiPackage != null ? uguiPackage.version : "không xác định";
                LogPass($"TextMeshPro có thể sử dụng thông qua Unity UI package com.unity.ugui {version}.");
                return;
            }

            LogWarning("Chưa tìm thấy TextMeshPro trong project.");
        }

        private static void ValidateCinemachineStatus()
        {
            PackageManagerPackageInfo packageInfo = PackageManagerPackageInfo.FindForPackageName("com.unity.cinemachine");
            if (packageInfo == null)
            {
                LogPass("Cinemachine chưa được cài, đúng với prototype camera tự viết.");
                return;
            }

            LogWarning($"Cinemachine đang tồn tại ({packageInfo.version}) nhưng prototype Chương 1 không sử dụng package này.");
        }

        private static void ValidatePackage(string displayName, string packageName)
        {
            PackageManagerPackageInfo packageInfo = PackageManagerPackageInfo.FindForPackageName(packageName);
            if (packageInfo == null)
            {
                LogWarning($"{displayName}: chưa có package {packageName}.");
                return;
            }

            LogPass($"{displayName}: đã có {packageInfo.name} {packageInfo.version}.");
        }

        private static void ValidateDefaultScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.enabled)
                {
                    LogPass($"Scene mặc định đầu tiên trong Build Settings: {scene.path}.");
                    return;
                }
            }

            LogWarning("Chưa có scene nào được bật trong Build Settings.");
        }

        private static void ValidateFolders()
        {
            foreach (string folder in RequiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    LogPass($"Có thư mục {folder}.");
                }
                else
                {
                    LogError($"Thiếu thư mục {folder}.");
                }
            }
        }

        private static void ValidateCoreTypes()
        {
            Dictionary<string, string> requiredTypes = new Dictionary<string, string>
            {
                { "Chapter1Step", "DormitoryMystery.Chapter1.Chapter1Step" },
                { "Chapter1ItemId", "DormitoryMystery.Chapter1.Chapter1ItemId" },
                { "NamRelationshipLevel", "DormitoryMystery.Chapter1.NamRelationshipLevel" },
                { "HardDriveChoice", "DormitoryMystery.Chapter1.HardDriveChoice" },
                { "Chapter1SaveData", "DormitoryMystery.Chapter1.Chapter1SaveData" },
                { "Chapter1EventBus", "DormitoryMystery.Chapter1.Chapter1EventBus" },
                { "JsonChapter1SaveService", "DormitoryMystery.Chapter1.JsonChapter1SaveService" },
                { "NamTrustCalculator", "DormitoryMystery.Chapter1.NamTrustCalculator" },
                { "Chapter1Manager", "DormitoryMystery.Chapter1.Chapter1Manager" }
            };

            ValidateLoadedTypes("cơ bản", requiredTypes);
        }

        private static void ValidatePrototypeTypes()
        {
            Dictionary<string, string> requiredTypes = new Dictionary<string, string>
            {
                { "Chapter1InputReader", "DormitoryMystery.Chapter1.Chapter1InputReader" },
                { "PlayerInputLock", "DormitoryMystery.Chapter1.PlayerInputLock" },
                { "PlayerStamina", "DormitoryMystery.Chapter1.PlayerStamina" },
                { "Chapter1PlayerMotor", "DormitoryMystery.Chapter1.Chapter1PlayerMotor" },
                { "PlayerVisualController", "DormitoryMystery.Chapter1.PlayerVisualController" },
                { "CameraTarget", "DormitoryMystery.Chapter1.CameraTarget" },
                { "ThirdPersonCameraRig", "DormitoryMystery.Chapter1.ThirdPersonCameraRig" },
                { "Chapter1GameplayBootstrap", "DormitoryMystery.Chapter1.Chapter1GameplayBootstrap" },
                { "PlayerDebugOverlay", "DormitoryMystery.Chapter1.PlayerDebugOverlay" }
            };

            ValidateLoadedTypes("player prototype", requiredTypes);
        }

        private static void ValidateLoadedTypes(string groupName, Dictionary<string, string> requiredTypes)
        {
            foreach (KeyValuePair<string, string> typeEntry in requiredTypes)
            {
                if (FindLoadedType(typeEntry.Value) != null)
                {
                    LogPass($"Compile type {groupName} tồn tại: {typeEntry.Key}.");
                }
                else
                {
                    LogError($"Không tìm thấy compile type {groupName}: {typeEntry.Value}.");
                }
            }
        }

        private static void ValidateScriptFiles()
        {
            ValidateScriptFile("Chapter1Manager", "Assets/Chapter1/Scripts/Core/Chapter1Manager.cs");
            ValidateScriptFile("Chapter1SaveData", "Assets/Chapter1/Scripts/Save/Chapter1SaveData.cs");
            ValidateScriptFile("Chapter1EventBus", "Assets/Chapter1/Scripts/Core/Chapter1EventBus.cs");
            ValidateScriptFile("Chapter1InputReader", "Assets/Chapter1/Scripts/Player/Chapter1InputReader.cs");
            ValidateScriptFile("PlayerInputLock", "Assets/Chapter1/Scripts/Player/PlayerInputLock.cs");
            ValidateScriptFile("PlayerStamina", "Assets/Chapter1/Scripts/Player/PlayerStamina.cs");
            ValidateScriptFile("Chapter1PlayerMotor", "Assets/Chapter1/Scripts/Player/Chapter1PlayerMotor.cs");
            ValidateScriptFile("PlayerVisualController", "Assets/Chapter1/Scripts/Player/PlayerVisualController.cs");
            ValidateScriptFile("CameraTarget", "Assets/Chapter1/Scripts/Camera/CameraTarget.cs");
            ValidateScriptFile("ThirdPersonCameraRig", "Assets/Chapter1/Scripts/Camera/ThirdPersonCameraRig.cs");
            ValidateScriptFile("Chapter1GameplayBootstrap", "Assets/Chapter1/Scripts/Core/Chapter1GameplayBootstrap.cs");
            ValidateScriptFile("PlayerDebugOverlay", "Assets/Chapter1/Scripts/UI/PlayerDebugOverlay.cs");
            ValidateScriptFile("Chapter1PlayerPrototypeBuilder", "Assets/Chapter1/Editor/Chapter1PlayerPrototypeBuilder.cs");
            ValidateScriptFile("PlayerInventory", "Assets/Chapter1/Scripts/Inventory/PlayerInventory.cs");
            ValidateScriptFile("Chapter1Interactable", "Assets/Chapter1/Scripts/Interaction/Chapter1Interactable.cs");
            ValidateScriptFile("Chapter1InteractionController", "Assets/Chapter1/Scripts/Interaction/Chapter1InteractionController.cs");
            ValidateScriptFile("Chapter1InteractionRuntimeSelfTest", "Assets/Chapter1/Scripts/Interaction/Chapter1InteractionRuntimeSelfTest.cs");
            ValidateScriptFile("ItemPickup", "Assets/Chapter1/Scripts/Items/ItemPickup.cs");
            ValidateScriptFile("WorldPickupPersistence", "Assets/Chapter1/Scripts/Items/WorldPickupPersistence.cs");
            ValidateScriptFile("FlashlightController", "Assets/Chapter1/Scripts/Items/FlashlightController.cs");
            ValidateScriptFile("Chapter1HUD", "Assets/Chapter1/Scripts/UI/Chapter1HUD.cs");
            ValidateScriptFile("InteractionPromptUI", "Assets/Chapter1/Scripts/UI/InteractionPromptUI.cs");
            ValidateScriptFile("StaminaHUD", "Assets/Chapter1/Scripts/UI/StaminaHUD.cs");
            ValidateScriptFile("InventoryHUD", "Assets/Chapter1/Scripts/UI/InventoryHUD.cs");
            ValidateScriptFile("NotificationUI", "Assets/Chapter1/Scripts/UI/NotificationUI.cs");
            ValidateScriptFile("ObjectiveHUD", "Assets/Chapter1/Scripts/UI/ObjectiveHUD.cs");
            ValidateScriptFile("Chapter1InteractionInventoryBuilder", "Assets/Chapter1/Editor/Chapter1InteractionInventoryBuilder.cs");
            ValidateScriptFile("Chapter1InteractionHudDiagnostic", "Assets/Chapter1/Editor/Chapter1InteractionHudDiagnostic.cs");
        }

        private static void ValidateScriptFile(string displayName, string assetPath)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null)
            {
                LogPass($"Có script {displayName}: {assetPath}.");
                return;
            }

            LogError($"Thiếu script {displayName}: {assetPath}.");
        }

        private static void ValidateTagsAndLayers()
        {
            if (InternalEditorUtility.tags.Contains("Player"))
            {
                LogPass("Có tag Player.");
            }
            else
            {
                LogError("Thiếu tag Player. Chạy Tools/Chapter 1/Build Player Prototype để tạo tag bằng Editor API.");
            }

            foreach (string layerName in RequiredLayers)
            {
                if (LayerMask.NameToLayer(layerName) >= 0)
                {
                    LogPass($"Có layer {layerName}.");
                }
                else
                {
                    LogWarning($"Thiếu layer {layerName}; builder sẽ thêm nếu còn slot trống.");
                }
            }
        }

        private static void ValidateInputActionAsset()
        {
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActionAsset == null)
            {
                LogError($"Thiếu Input Action Asset: {InputActionsPath}. Hãy chạy Tools/Chapter 1/Build Player Prototype.");
                return;
            }

            LogPass($"Có Input Action Asset: {InputActionsPath}.");

            InputActionMap gameplayMap = inputActionAsset.FindActionMap("Gameplay", false);
            if (gameplayMap == null)
            {
                LogError("Input Action Asset thiếu action map Gameplay.");
                return;
            }

            LogPass("Input Action Asset có action map Gameplay.");
            foreach (string actionName in RequiredActions)
            {
                InputAction action = gameplayMap.FindAction(actionName, false);
                if (action == null)
                {
                    LogError($"Gameplay thiếu action {actionName}.");
                }
                else
                {
                    LogPass($"Gameplay có action {actionName}.");
                }
            }

            HashSet<string> actionReferences = new HashSet<string>(StringComparer.Ordinal);
            foreach (InputReferenceValidationEntry inputReference in RequiredInputReferences)
            {
                if (ValidateInputActionReferenceAsset(inputActionAsset, inputReference))
                {
                    actionReferences.Add(inputReference.ActionName);
                }
            }

            foreach (string actionName in RequiredActions)
            {
                if (actionReferences.Contains(actionName))
                {
                    LogPass($"Có InputActionReference cho action {actionName}.");
                }
                else
                {
                    LogWarning($"Chưa thấy InputActionReference sub-asset cho action {actionName}; hãy reimport asset nếu Unity chưa kịp tạo.");
                }
            }
        }

        private static bool ValidateInputActionReferenceAsset(InputActionAsset inputActionAsset, InputReferenceValidationEntry inputReference)
        {
            InputActionReference actionReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(inputReference.AssetPath);
            if (actionReference == null)
            {
                LogError($"Thiếu InputActionReference asset cho action {inputReference.ActionName}: {inputReference.AssetPath}.");
                return false;
            }

            return ValidateInputActionReference(inputActionAsset, actionReference, inputReference, inputReference.AssetPath);
        }

        private static bool ValidateInputActionReference(InputActionAsset inputActionAsset, InputActionReference actionReference, InputReferenceValidationEntry inputReference, string context)
        {
            if (actionReference == null)
            {
                LogError($"{context}: InputActionReference cho action {inputReference.ActionName} dang null.");
                return false;
            }

            if (!EditorUtility.IsPersistent(actionReference))
            {
                LogError($"{context}: InputActionReference cho action {inputReference.ActionName} khong phai persistent asset.");
                return false;
            }

            string referenceAssetPath = AssetDatabase.GetAssetPath(actionReference);
            if (!string.Equals(referenceAssetPath, inputReference.AssetPath, StringComparison.Ordinal))
            {
                LogError($"{context}: InputActionReference cho action {inputReference.ActionName} nam o '{referenceAssetPath}', ky vong '{inputReference.AssetPath}'.");
                return false;
            }

            InputAction actualAction = actionReference.action;
            if (actualAction == null)
            {
                LogError($"{context}: reference.action cho {inputReference.ActionName} dang null.");
                return false;
            }

            if (inputActionAsset == null)
            {
                LogError($"{context}: khong load duoc Input Action Asset de doi chieu {inputReference.ActionName}.");
                return false;
            }

            InputAction expectedAction = inputActionAsset.FindAction(inputReference.ActionPath, false);
            if (expectedAction == null)
            {
                LogError($"{context}: Input Action Asset thieu action {inputReference.ActionPath}.");
                return false;
            }

            if (actualAction.actionMap == null || !string.Equals(actualAction.actionMap.name, GameplayMapName, StringComparison.Ordinal))
            {
                LogError($"{context}: action {inputReference.ActionName} khong thuoc map {GameplayMapName}.");
                return false;
            }

            string actualAssetPath = actionReference.asset != null ? AssetDatabase.GetAssetPath(actionReference.asset) : string.Empty;
            string expectedAssetPath = expectedAction.actionMap != null && expectedAction.actionMap.asset != null
                ? AssetDatabase.GetAssetPath(expectedAction.actionMap.asset)
                : string.Empty;

            if (actualAction.id != expectedAction.id
                || !string.Equals(actualAction.name, expectedAction.name, StringComparison.Ordinal)
                || !string.Equals(actualAssetPath, expectedAssetPath, StringComparison.Ordinal))
            {
                LogError($"{context}: action reference {inputReference.ActionName} khong tro dung {inputReference.ActionPath} trong {InputActionsPath}.");
                return false;
            }

            LogPass($"InputActionReference {inputReference.ActionName} hop le va persistent: {inputReference.AssetPath}.");
            return true;
        }

        private static void ValidatePlayerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                LogError($"Thiếu prefab player: {PlayerPrefabPath}. Hãy chạy Tools/Chapter 1/Build Player Prototype.");
                return;
            }

            LogPass($"Có prefab player: {PlayerPrefabPath}.");
            ValidatePlayerObject(prefab, "prefab player");
            ValidateInputReaderReferences(prefab.GetComponent<Chapter1InputReader>());
        }

        private static void ValidateCameraPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            if (prefab == null)
            {
                LogError($"Thiếu prefab camera: {CameraPrefabPath}. Hãy chạy Tools/Chapter 1/Build Player Prototype.");
                return;
            }

            LogPass($"Có prefab camera: {CameraPrefabPath}.");
            ValidateCameraRigObject(prefab, "prefab camera");
        }

        private static void ValidatePrototypeScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                LogError($"Thiếu scene prototype: {ScenePath}. Hãy chạy Tools/Chapter 1/Build Player Prototype.");
                return;
            }

            LogPass($"Có scene prototype: {ScenePath}.");

            bool openedByValidator = false;
            Scene scene = GetLoadedScene(ScenePath);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedByValidator = true;
            }

            try
            {
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    LogError($"Không thể mở scene để kiểm tra: {ScenePath}.");
                    return;
                }

                List<Chapter1Manager> managers = GetSceneComponents<Chapter1Manager>(scene);
                if (managers.Count == 1)
                {
                    LogPass("Scene prototype có đúng 1 Chapter1Manager.");
                }
                else if (managers.Count == 0)
                {
                    LogError("Scene prototype thiếu Chapter1Manager.");
                }
                else
                {
                    LogWarning($"Scene prototype có {managers.Count} Chapter1Manager; nên giữ đúng 1 instance.");
                }

                List<Chapter1GameplayBootstrap> bootstraps = GetSceneComponents<Chapter1GameplayBootstrap>(scene);
                if (bootstraps.Count > 0)
                {
                    LogPass("Scene prototype có Chapter1GameplayBootstrap.");
                }
                else
                {
                    LogError("Scene prototype thiếu Chapter1GameplayBootstrap.");
                }

                List<Chapter1PlayerMotor> motors = GetSceneComponents<Chapter1PlayerMotor>(scene);
                if (motors.Count > 0)
                {
                    LogPass("Scene prototype có player motor.");
                    ValidatePlayerObject(motors[0].gameObject, "player trong scene");
                    ValidateInputReaderReferences(motors[0].GetComponent<Chapter1InputReader>());
                }
                else
                {
                    LogError("Scene prototype thiếu Chapter1PlayerMotor.");
                }

                List<ThirdPersonCameraRig> rigs = GetSceneComponents<ThirdPersonCameraRig>(scene);
                if (rigs.Count > 0)
                {
                    LogPass("Scene prototype có ThirdPersonCameraRig.");
                    ValidateCameraRigObject(rigs[0].gameObject, "camera rig trong scene");
                }
                else
                {
                    LogError("Scene prototype thiếu ThirdPersonCameraRig.");
                }

                ValidateSceneAudioListeners(scene);
                ValidateLowCeiling(scene);
            }
            finally
            {
                if (openedByValidator && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidatePlayerObject(GameObject player, string context)
        {
            ValidateRequiredComponent<CharacterController>(player, context);
            ValidateRequiredComponent<Chapter1InputReader>(player, context);
            ValidateRequiredComponent<PlayerInputLock>(player, context);
            ValidateRequiredComponent<PlayerStamina>(player, context);
            ValidateRequiredComponent<Chapter1PlayerMotor>(player, context);
            ValidateRequiredChildComponent<CameraTarget>(player, context);

            if (player.GetComponent<Rigidbody>() == null)
            {
                LogPass($"{context} không có Rigidbody ở root.");
            }
            else
            {
                LogError($"{context} không được gắn Rigidbody ở root.");
            }

            if (player.GetComponent("NavMeshAgent") == null)
            {
                LogPass($"{context} không có NavMeshAgent ở root.");
            }
            else
            {
                LogError($"{context} không được gắn NavMeshAgent ở root.");
            }

            if (player.tag == "Player")
            {
                LogPass($"{context} có tag Player.");
            }
            else
            {
                LogError($"{context} chưa gắn tag Player.");
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0 && player.layer == playerLayer)
            {
                LogPass($"{context} đang ở layer Player.");
            }
            else
            {
                LogWarning($"{context} chưa ở layer Player.");
            }
        }

        private static void ValidateCameraRigObject(GameObject cameraRig, string context)
        {
            ThirdPersonCameraRig rig = cameraRig.GetComponent<ThirdPersonCameraRig>();
            if (rig == null)
            {
                LogError($"{context} thiếu ThirdPersonCameraRig.");
                return;
            }

            LogPass($"{context} có ThirdPersonCameraRig.");

            Camera camera = cameraRig.GetComponentInChildren<Camera>(true);
            if (camera == null)
            {
                LogError($"{context} thiếu Camera con.");
            }
            else
            {
                LogPass($"{context} có Camera con.");
                if (camera.tag == "MainCamera")
                {
                    LogPass($"{context} có Camera tag MainCamera.");
                }
                else
                {
                    LogError($"{context} Camera chưa gắn tag MainCamera.");
                }

                if (camera.GetComponent<AudioListener>() != null)
                {
                    LogPass($"{context} Camera có AudioListener.");
                }
                else
                {
                    LogError($"{context} Camera thiếu AudioListener.");
                }
            }

            AudioListener[] listeners = cameraRig.GetComponentsInChildren<AudioListener>(true);
            if (listeners.Length == 1)
            {
                LogPass($"{context} có đúng 1 AudioListener.");
            }
            else
            {
                LogError($"{context} có {listeners.Length} AudioListener, yêu cầu đúng 1.");
            }

            SerializedObject rigObject = new SerializedObject(rig);
            SerializedProperty collisionMask = rigObject.FindProperty("collisionMask");
            if (collisionMask != null && collisionMask.intValue != 0)
            {
                LogPass($"{context} đã cấu hình collision mask cho camera.");
            }
            else
            {
                LogWarning($"{context} chưa có collision mask cho camera.");
            }

            SerializedProperty lockLookWhenInputLocked = rigObject.FindProperty("lockLookWhenInputLocked");
            if (lockLookWhenInputLocked != null && lockLookWhenInputLocked.boolValue)
            {
                LogPass($"{context} đang bật khóa xoay camera khi input bị khóa.");
            }
            else
            {
                LogWarning($"{context} chưa bật khóa xoay camera khi input bị khóa.");
            }
        }

        private static void ValidateInputReaderReferences(Chapter1InputReader inputReader)
        {
            if (inputReader == null)
            {
                return;
            }

            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            foreach (InputReferenceValidationEntry inputReference in RequiredInputReferences)
            {
                ValidateObjectReferenceField(inputReader, inputReference, inputActionAsset);
            }
        }

        private static void ValidateObjectReferenceField(Object target, InputReferenceValidationEntry inputReference, InputActionAsset inputActionAsset)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(inputReference.FieldName);
            if (property == null)
            {
                LogError($"Khong tim thay serialized field {inputReference.FieldName} tren {target.name}.");
                return;
            }

            InputActionReference actionReference = property.objectReferenceValue as InputActionReference;
            if (actionReference == null)
            {
                LogError($"{target.name} thieu reference action {inputReference.ActionName}.");
                return;
            }

            if (ValidateInputActionReference(inputActionAsset, actionReference, inputReference, $"{target.name}.{inputReference.FieldName}"))
            {
                LogPass($"{target.name} da lien ket action {inputReference.ActionName}.");
            }
        }

        private static void ValidateObjectReferenceField(Object target, string fieldName, string displayName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                LogError($"Không tìm thấy serialized field {fieldName} trên {target.name}.");
                return;
            }

            if (property.objectReferenceValue != null)
            {
                LogPass($"{target.name} đã liên kết action {displayName}.");
            }
            else
            {
                LogError($"{target.name} thiếu reference action {displayName}.");
            }
        }

        private static void ValidateSceneAudioListeners(Scene scene)
        {
            List<AudioListener> listeners = GetSceneComponents<AudioListener>(scene);
            if (listeners.Count == 1)
            {
                LogPass("Scene prototype có đúng 1 AudioListener.");
            }
            else
            {
                LogError($"Scene prototype có {listeners.Count} AudioListener, yêu cầu đúng 1.");
            }
        }

        private static void ValidateLowCeiling(Scene scene)
        {
            GameObject lowCeiling = FindSceneObject(scene, "LowCeilingTest");
            if (lowCeiling == null)
            {
                LogWarning("Scene prototype chưa có LowCeilingTest để kiểm tra crouch.");
                return;
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0 && lowCeiling.layer == environmentLayer)
            {
                LogPass("LowCeilingTest đang ở layer Environment.");
            }
            else
            {
                LogWarning("LowCeilingTest chưa ở layer Environment.");
            }
        }

        private static void ValidateRequiredComponent<T>(GameObject gameObject, string context) where T : Component
        {
            if (gameObject.GetComponent<T>() != null)
            {
                LogPass($"{context} có component {typeof(T).Name}.");
            }
            else
            {
                LogError($"{context} thiếu component {typeof(T).Name}.");
            }
        }

        private static void ValidateRequiredChildComponent<T>(GameObject gameObject, string context) where T : Component
        {
            if (gameObject.GetComponentInChildren<T>(true) != null)
            {
                LogPass($"{context} có component con {typeof(T).Name}.");
            }
            else
            {
                LogError($"{context} thiếu component con {typeof(T).Name}.");
            }
        }

        private static Scene GetLoadedScene(string assetPath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (string.Equals(scene.path, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return scene;
                }
            }

            return default;
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            foreach (GameObject root in roots)
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            foreach (GameObject root in roots)
            {
                Transform match = FindChildRecursive(root.transform, objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform match = FindChildRecursive(child, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void ValidateInteractionInventoryScripts()
        {
            Dictionary<string, string> requiredTypes = new Dictionary<string, string>
            {
                { "PlayerInventory", "DormitoryMystery.Chapter1.PlayerInventory" },
                { "Chapter1Interactable", "DormitoryMystery.Chapter1.Chapter1Interactable" },
                { "Chapter1InteractionController", "DormitoryMystery.Chapter1.Chapter1InteractionController" },
                { "Chapter1InteractionRuntimeSelfTest", "DormitoryMystery.Chapter1.Chapter1InteractionRuntimeSelfTest" },
                { "ItemPickup", "DormitoryMystery.Chapter1.ItemPickup" },
                { "WorldPickupPersistence", "DormitoryMystery.Chapter1.WorldPickupPersistence" },
                { "FlashlightController", "DormitoryMystery.Chapter1.FlashlightController" },
                { "Chapter1HUD", "DormitoryMystery.Chapter1.Chapter1HUD" },
                { "InteractionPromptUI", "DormitoryMystery.Chapter1.InteractionPromptUI" },
                { "StaminaHUD", "DormitoryMystery.Chapter1.StaminaHUD" },
                { "InventoryHUD", "DormitoryMystery.Chapter1.InventoryHUD" },
                { "NotificationUI", "DormitoryMystery.Chapter1.NotificationUI" },
                { "ObjectiveHUD", "DormitoryMystery.Chapter1.ObjectiveHUD" },
                { "Chapter1InteractionInventoryBuilder", "DormitoryMystery.Chapter1.Editor.Chapter1InteractionInventoryBuilder" },
                { "Chapter1InteractionHudDiagnostic", "DormitoryMystery.Chapter1.Editor.Chapter1InteractionHudDiagnostic" }
            };

            ValidateLoadedTypes("interaction inventory", requiredTypes);
        }

        private static void ValidateInteractionInventoryPlayerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                LogError($"Thiếu prefab player để kiểm tra interaction inventory: {PlayerPrefabPath}.");
                return;
            }

            ValidateSingleComponent<PlayerInventory>(prefab, "prefab player");
            Chapter1InteractionController interactionController = ValidateSingleComponent<Chapter1InteractionController>(prefab, "prefab player");
            FlashlightController flashlightController = ValidateSingleComponent<FlashlightController>(prefab, "prefab player");
            ValidateInputReaderReferences(prefab.GetComponent<Chapter1InputReader>());

            if (interactionController != null)
            {
                ValidateSerializedObjectReference(interactionController, "inputReader", "InteractionController inputReader");
                ValidateSerializedObjectReference(interactionController, "inventory", "InteractionController inventory");
                ValidateLayerMaskIncludes(interactionController, "interactionMask", "Interactable", "InteractionController interactionMask");
            }

            if (flashlightController != null)
            {
                Light flashlightLight = ValidateSerializedObjectReference(flashlightController, "flashlightLight", "FlashlightController flashlightLight") as Light;
                if (flashlightLight != null && !flashlightLight.enabled)
                {
                    LogPass("Flashlight Light mặc định đang tắt.");
                }
                else
                {
                    LogError("Flashlight Light phải tồn tại và mặc định tắt.");
                }
            }
        }

        private static void ValidateInteractionInventoryPrefabs()
        {
            ValidatePickupPrefab(FlashlightPickupPrefabPath, Chapter1ItemId.Flashlight);
            ValidatePickupPrefab(FusePickupPrefabPath, Chapter1ItemId.Fuse);
            ValidatePickupPrefab(CanPickupPrefabPath, Chapter1ItemId.ThrowableCan);
            ValidateInspectablePrefab();
        }

        private static void ValidatePickupPrefab(string prefabPath, Chapter1ItemId expectedItem)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                LogError($"Thiếu prefab pickup: {prefabPath}.");
                return;
            }

            LogPass($"Có prefab pickup: {prefabPath}.");
            ItemPickup pickup = prefab.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                LogError($"{prefab.name} thiếu ItemPickup.");
            }
            else if (pickup.ItemId == expectedItem)
            {
                LogPass($"{prefab.name} có ItemPickup item đúng: {expectedItem}.");
            }
            else
            {
                LogError($"{prefab.name} có ItemPickup item {pickup.ItemId}, yêu cầu {expectedItem}.");
            }

            ValidatePrefabColliderAndLayer(prefab, "pickup prefab");
            ValidatePersistentId(prefab.GetComponent<WorldPickupPersistence>(), prefab.name);
        }

        private static void ValidateInspectablePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestInspectablePrefabPath);
            if (prefab == null)
            {
                LogError($"Thiếu prefab inspectable: {TestInspectablePrefabPath}.");
                return;
            }

            LogPass($"Có prefab inspectable: {TestInspectablePrefabPath}.");
            ValidateRequiredComponent<TestInspectableInteractable>(prefab, "TestInspectableTable prefab");
            ValidatePrefabColliderAndLayer(prefab, "TestInspectableTable prefab");
        }

        private static void ValidateInteractionInventoryScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                LogError($"Thiếu scene để kiểm tra interaction inventory: {ScenePath}.");
                return;
            }

            bool openedByValidator = false;
            Scene scene = GetLoadedScene(ScenePath);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedByValidator = true;
            }

            try
            {
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    LogError($"Không thể mở scene để kiểm tra interaction inventory: {ScenePath}.");
                    return;
                }

                ValidateSceneComponentCount<Chapter1PlayerMotor>(scene, "Player", 1);
                ValidateSceneComponentCount<Camera>(scene, "Camera", 1);
                ValidateSceneComponentCount<Chapter1HUD>(scene, "Chapter1HUD", 1);
                ValidateSceneComponentCount<EventSystem>(scene, "EventSystem", 1);
                ValidateSceneComponentCount<InteractionPromptUI>(scene, "InteractionPromptUI", 1);
                ValidateSceneComponentCount<NotificationUI>(scene, "NotificationUI", 1);
                ValidateSceneComponentCount<StaminaHUD>(scene, "StaminaHUD", 1);
                ValidateSceneComponentCount<InventoryHUD>(scene, "InventoryHUD", 1);
                ValidateSceneComponentCount<ObjectiveHUD>(scene, "ObjectiveHUD", 1);

                EventSystem eventSystem = GetSceneComponents<EventSystem>(scene).Count > 0 ? GetSceneComponents<EventSystem>(scene)[0] : null;
                if (eventSystem != null && eventSystem.GetComponent<InputSystemUIInputModule>() != null)
                {
                    LogPass("EventSystem dùng InputSystemUIInputModule.");
                }
                else
                {
                    LogError("EventSystem thiếu InputSystemUIInputModule.");
                }

                GameObject testGroup = FindSceneObject(scene, "InteractionInventoryTest");
                if (testGroup != null)
                {
                    LogPass("Scene có InteractionInventoryTest.");
                }
                else
                {
                    LogError("Scene thiếu InteractionInventoryTest.");
                }

                ValidateSceneNamedObject(scene, "Pickup_Flashlight_Test");
                ValidateSceneNamedObject(scene, "Pickup_Fuse_Test");
                ValidateSceneNamedObject(scene, "Pickup_Can_Test_01");
                ValidateSceneNamedObject(scene, "Pickup_Can_Test_02");
                ValidateSceneNamedObject(scene, "Pickup_Can_Test_03");
                ValidateSceneNamedObject(scene, "TestInspectableTable");
                ValidateScenePickupIds(scene);
                ValidateSceneCrosshair(scene);
                ValidateSceneRuntimeSelfTest(scene);

                Chapter1PlayerMotor playerMotor = GetSceneComponents<Chapter1PlayerMotor>(scene).Count > 0 ? GetSceneComponents<Chapter1PlayerMotor>(scene)[0] : null;
                if (playerMotor != null)
                {
                    ValidateSingleComponent<PlayerInventory>(playerMotor.gameObject, "player trong scene");
                    ValidateSingleComponent<Chapter1InteractionController>(playerMotor.gameObject, "player trong scene");
                    ValidateSingleComponent<FlashlightController>(playerMotor.gameObject, "player trong scene");
                    ValidateInputReaderReferences(playerMotor.GetComponent<Chapter1InputReader>());
                }

                Chapter1InteractionHudDiagnostic.DiagnosticResult diagnosticResult = Chapter1InteractionHudDiagnostic.RunDiagnosticForScene(scene, false);
                if (diagnosticResult.ErrorCount == 0)
                {
                    LogPass($"Interaction/HUD diagnostic không còn ERROR. PASS={diagnosticResult.PassCount}, WARNING={diagnosticResult.WarningCount}.");
                }
                else
                {
                    LogError($"Interaction/HUD diagnostic còn {diagnosticResult.ErrorCount} ERROR. Chạy Tools/Chapter 1/Diagnose Interaction and HUD để xem chi tiết.");
                }
            }
            finally
            {
                if (openedByValidator && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static T ValidateSingleComponent<T>(GameObject gameObject, string context) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length == 1)
            {
                LogPass($"{context} có đúng 1 component {typeof(T).Name}.");
                return components[0];
            }

            LogError($"{context} có {components.Length} component {typeof(T).Name}, yêu cầu đúng 1.");
            return components.Length > 0 ? components[0] : null;
        }

        private static Object ValidateSerializedObjectReference(Object target, string fieldName, string label)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null && property.objectReferenceValue != null)
            {
                LogPass($"{label} đã được gán.");
                return property.objectReferenceValue;
            }

            LogError($"{label} chưa được gán.");
            return null;
        }

        private static void ValidateLayerMaskIncludes(Object target, string fieldName, string layerName, string label)
        {
            int layer = LayerMask.NameToLayer(layerName);
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null && layer >= 0 && (property.intValue & (1 << layer)) != 0)
            {
                LogPass($"{label} gồm layer {layerName}.");
            }
            else
            {
                LogError($"{label} thiếu layer {layerName}.");
            }
        }

        private static void ValidatePrefabColliderAndLayer(GameObject prefab, string context)
        {
            if (prefab.GetComponentInChildren<Collider>(true) != null)
            {
                LogPass($"{context} có Collider.");
            }
            else
            {
                LogError($"{context} thiếu Collider.");
            }

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0 && prefab.layer == interactableLayer)
            {
                LogPass($"{context} ở layer Interactable.");
            }
            else
            {
                LogError($"{context} chưa ở layer Interactable.");
            }
        }

        private static void ValidatePersistentId(WorldPickupPersistence persistence, string context)
        {
            if (persistence != null && !string.IsNullOrWhiteSpace(persistence.PersistentId))
            {
                LogPass($"{context} có persistent ID: {persistence.PersistentId}.");
            }
            else
            {
                LogError($"{context} thiếu persistent ID.");
            }
        }

        private static void ValidateSceneComponentCount<T>(Scene scene, string label, int expectedCount) where T : Component
        {
            int count = GetSceneComponents<T>(scene).Count;
            if (count == expectedCount)
            {
                LogPass($"Scene có đúng {expectedCount} {label}.");
            }
            else
            {
                LogError($"Scene có {count} {label}, yêu cầu {expectedCount}.");
            }
        }

        private static void ValidateSceneNamedObject(Scene scene, string objectName)
        {
            if (FindSceneObject(scene, objectName) != null)
            {
                LogPass($"Scene có {objectName}.");
            }
            else
            {
                LogError($"Scene thiếu {objectName}.");
            }
        }

        private static void ValidateScenePickupIds(Scene scene)
        {
            List<WorldPickupPersistence> persistences = GetSceneComponents<WorldPickupPersistence>(scene);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int canCount = 0;
            foreach (WorldPickupPersistence persistence in persistences)
            {
                ValidatePersistentId(persistence, persistence.gameObject.name);
                if (!string.IsNullOrWhiteSpace(persistence.PersistentId) && !ids.Add(persistence.PersistentId))
                {
                    LogError($"Persistent ID bị trùng: {persistence.PersistentId}.");
                }

                ItemPickup pickup = persistence.GetComponent<ItemPickup>();
                if (pickup != null && pickup.ItemId == Chapter1ItemId.ThrowableCan)
                {
                    canCount++;
                }
            }

            if (canCount == 3)
            {
                LogPass("Scene có đúng 3 lon nước pickup.");
            }
            else
            {
                LogError($"Scene có {canCount} lon nước pickup, yêu cầu 3.");
            }
        }

        private static void ValidateSceneCrosshair(Scene scene)
        {
            GameObject crosshair = FindSceneObject(scene, "Crosshair");
            if (crosshair == null)
            {
                LogError("Scene thiếu Crosshair trong Chapter1HUD.");
                return;
            }

            Image image = crosshair.GetComponent<Image>();
            RectTransform rect = crosshair.GetComponent<RectTransform>();
            if (image != null && rect != null && !image.raycastTarget)
            {
                LogPass("Scene có Crosshair Image không chặn raycast.");
            }
            else
            {
                LogError("Crosshair thiếu Image/RectTransform hợp lệ hoặc đang Raycast Target.");
            }
        }

        private static void ValidateSceneRuntimeSelfTest(Scene scene)
        {
            GameObject testGroup = FindSceneObject(scene, "InteractionInventoryTest");
            Chapter1InteractionRuntimeSelfTest selfTest = testGroup != null ? testGroup.GetComponent<Chapter1InteractionRuntimeSelfTest>() : null;
            if (selfTest != null)
            {
                LogPass("InteractionInventoryTest có Chapter1InteractionRuntimeSelfTest.");
            }
            else
            {
                LogError("InteractionInventoryTest thiếu Chapter1InteractionRuntimeSelfTest.");
            }
        }

        private static void LogPass(string message)
        {
            Debug.Log($"[Chapter1 Validator] PASS: {message}");
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[Chapter1 Validator] WARNING: {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[Chapter1 Validator] ERROR: {message}");
        }
    }
}
