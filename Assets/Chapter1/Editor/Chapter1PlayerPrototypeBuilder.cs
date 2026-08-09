using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DormitoryMystery.Chapter1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1PlayerPrototypeBuilder
    {
        private const string InputActionsPath = "Assets/Chapter1/Settings/Chapter1Controls.inputactions";
        private const string InputReferencesFolderPath = "Assets/Chapter1/Settings/InputReferences";
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string PlayerModelPath = "Assets/Chapter1/ExternalAssets/Nam.fbx";
        private const string PlayerAnimatorControllerPath =
            "Assets/Chapter1/Animations/Controllers/Chapter1PlayerAnimator.controller";
        private const float PlayerModelScale = 0.52161586f;
        private const string CameraPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/ThirdPersonCameraRig.prefab";
        private const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string PlayerMaterialPath = "Assets/Chapter1/Materials/M_Player_Prototype.mat";
        private const string GroundMaterialPath = "Assets/Chapter1/Materials/M_Ground_Prototype.mat";
        private const string WallMaterialPath = "Assets/Chapter1/Materials/M_Wall_Prototype.mat";
        private const string GameplayMapName = "Gameplay";

        private static readonly string[] RequiredLayers =
        {
            "Player",
            "Environment",
            "Interactable",
            "Enemy",
            "HideSpot"
        };

        private static readonly InputReferenceBinding[] RequiredInputReferences =
        {
            new InputReferenceBinding("Move", "moveActionReference"),
            new InputReferenceBinding("Look", "lookActionReference"),
            new InputReferenceBinding("Attack", "attackActionReference"),
            new InputReferenceBinding("Kick", "kickActionReference"),
            new InputReferenceBinding("Jump", "jumpActionReference"),
            new InputReferenceBinding("Sprint", "sprintActionReference"),
            new InputReferenceBinding("Crouch", "crouchActionReference"),
            new InputReferenceBinding("Interact", "interactActionReference"),
            new InputReferenceBinding("Talk", "talkActionReference"),
            new InputReferenceBinding("ToggleFlashlight", "toggleFlashlightActionReference"),
            new InputReferenceBinding("ThrowCan", "throwCanActionReference"),
            new InputReferenceBinding("Pause", "pauseActionReference")
        };

        private readonly struct InputReferenceBinding
        {
            public InputReferenceBinding(string actionName, string fieldName)
            {
                ActionName = actionName;
                FieldName = fieldName;
            }

            public string ActionName { get; }
            public string FieldName { get; }
            public string ActionPath => $"{GameplayMapName}/{ActionName}";
            public string AssetPath => $"{InputReferencesFolderPath}/{ActionName}.inputactionreference.asset";
        }

        private sealed class InputReferenceSet
        {
            private readonly Dictionary<string, InputActionReference> referencesByActionName;

            public InputReferenceSet(InputActionAsset inputActionAsset, Dictionary<string, InputActionReference> referencesByActionName)
            {
                InputActionAsset = inputActionAsset;
                this.referencesByActionName = referencesByActionName;
            }

            public InputActionAsset InputActionAsset { get; }

            public bool TryGetReference(string actionName, out InputActionReference reference)
            {
                return referencesByActionName.TryGetValue(actionName, out reference);
            }
        }

        [MenuItem("Tools/Chapter 1/Build Player Prototype")]
        public static void BuildPlayerPrototype()
        {
            if (!ConfirmOverwriteIfNeeded())
            {
                Debug.LogWarning("[Chapter1 Builder] Đã hủy tạo Player Prototype.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Chapter1 Builder] Đã hủy vì Scene hiện tại chưa được lưu.");
                return;
            }

            EnsureFolders();
            EnsureTag("Player");
            EnsureLayers();
            DeleteExistingGeneratedAssets();

            InputActionAsset inputActionAsset = CreateInputActions();
            if (inputActionAsset == null)
            {
                ShowFailureDialog("Build Player Prototype", "Failed to create or load Chapter1Controls.inputactions.");
                return;
            }

            InputReferenceSet inputReferences = EnsureInputActionReferences(inputActionAsset);
            if (inputReferences == null)
            {
                ShowFailureDialog("Build Player Prototype", "Failed to create or repair persistent input action references.");
                return;
            }

            Material playerMaterial = CreateMaterial(PlayerMaterialPath, new Color(0.35f, 0.55f, 0.7f));
            Material groundMaterial = CreateMaterial(GroundMaterialPath, new Color(0.28f, 0.31f, 0.28f));
            Material wallMaterial = CreateMaterial(WallMaterialPath, new Color(0.42f, 0.40f, 0.36f));

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject playerPrefabSource = CreatePlayerPrototype(null, playerMaterial);
            if (!AssignInputActionReferences(playerPrefabSource.GetComponent<Chapter1InputReader>(), inputReferences, "player prefab source"))
            {
                ShowFailureDialog("Build Player Prototype", "Failed to assign input references before saving the player prefab.");
                return;
            }

            if (!ValidatePlayerInputReferences(playerPrefabSource, inputReferences, "player prefab source before save"))
            {
                ShowFailureDialog("Build Player Prototype", "Player prefab source still has invalid input references.");
                return;
            }

            GameObject savedPlayerPrefab = SavePlayerPrefab(playerPrefabSource);
            if (savedPlayerPrefab == null)
            {
                ShowFailureDialog("Build Player Prototype", "Failed to save Player prefab.");
                return;
            }

            if (!ValidateSavedPlayerPrefab(inputReferences))
            {
                ShowFailureDialog("Build Player Prototype", "Saved Player prefab failed input reference validation.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject sceneRoot = CreateGameObject("Chapter1_PlayerPrototype");
            GameObject managers = CreateChild(sceneRoot.transform, "Managers");
            GameObject environment = CreateChild(sceneRoot.transform, "Environment");
            GameObject lighting = CreateChild(sceneRoot.transform, "Lighting");
            GameObject debug = CreateChild(sceneRoot.transform, "Debug");

            GameObject player = InstantiatePlayerPrefab(sceneRoot.transform, scene);
            if (player == null)
            {
                ShowFailureDialog("Build Player Prototype", "Failed to instantiate Player into the prototype scene.");
                return;
            }

            GameObject cameraRig = CreateCameraRig(sceneRoot.transform, player);
            CreateManagers(managers.transform, player, cameraRig);
            CreateEnvironment(environment.transform, groundMaterial, wallMaterial);
            CreateLighting(lighting.transform);
            CreateDebug(debug.transform, player, cameraRig);

            if (!LinkPlayerReferences(player, cameraRig, inputReferences))
            {
                ShowFailureDialog("Build Player Prototype", "Failed to link player input references in the prototype scene.");
                return;
            }

            LinkCameraReferences(cameraRig, player);
            SaveCameraPrefab(cameraRig);

            if (!ValidatePlayerInputReferences(player, inputReferences, "scene Player before save"))
            {
                ShowFailureDialog("Build Player Prototype", "Scene Player failed input reference validation before scene save.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene reloadedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!ValidateScenePlayerInputReferences(reloadedScene, inputReferences, "scene Player after reload"))
            {
                ShowFailureDialog("Build Player Prototype", "Saved prototype scene failed input reference validation after reload.");
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("[Chapter1 Builder] Hoàn tất tạo Player Prototype. Mở scene Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity và bấm Play để kiểm tra.");
        }

        [MenuItem("Tools/Chapter 1/Repair Player Input References")]
        public static void RepairPlayerInputReferences()
        {
            if (!SaveCurrentModifiedScenesOrCancel("Repair Player Input References"))
            {
                Debug.LogWarning("[Chapter1 Builder] Repair canceled because current scene changes were not saved.");
                return;
            }

            EnsureFolders();

            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActionAsset == null)
            {
                LogBuilderError($"Missing Input Action Asset: {InputActionsPath}.");
                ShowFailureDialog("Repair Player Input References", "Missing Chapter1Controls.inputactions.");
                return;
            }

            InputReferenceSet inputReferences = EnsureInputActionReferences(inputActionAsset);
            if (inputReferences == null)
            {
                ShowFailureDialog("Repair Player Input References", "Failed to create or repair persistent input action references.");
                return;
            }

            bool prefabRepaired = RepairPlayerPrefab(inputReferences);
            bool sceneRepaired = RepairPrototypeScene(inputReferences);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefabRepaired && sceneRepaired)
            {
                EditorUtility.DisplayDialog("Repair Player Input References", "Player prefab and prototype scene input references were repaired successfully.", "OK");
                Debug.Log("[Chapter1 Builder] Repair Player Input References completed successfully.");
            }
            else
            {
                ShowFailureDialog("Repair Player Input References", "Repair failed. Check Console errors prefixed with [Chapter1 Builder] ERROR.");
            }
        }

        public static bool RepairPlayerInputReferencesForAutomation()
        {
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActionAsset == null)
            {
                LogBuilderError($"Missing Chapter1Controls.inputactions at '{InputActionsPath}'.");
                return false;
            }

            InputReferenceSet inputReferences = EnsureInputActionReferences(inputActionAsset);
            if (inputReferences == null)
            {
                LogBuilderError("Failed to create or repair persistent input action references.");
                return false;
            }

            bool prefabRepaired = RepairPlayerPrefab(inputReferences);
            bool sceneRepaired = RepairPrototypeScene(inputReferences);
            return prefabRepaired && sceneRepaired;
        }

        private static bool ConfirmOverwriteIfNeeded()
        {
            List<string> existingAssets = new List<string>();
            AddIfExists(existingAssets, InputActionsPath);
            AddIfExists(existingAssets, PlayerPrefabPath);
            AddIfExists(existingAssets, CameraPrefabPath);
            AddIfExists(existingAssets, ScenePath);
            AddIfExists(existingAssets, PlayerMaterialPath);
            AddIfExists(existingAssets, GroundMaterialPath);
            AddIfExists(existingAssets, WallMaterialPath);

            if (existingAssets.Count == 0)
            {
                return true;
            }

            string message = "Các asset prototype sau đã tồn tại và sẽ được tạo lại:\n\n" + string.Join("\n", existingAssets);
            return EditorUtility.DisplayDialog("Build Player Prototype", message, "Tạo lại", "Hủy");
        }

        private static void AddIfExists(List<string> existingAssets, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                existingAssets.Add(assetPath);
            }
        }

        private static void DeleteExistingGeneratedAssets()
        {
            DeleteAssetIfExists(InputActionsPath);
            DeleteAssetIfExists(ScenePath);
            DeleteAssetIfExists(PlayerMaterialPath);
            DeleteAssetIfExists(GroundMaterialPath);
            DeleteAssetIfExists(WallMaterialPath);
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Chapter1", "Settings");
            EnsureFolder("Assets/Chapter1/Settings", "InputReferences");
            EnsureFolder("Assets/Chapter1", "Materials");
            EnsureFolder("Assets/Chapter1/Prefabs", "Characters");
            EnsureFolder("Assets/Chapter1/Prefabs", "Gameplay");
            EnsureFolder("Assets/Chapter1", "Scenes");
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string fullPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void EnsureTag(string tagName)
        {
            if (InternalEditorUtility.tags.Contains(tagName))
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[Chapter1 Builder] Đã thêm tag '{tagName}'.");
        }

        private static void EnsureLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            foreach (string layerName in RequiredLayers)
            {
                if (LayerMask.NameToLayer(layerName) >= 0 || SerializedLayerExists(layers, layerName))
                {
                    continue;
                }

                int emptyIndex = FindEmptyUserLayer(layers);
                if (emptyIndex < 0)
                {
                    Debug.LogWarning($"[Chapter1 Builder] Không còn slot layer trống để thêm '{layerName}'.");
                    continue;
                }

                layers.GetArrayElementAtIndex(emptyIndex).stringValue = layerName;
                Debug.Log($"[Chapter1 Builder] Đã thêm layer '{layerName}' tại slot {emptyIndex}.");
            }

            tagManager.ApplyModifiedProperties();
        }

        private static bool SerializedLayerExists(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindEmptyUserLayer(SerializedProperty layers)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    return i;
                }
            }

            return -1;
        }

        private static InputActionAsset CreateInputActions()
        {
            InputActionAsset inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            inputActionAsset.name = "Chapter1Controls";

            InputActionMap gameplayMap = inputActionAsset.AddActionMap("Gameplay");

            InputAction move = gameplayMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            InputAction look = gameplayMap.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            InputAction attack = gameplayMap.AddAction("Attack", InputActionType.Button, expectedControlLayout: "Button");
            attack.AddBinding("<Mouse>/leftButton");

            InputAction kick = gameplayMap.AddAction("Kick", InputActionType.Button, expectedControlLayout: "Button");
            kick.AddBinding("<Mouse>/rightButton");
            kick.AddBinding("<Gamepad>/rightTrigger");

            InputAction jump = gameplayMap.AddAction("Jump", InputActionType.Button, expectedControlLayout: "Button");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            InputAction sprint = gameplayMap.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");
            sprint.AddBinding("<Gamepad>/leftShoulder");

            InputAction crouch = gameplayMap.AddAction("Crouch", InputActionType.Button);
            crouch.AddBinding("<Keyboard>/c");
            crouch.AddBinding("<Gamepad>/buttonEast");

            InputAction interact = gameplayMap.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/f");
            interact.AddBinding("<Gamepad>/buttonSouth");

            InputAction talk = gameplayMap.AddAction("Talk", InputActionType.Button);
            talk.AddBinding("<Keyboard>/e");
            talk.AddBinding("<Gamepad>/buttonNorth");

            InputAction toggleFlashlight = gameplayMap.AddAction("ToggleFlashlight", InputActionType.Button);
            toggleFlashlight.AddBinding("<Keyboard>/t");
            toggleFlashlight.AddBinding("<Gamepad>/dpad/up");

            InputAction throwCan = gameplayMap.AddAction("ThrowCan", InputActionType.Button);
            throwCan.AddBinding("<Keyboard>/e");
            throwCan.AddBinding("<Gamepad>/rightShoulder");

            InputAction pause = gameplayMap.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/startButton");

            string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), InputActionsPath);
            File.WriteAllText(physicalPath, inputActionAsset.ToJson());
            Object.DestroyImmediate(inputActionAsset);

            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            DisableGeneratedInputWrapper();

            InputActionAsset importedAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (importedAsset == null)
            {
                Debug.LogError($"[Chapter1 Builder] Không thể tạo Input Action Asset tại '{InputActionsPath}'.");
            }

            return importedAsset;
        }

        private static void DisableGeneratedInputWrapper()
        {
            AssetImporter importer = AssetImporter.GetAtPath(InputActionsPath);
            if (importer == null)
            {
                return;
            }

            SerializedObject importerObject = new SerializedObject(importer);
            SerializedProperty generateWrapperCode = importerObject.FindProperty("m_GenerateWrapperCode");
            if (generateWrapperCode != null)
            {
                generateWrapperCode.boolValue = false;
                importerObject.ApplyModifiedProperties();
                importer.SaveAndReimport();
            }
        }

        private static Material CreateMaterial(string assetPath, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                Debug.LogWarning("[Chapter1 Builder] Không tìm thấy shader URP Lit, tạm dùng shader Standard.");
            }

            Material material = new Material(shader)
            {
                color = color
            };

            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static GameObject CreatePlayerPrototype(Transform parent, Material playerMaterial)
        {
            GameObject player = CreateChild(parent, "Player");
            TryAssignTag(player, "Player");

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 50f;
            characterController.stepOffset = 0.35f;

            Chapter1InputReader inputReader = player.AddComponent<Chapter1InputReader>();
            PlayerInputLock inputLock = player.AddComponent<PlayerInputLock>();
            player.AddComponent<PlayerStamina>();
            Chapter1PlayerMotor playerMotor = player.AddComponent<Chapter1PlayerMotor>();

            GameObject cameraTarget = CreateChild(player.transform, "CameraTarget");
            cameraTarget.AddComponent<CameraTarget>();

            GameObject visual = CreateChild(player.transform, "Visual");
            PlayerVisualController visualController = player.AddComponent<PlayerVisualController>();

            GameObject modelAnchor = CreateChild(visual.transform, "ModelAnchor");
            GameObject playerModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            GameObject modelInstance = playerModel != null
                ? PrefabUtility.InstantiatePrefab(playerModel, modelAnchor.transform) as GameObject
                : null;

            if (modelInstance == null)
            {
                LogBuilderError($"Missing or invalid player model: {PlayerModelPath}.");
            }
            else
            {
                modelInstance.name = "Nam";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * PlayerModelScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(modelInstance.transform);
                RemoveNestedAnimators(modelInstance);

                SetSerializedObjectReference(visualController, "animatedModelRoot", modelInstance.transform);
            }

            SetSerializedObjectReference(visualController, "visualRoot", visual.transform);
            SetSerializedBool(visualController, "useLegacyLocomotion", false);
            SetSerializedObjectReference(visualController, "legacyAnimation", null);
            SetSerializedObjectReference(visualController, "walkClip", null);
            SetSerializedObjectReference(visualController, "runClip", null);

            RuntimeAnimatorController animatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
            Avatar avatar = LoadPlayerAvatar();
            if (animatorController == null)
            {
                LogBuilderError($"Missing player Animator Controller: {PlayerAnimatorControllerPath}.");
            }

            if (avatar == null)
            {
                LogBuilderError($"The Nam model does not contain a valid Humanoid Avatar: {PlayerModelPath}.");
            }

            Animator animator = player.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.keepAnimatorStateOnDisable = true;
            animator.enabled = true;

            GameObject attackPoint = CreateChild(player.transform, "AttackPoint");
            attackPoint.transform.localPosition = new Vector3(0f, 1f, 0.85f);

            PlayerCombatController combatController = player.AddComponent<PlayerCombatController>();
            SetSerializedObjectReference(combatController, "inputReader", inputReader);
            SetSerializedObjectReference(combatController, "playerMotor", playerMotor);
            SetSerializedObjectReference(combatController, "inputLock", inputLock);
            SetSerializedObjectReference(combatController, "playerVisualController", visualController);
            SetSerializedObjectReference(combatController, "legacyAnimationToPause", null);
            SetSerializedObjectReference(combatController, "animator", animator);
            SetSerializedObjectReference(combatController, "attackPoint", attackPoint.transform);
            SetSerializedObjectReference(combatController, "proceduralAnimationRoot", modelAnchor.transform);
            SetSerializedBool(combatController, "enableAnimatorOnlyDuringAttack", false);
            SetSerializedBool(combatController, "enableAnimatorWhileCrouching", true);
            SetSerializedBool(combatController, "enableAnimatorWhileIdle", true);
            SetSerializedBool(combatController, "enableAnimatorWhileMoving", true);
            SetSerializedBool(combatController, "enableAnimatorWhileJumping", true);
            SetSerializedBool(combatController, "suspendLegacyAnimationDuringAttack", false);
            SetSerializedInt(combatController, "enemyLayerMask", LayerMask.GetMask("Enemy"));

            if (player.GetComponent<CombatAnimationEventRelay>() == null)
            {
                player.AddComponent<CombatAnimationEventRelay>();
            }

            SetLayerRecursively(player, LayerMask.NameToLayer("Player"));
            return player;
        }

        private static Avatar LoadPlayerAvatar()
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath))
            {
                if (asset is Avatar avatar && avatar.isValid && avatar.isHuman)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void RemoveNestedAnimators(GameObject modelInstance)
        {
            foreach (Animator nestedAnimator in modelInstance.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(nestedAnimator, true);
            }
        }

        private static GameObject CreateCameraRig(Transform parent, GameObject player)
        {
            GameObject cameraRig = CreateChild(parent, "CameraRig");
            GameObject cameraPivot = CreateChild(cameraRig.transform, "CameraPivot");
            GameObject cameraObject = CreateChild(cameraPivot.transform, "Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.fieldOfView = 62f;
            cameraObject.AddComponent<AudioListener>();

            ThirdPersonCameraRig thirdPersonCameraRig = cameraRig.AddComponent<ThirdPersonCameraRig>();
            CameraTarget cameraTarget = player.GetComponentInChildren<CameraTarget>();
            SetSerializedObjectReference(thirdPersonCameraRig, "target", cameraTarget != null ? cameraTarget.transform : player.transform);
            SetSerializedObjectReference(thirdPersonCameraRig, "cameraPivot", cameraPivot.transform);
            SetSerializedObjectReference(thirdPersonCameraRig, "controlledCamera", camera);
            SetSerializedInt(thirdPersonCameraRig, "collisionMask", LayerMask.GetMask("Environment"));

            return cameraRig;
        }

        private static void CreateManagers(Transform parent, GameObject player, GameObject cameraRig)
        {
            GameObject managerObject = CreateChild(parent, "Chapter1Manager");
            Chapter1Manager chapter1Manager = managerObject.AddComponent<Chapter1Manager>();

            GameObject bootstrapObject = CreateChild(parent, "Chapter1GameplayBootstrap");
            Chapter1GameplayBootstrap bootstrap = bootstrapObject.AddComponent<Chapter1GameplayBootstrap>();
            SetSerializedObjectReference(bootstrap, "chapter1Manager", chapter1Manager);
            SetSerializedObjectReference(bootstrap, "player", player.transform);
            SetSerializedObjectReference(bootstrap, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetSerializedObjectReference(bootstrap, "cameraRig", cameraRig.GetComponent<ThirdPersonCameraRig>());
            SetSerializedObjectReference(bootstrap, "playerInputLock", player.GetComponent<PlayerInputLock>());
            SetSerializedObjectReference(bootstrap, "cameraTarget", player.GetComponentInChildren<CameraTarget>());
        }

        private static void CreateEnvironment(Transform parent, Material groundMaterial, Material wallMaterial)
        {
            int environmentLayer = LayerMask.NameToLayer("Environment");

            GameObject ground = CreatePrimitive(parent, "Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(30f, 0.2f, 30f), groundMaterial);
            SetLayerRecursively(ground, environmentLayer);

            GameObject wall01 = CreatePrimitive(parent, "TestWall_01", PrimitiveType.Cube, new Vector3(0f, 1.5f, 8f), Quaternion.identity, new Vector3(14f, 3f, 0.35f), wallMaterial);
            GameObject wall02 = CreatePrimitive(parent, "TestWall_02", PrimitiveType.Cube, new Vector3(-7f, 1.5f, 0f), Quaternion.identity, new Vector3(0.35f, 3f, 12f), wallMaterial);
            GameObject wall03 = CreatePrimitive(parent, "TestWall_03", PrimitiveType.Cube, new Vector3(7f, 1.5f, -2f), Quaternion.identity, new Vector3(0.35f, 3f, 12f), wallMaterial);
            SetLayerRecursively(wall01, environmentLayer);
            SetLayerRecursively(wall02, environmentLayer);
            SetLayerRecursively(wall03, environmentLayer);

            GameObject lowCeiling = CreatePrimitive(parent, "LowCeilingTest", PrimitiveType.Cube, new Vector3(3.5f, 1.25f, 2.5f), Quaternion.identity, new Vector3(4f, 0.2f, 4f), wallMaterial);
            SetLayerRecursively(lowCeiling, environmentLayer);

            GameObject obstacle = CreatePrimitive(parent, "RampOrObstacle", PrimitiveType.Cube, new Vector3(-2.5f, 0.35f, -3f), Quaternion.Euler(0f, 25f, 0f), new Vector3(3f, 0.7f, 1.4f), wallMaterial);
            SetLayerRecursively(obstacle, environmentLayer);
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject directionalLightObject = CreateChild(parent, "Directional Light");
            Light directionalLight = directionalLightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.1f;
            directionalLightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject optionalFillLightObject = CreateChild(parent, "OptionalFillLight");
            Light optionalFillLight = optionalFillLightObject.AddComponent<Light>();
            optionalFillLight.type = LightType.Point;
            optionalFillLight.intensity = 1.3f;
            optionalFillLight.range = 10f;
            optionalFillLightObject.transform.position = new Vector3(-4f, 3f, -4f);
        }

        private static void CreateDebug(Transform parent, GameObject player, GameObject cameraRig)
        {
            GameObject spawnPoint = CreateChild(parent, "SpawnPoint");
            spawnPoint.transform.position = Vector3.zero;

            PlayerDebugOverlay overlay = parent.gameObject.AddComponent<PlayerDebugOverlay>();
            SetSerializedObjectReference(overlay, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetSerializedObjectReference(overlay, "playerMotor", player.GetComponent<Chapter1PlayerMotor>());
            SetSerializedObjectReference(overlay, "playerStamina", player.GetComponent<PlayerStamina>());
            SetSerializedObjectReference(overlay, "inputLock", player.GetComponent<PlayerInputLock>());
            SetSerializedObjectReference(overlay, "cameraRig", cameraRig.GetComponent<ThirdPersonCameraRig>());
        }

        private static bool LinkPlayerReferences(GameObject player, GameObject cameraRig, InputReferenceSet inputReferences)
        {
            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            bool inputAssigned = AssignInputActionReferences(inputReader, inputReferences, player.name);

            Chapter1PlayerMotor playerMotor = player.GetComponent<Chapter1PlayerMotor>();
            Camera mainCamera = cameraRig.GetComponentInChildren<Camera>();
            PlayerVisualController visualController = player.GetComponent<PlayerVisualController>();
            CameraTarget cameraTarget = player.GetComponentInChildren<CameraTarget>();
            SetSerializedObjectReference(playerMotor, "cameraTransform", mainCamera != null ? mainCamera.transform : null);
            SetSerializedObjectReference(playerMotor, "playerVisualController", visualController);
            SetSerializedObjectReference(playerMotor, "cameraTarget", cameraTarget);
            SetSerializedInt(playerMotor, "standUpBlockingMask", LayerMask.GetMask("Environment"));

            SetSerializedObjectReference(cameraTarget, "playerMotor", playerMotor);
            return inputAssigned;
        }

        private static void LinkCameraReferences(GameObject cameraRig, GameObject player)
        {
            ThirdPersonCameraRig rig = cameraRig.GetComponent<ThirdPersonCameraRig>();
            SetSerializedObjectReference(rig, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetSerializedObjectReference(rig, "inputLock", player.GetComponent<PlayerInputLock>());
            SetSerializedObjectReference(rig, "target", player.GetComponentInChildren<CameraTarget>().transform);
            SetSerializedInt(rig, "collisionMask", LayerMask.GetMask("Environment"));
        }

        private static InputReferenceSet EnsureInputActionReferences(InputActionAsset inputActionAsset)
        {
            if (inputActionAsset == null)
            {
                LogBuilderError("Cannot create InputActionReference assets because the Input Action Asset is null.");
                return null;
            }

            EnsureFolder("Assets/Chapter1/Settings", "InputReferences");

            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                InputAction action = FindRequiredAction(inputActionAsset, binding);
                if (action == null)
                {
                    return null;
                }

                Object existingAsset = AssetDatabase.LoadAssetAtPath<Object>(binding.AssetPath);
                InputActionReference actionReference = existingAsset as InputActionReference;
                if (existingAsset != null && actionReference == null)
                {
                    LogBuilderError($"Asset at '{binding.AssetPath}' is not an InputActionReference.");
                    return null;
                }

                if (actionReference == null)
                {
                    actionReference = InputActionReference.Create(action);
                    actionReference.name = binding.ActionName;
                    AssetDatabase.CreateAsset(actionReference, binding.AssetPath);
                    Debug.Log($"[Chapter1 Builder] Created persistent InputActionReference for {binding.ActionPath}: {binding.AssetPath}");
                }
                else if (!ReferenceTargetsAction(actionReference, action))
                {
                    actionReference.Set(action);
                    actionReference.name = binding.ActionName;
                    EditorUtility.SetDirty(actionReference);
                    Debug.Log($"[Chapter1 Builder] Repaired persistent InputActionReference for {binding.ActionPath}: {binding.AssetPath}");
                }
                else
                {
                    Debug.Log($"[Chapter1 Builder] Reused persistent InputActionReference for {binding.ActionPath}: {binding.AssetPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            InputActionAsset reloadedInputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (reloadedInputAsset == null)
            {
                LogBuilderError($"Failed to reload Input Action Asset at '{InputActionsPath}'.");
                return null;
            }

            Dictionary<string, InputActionReference> references = new Dictionary<string, InputActionReference>(StringComparer.Ordinal);
            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                InputAction action = FindRequiredAction(reloadedInputAsset, binding);
                InputActionReference actionReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(binding.AssetPath);
                if (!ValidatePersistentReference(actionReference, action, binding, binding.AssetPath))
                {
                    return null;
                }

                references.Add(binding.ActionName, actionReference);
            }

            return new InputReferenceSet(reloadedInputAsset, references);
        }

        private static InputAction FindRequiredAction(InputActionAsset inputActionAsset, InputReferenceBinding binding)
        {
            try
            {
                return inputActionAsset.FindAction(binding.ActionPath, true);
            }
            catch (Exception exception)
            {
                LogBuilderError($"Missing required input action '{binding.ActionPath}'. {exception.Message}");
                return null;
            }
        }

        private static bool AssignInputActionReferences(Chapter1InputReader inputReader, InputReferenceSet references, string context)
        {
            if (inputReader == null)
            {
                LogBuilderError($"Cannot assign input references for '{context}' because Chapter1InputReader is missing.");
                return false;
            }

            bool success = true;
            SerializedObject serializedReader = new SerializedObject(inputReader);
            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                SerializedProperty property = serializedReader.FindProperty(binding.FieldName);
                if (property == null)
                {
                    LogBuilderError($"Missing serialized property '{binding.FieldName}' on '{context}'.");
                    success = false;
                    continue;
                }

                if (!references.TryGetReference(binding.ActionName, out InputActionReference actionReference))
                {
                    LogBuilderError($"Missing persistent InputActionReference for '{binding.ActionPath}'.");
                    success = false;
                    continue;
                }

                InputAction expectedAction = FindRequiredAction(references.InputActionAsset, binding);
                if (!ValidatePersistentReference(actionReference, expectedAction, binding, binding.AssetPath))
                {
                    success = false;
                    continue;
                }

                property.objectReferenceValue = actionReference;
                Debug.Log($"[Chapter1 Builder] Linked {binding.ActionPath} to {context}.{binding.FieldName}.");
            }

            if (!success)
            {
                return false;
            }

            serializedReader.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inputReader);
            EditorUtility.SetDirty(inputReader.gameObject);

            serializedReader.Update();
            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                SerializedProperty property = serializedReader.FindProperty(binding.FieldName);
                if (property == null || property.objectReferenceValue == null)
                {
                    LogBuilderError($"Read-back failed for '{context}.{binding.FieldName}'.");
                    success = false;
                }
            }

            return success;
        }

        private static bool ValidatePersistentReference(InputActionReference actionReference, InputAction expectedAction, InputReferenceBinding binding, string assetPath)
        {
            if (actionReference == null)
            {
                LogBuilderError($"InputActionReference for '{binding.ActionPath}' is null at '{assetPath}'.");
                return false;
            }

            if (!EditorUtility.IsPersistent(actionReference))
            {
                LogBuilderError($"InputActionReference for '{binding.ActionPath}' is not persistent: '{assetPath}'.");
                return false;
            }

            InputAction actualAction = actionReference.action;
            if (actualAction == null)
            {
                LogBuilderError($"InputActionReference for '{binding.ActionPath}' has null action: '{assetPath}'.");
                return false;
            }

            if (expectedAction == null)
            {
                LogBuilderError($"Expected action '{binding.ActionPath}' is null.");
                return false;
            }

            if (!ReferenceTargetsAction(actionReference, expectedAction))
            {
                LogBuilderError($"InputActionReference '{assetPath}' points to '{DescribeAction(actualAction)}' instead of '{binding.ActionPath}'.");
                return false;
            }

            return true;
        }

        private static bool ReferenceTargetsAction(InputActionReference actionReference, InputAction expectedAction)
        {
            if (actionReference == null || expectedAction == null)
            {
                return false;
            }

            InputAction actualAction = actionReference.action;
            if (actualAction == null || actualAction.actionMap == null || expectedAction.actionMap == null)
            {
                return false;
            }

            string actualAssetPath = actionReference.asset != null ? AssetDatabase.GetAssetPath(actionReference.asset) : string.Empty;
            string expectedAssetPath = expectedAction.actionMap.asset != null ? AssetDatabase.GetAssetPath(expectedAction.actionMap.asset) : string.Empty;
            return actualAction.id == expectedAction.id
                && string.Equals(actualAction.name, expectedAction.name, StringComparison.Ordinal)
                && string.Equals(actualAction.actionMap.name, GameplayMapName, StringComparison.Ordinal)
                && string.Equals(actualAssetPath, expectedAssetPath, StringComparison.Ordinal);
        }

        private static string DescribeAction(InputAction action)
        {
            if (action == null)
            {
                return "<null>";
            }

            string mapName = action.actionMap != null ? action.actionMap.name : "<no map>";
            return $"{mapName}/{action.name}";
        }

        private static InputActionReference GetReference(Dictionary<string, InputActionReference> references, string actionName)
        {
            if (references.TryGetValue(actionName, out InputActionReference actionReference))
            {
                return actionReference;
            }

            Debug.LogWarning($"[Chapter1 Builder] Không tìm thấy InputActionReference cho action '{actionName}'.");
            return null;
        }

        private static GameObject SavePlayerPrefab(GameObject player)
        {
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PlayerPrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return savedPrefab != null ? AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) : null;
        }

        private static void SaveCameraPrefab(GameObject cameraRig)
        {
            PrefabUtility.SaveAsPrefabAsset(cameraRig, CameraPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CameraPrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static GameObject InstantiatePlayerPrefab(Transform parent, Scene scene)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                LogBuilderError($"Missing player prefab: {PlayerPrefabPath}.");
                return null;
            }

            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
            if (player == null)
            {
                LogBuilderError($"Failed to instantiate player prefab: {PlayerPrefabPath}.");
                return null;
            }

            player.name = "Player";
            player.transform.SetParent(parent);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            return player;
        }

        private static bool RepairPlayerPrefab(InputReferenceSet inputReferences)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabAsset == null)
            {
                LogBuilderError($"Missing player prefab: {PlayerPrefabPath}.");
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Chapter1InputReader inputReader = prefabRoot.GetComponent<Chapter1InputReader>();
                if (!AssignInputActionReferences(inputReader, inputReferences, "Player prefab"))
                {
                    return false;
                }

                if (!ValidatePlayerInputReferences(prefabRoot, inputReferences, "Player prefab before save"))
                {
                    return false;
                }

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                if (savedPrefab == null)
                {
                    LogBuilderError($"Failed to save repaired player prefab: {PlayerPrefabPath}.");
                    return false;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PlayerPrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return ValidateSavedPlayerPrefab(inputReferences);
        }

        private static bool RepairPrototypeScene(InputReferenceSet inputReferences)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                LogBuilderError($"Missing prototype scene: {ScenePath}.");
                return false;
            }

            bool openedByRepair = false;
            Scene scene = GetLoadedScene(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedByRepair = true;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                LogBuilderError($"Failed to open prototype scene: {ScenePath}.");
                return false;
            }

            bool success = false;
            try
            {
                GameObject player = FindScenePlayer(scene);
                if (player == null)
                {
                    return false;
                }

                Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
                if (!AssignInputActionReferences(inputReader, inputReferences, "scene Player"))
                {
                    return false;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(player))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(inputReader);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    LogBuilderError($"Failed to save prototype scene: {ScenePath}.");
                    return false;
                }

                if (openedByRepair)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    Scene reloadedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    success = ValidateScenePlayerInputReferences(reloadedScene, inputReferences, "scene Player after reload");
                    EditorSceneManager.CloseScene(reloadedScene, true);
                }
                else
                {
                    success = ValidatePlayerInputReferences(player, inputReferences, "scene Player after save")
                        && ValidateSerializedFileHasInputReferences(ScenePath, "prototype scene file");
                }
            }
            finally
            {
                if (openedByRepair)
                {
                    Scene loadedScene = GetLoadedScene(ScenePath);
                    if (loadedScene.IsValid() && loadedScene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(loadedScene, true);
                    }
                }
            }

            return success;
        }

        private static bool ValidateSavedPlayerPrefab(InputReferenceSet inputReferences)
        {
            AssetDatabase.ImportAsset(PlayerPrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            return ValidatePlayerInputReferences(prefab, inputReferences, "saved Player prefab")
                && ValidateSerializedFileHasInputReferences(PlayerPrefabPath, "player prefab file");
        }

        private static bool ValidateScenePlayerInputReferences(Scene scene, InputReferenceSet inputReferences, string context)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                LogBuilderError($"Cannot validate '{context}' because the scene is not loaded.");
                return false;
            }

            GameObject player = FindScenePlayer(scene);
            return ValidatePlayerInputReferences(player, inputReferences, context)
                && ValidateSerializedFileHasInputReferences(ScenePath, "prototype scene file");
        }

        private static bool ValidatePlayerInputReferences(GameObject player, InputReferenceSet inputReferences, string context)
        {
            if (player == null)
            {
                LogBuilderError($"Cannot validate '{context}' because the player GameObject is missing.");
                return false;
            }

            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            if (inputReader == null)
            {
                LogBuilderError($"'{context}' is missing Chapter1InputReader.");
                return false;
            }

            bool success = true;
            SerializedObject serializedReader = new SerializedObject(inputReader);
            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                SerializedProperty property = serializedReader.FindProperty(binding.FieldName);
                if (property == null)
                {
                    LogBuilderError($"Missing serialized property '{binding.FieldName}' on '{context}'.");
                    success = false;
                    continue;
                }

                InputActionReference actionReference = property.objectReferenceValue as InputActionReference;
                InputAction expectedAction = FindRequiredAction(inputReferences.InputActionAsset, binding);
                if (!ValidatePersistentReference(actionReference, expectedAction, binding, $"{context}.{binding.FieldName}"))
                {
                    success = false;
                }
            }

            return success;
        }

        private static bool ValidateSerializedFileHasInputReferences(string assetPath, string context)
        {
            string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(physicalPath))
            {
                LogBuilderError($"Cannot validate serialized {context}; file does not exist: {assetPath}.");
                return false;
            }

            string text = File.ReadAllText(physicalPath);
            bool success = true;
            foreach (InputReferenceBinding binding in RequiredInputReferences)
            {
                string nullReferencePattern = $"{binding.FieldName}: {{fileID: 0}}";
                if (text.Contains(nullReferencePattern))
                {
                    LogBuilderError($"Serialized {context} still has null input reference field '{binding.FieldName}'.");
                    success = false;
                }
            }

            return success;
        }

        private static GameObject FindScenePlayer(Scene scene)
        {
            GameObject namedPlayer = FindSceneObject(scene, "Player");
            if (namedPlayer != null)
            {
                return namedPlayer;
            }

            List<Chapter1InputReader> inputReaders = GetSceneComponents<Chapter1InputReader>(scene);
            if (inputReaders.Count == 1)
            {
                Debug.LogWarning("[Chapter1 Builder] Scene player named 'Player' was not found; using the only Chapter1InputReader in the scene.");
                return inputReaders[0].gameObject;
            }

            LogBuilderError($"Could not find a unique Player in scene '{ScenePath}'. Found {inputReaders.Count} Chapter1InputReader components.");
            return null;
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

        private static bool SaveCurrentModifiedScenesOrCancel(string title)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    bool shouldSave = EditorUtility.DisplayDialog(title, "Current scenes have unsaved changes. Save them before repairing input references?", "Save", "Cancel");
                    if (!shouldSave)
                    {
                        return false;
                    }

                    return EditorSceneManager.SaveOpenScenes();
                }
            }

            return true;
        }

        private static void ShowFailureDialog(string title, string message)
        {
            LogBuilderError(message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static void LogBuilderError(string message)
        {
            Debug.LogError($"[Chapter1 Builder] ERROR: {message}");
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = CreateGameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            return gameObject;
        }

        private static GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Tạo Player Prototype Chương 1");
            return gameObject;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            Undo.RegisterCreatedObjectUndo(primitive, "Tạo môi trường prototype");
            primitive.name = name;
            primitive.transform.SetParent(parent);
            primitive.transform.position = position;
            primitive.transform.rotation = rotation;
            primitive.transform.localScale = scale;
            ApplyMaterial(primitive, material);
            return primitive;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void ApplyMaterial(GameObject gameObject, Material material)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void TryAssignTag(GameObject gameObject, string tagName)
        {
            try
            {
                gameObject.tag = tagName;
            }
            catch (UnityException exception)
            {
                Debug.LogWarning($"[Chapter1 Builder] Không thể gán tag '{tagName}' cho '{gameObject.name}'. Lỗi: {exception.Message}");
            }
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (gameObject == null || layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            }

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void SetSerializedObjectReference(Object targetObject, string propertyName, Object value)
        {
            if (targetObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Chapter1 Builder] Không tìm thấy serialized field '{propertyName}' trên '{targetObject.name}'.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedInt(Object targetObject, string propertyName, int value)
        {
            if (targetObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Chapter1 Builder] Không tìm thấy serialized field '{propertyName}' trên '{targetObject.name}'.");
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedBool(Object targetObject, string propertyName, bool value)
        {
            if (targetObject == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Chapter1 Builder] KhÃ´ng tÃ¬m tháº¥y serialized field '{propertyName}' trÃªn '{targetObject.name}'.");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
