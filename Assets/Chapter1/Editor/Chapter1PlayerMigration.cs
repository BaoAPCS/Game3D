using System;
using System.IO;
using System.Linq;
using DormitoryMystery.Chapter1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1PlayerMigration
    {
        private const string PlayerPrefabGuid = "062497d03c3ce134b8d5164b06fb04e6";
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string ModelPath = "Assets/Chapter1/ExternalAssets/Nam.fbx";
        private const string AnimatorControllerPath =
            "Assets/Chapter1/Animations/Controllers/Chapter1PlayerAnimator.controller";
        private const string DormitoryScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string PrototypeScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const float ModelScale = 0.52161586f;

        [MenuItem("Tools/Chapter 1/Migrate Player Model")]
        public static void MigrateFromMenu()
        {
            try
            {
                RunMigration();
                EditorUtility.DisplayDialog(
                    "Migrate Player Model",
                    "Player migration completed successfully.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Chapter1 Player Migration] Migration failed.\n{exception}");
                EditorUtility.DisplayDialog(
                    "Migrate Player Model",
                    $"Migration failed. Check the Console for details.\n\n{exception.Message}",
                    "OK");
            }
        }

        public static void MigrateForAutomation()
        {
            RunMigration();
        }

        private static bool NeedsMigration()
        {
            string currentPlayerPrefabPath = AssetDatabase.GUIDToAssetPath(PlayerPrefabGuid);
            if (!string.Equals(currentPlayerPrefabPath, PlayerPrefabPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null || !string.Equals(playerPrefab.name, "Player", StringComparison.Ordinal))
            {
                return true;
            }

            Transform modelAnchor = playerPrefab.transform.Find("Visual/ModelAnchor");
            PlayerVisualController visualController = playerPrefab.GetComponent<PlayerVisualController>();
            GameObject modelInstance = modelAnchor != null ? FindModelInstance(modelAnchor) : null;
            RuntimeAnimatorController expectedController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            Avatar expectedAvatar = LoadModelAvatar();
            Animator animator = playerPrefab.GetComponent<Animator>();
            PlayerCombatController combatController = playerPrefab.GetComponent<PlayerCombatController>();
            if (modelAnchor == null
                || visualController == null
                || modelInstance == null
                || modelAnchor.childCount != 1
                || modelInstance.GetComponentInChildren<Animation>(true) != null
                || modelInstance.GetComponentInChildren<Animator>(true) != null
                || Quaternion.Angle(modelInstance.transform.localRotation, Quaternion.identity) > 0.01f
                || Vector3.Distance(modelInstance.transform.localPosition, Vector3.zero) > 0.0001f
                || Vector3.Distance(
                    modelInstance.transform.localScale,
                    Vector3.one * ModelScale) > 0.0001f
                || animator == null
                || !animator.enabled
                || animator.applyRootMotion
                || animator.avatar != expectedAvatar
                || animator.runtimeAnimatorController != expectedController
                || combatController == null)
            {
                return true;
            }

            SerializedObject serializedController = new SerializedObject(visualController);
            if (!HasObjectReference(serializedController, "visualRoot")
                || !HasObjectReferenceTo(serializedController, "animatedModelRoot", modelInstance.transform)
                || !HasNullObjectReference(serializedController, "legacyAnimation")
                || !HasNullObjectReference(serializedController, "walkClip")
                || !HasNullObjectReference(serializedController, "runClip")
                || !HasBooleanValue(serializedController, "useLegacyLocomotion", false))
            {
                return true;
            }

            SerializedObject serializedCombat = new SerializedObject(combatController);
            if (!HasObjectReferenceTo(serializedCombat, "animator", animator)
                || !HasNullObjectReference(serializedCombat, "legacyAnimationToPause")
                || !HasBooleanValue(serializedCombat, "enableAnimatorOnlyDuringAttack", false)
                || !HasBooleanValue(serializedCombat, "enableAnimatorWhileCrouching", true)
                || !HasBooleanValue(serializedCombat, "enableAnimatorWhileIdle", true)
                || !HasBooleanValue(serializedCombat, "enableAnimatorWhileMoving", true)
                || !HasBooleanValue(serializedCombat, "enableAnimatorWhileJumping", true)
                || !HasBooleanValue(serializedCombat, "suspendLegacyAnimationDuringAttack", false))
            {
                return true;
            }

            return SerializedSceneContains(DormitoryScenePath, "Player_Minh")
                || SerializedSceneContains(PrototypeScenePath, "Player_Minh");
        }

        private static void RunMigration()
        {
            EnsureTargetScenesAreSafeToSave();
            MovePlayerPrefabWithoutChangingGuid();
            ConfigurePlayerPrefab();
            MigrateScene(DormitoryScenePath, removeStandaloneModel: true);
            MigrateScene(PrototypeScenePath, removeStandaloneModel: false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (NeedsMigration())
            {
                throw new InvalidOperationException("Serialized assets still contain an incomplete Player migration.");
            }

            Debug.Log("[Chapter1 Player Migration] Player migration completed successfully.");
        }

        private static void MovePlayerPrefabWithoutChangingGuid()
        {
            string currentPath = AssetDatabase.GUIDToAssetPath(PlayerPrefabGuid);
            if (string.Equals(currentPath, PlayerPrefabPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(currentPath))
            {
                throw new InvalidOperationException($"Cannot find the gameplay player prefab with GUID '{PlayerPrefabGuid}'.");
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(PlayerPrefabPath) != null)
            {
                throw new InvalidOperationException($"Cannot move the player prefab because '{PlayerPrefabPath}' already exists.");
            }

            string error = AssetDatabase.MoveAsset(currentPath, PlayerPrefabPath);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException($"Failed to move player prefab from '{currentPath}' to '{PlayerPrefabPath}': {error}");
            }
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                prefabRoot.name = "Player";
                TryAssignPlayerTag(prefabRoot);
                ConfigurePlayerVisual(prefabRoot);
                SetLayerRecursively(prefabRoot, LayerMask.NameToLayer("Player"));

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException($"Failed to save '{PlayerPrefabPath}'.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.ImportAsset(
                PlayerPrefabPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigurePlayerVisual(GameObject player)
        {
            PlayerVisualController visualController = player.GetComponent<PlayerVisualController>();
            if (visualController == null)
            {
                throw new InvalidOperationException($"Player '{player.name}' is missing {nameof(PlayerVisualController)}.");
            }

            Transform visual = FindOrCreateChild(player.transform, "Visual");
            DestroyDirectChildIfPresent(visual, "Body");
            DestroyDirectChildIfPresent(visual, "Head");

            Transform modelAnchor = FindOrCreateChild(visual, "ModelAnchor");
            modelAnchor.localPosition = Vector3.zero;
            modelAnchor.localRotation = Quaternion.identity;
            modelAnchor.localScale = Vector3.one;

            GameObject modelInstance = FindModelInstance(modelAnchor);
            if (modelInstance == null)
            {
                DestroyAllChildren(modelAnchor);

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (model == null)
                {
                    throw new InvalidOperationException($"Missing player model: '{ModelPath}'.");
                }

                modelInstance = PrefabUtility.InstantiatePrefab(model, modelAnchor) as GameObject;
                if (modelInstance == null)
                {
                    throw new InvalidOperationException($"Failed to instantiate player model: '{ModelPath}'.");
                }
            }

            DestroyOtherChildren(modelAnchor, modelInstance.transform);

            modelInstance.name = "Nam";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one * ModelScale;
            SetLayerRecursively(modelInstance, LayerMask.NameToLayer("Player"));
            RemoveNestedAnimators(modelInstance);

            Avatar avatar = LoadModelAvatar();
            RuntimeAnimatorController animatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (avatar == null)
            {
                throw new InvalidOperationException($"Model '{ModelPath}' does not contain a valid Humanoid Avatar.");
            }

            if (animatorController == null)
            {
                throw new InvalidOperationException($"Missing Animator Controller: '{AnimatorControllerPath}'.");
            }

            Animator animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.keepAnimatorStateOnDisable = true;
            animator.enabled = true;

            SerializedObject serializedController = new SerializedObject(visualController);
            AssignObjectReference(serializedController, "visualRoot", visual);
            AssignBoolean(serializedController, "useLegacyLocomotion", false);
            AssignObjectReference(serializedController, "legacyAnimation", null);
            AssignObjectReference(serializedController, "animatedModelRoot", modelInstance.transform);
            AssignObjectReference(serializedController, "walkClip", null);
            AssignObjectReference(serializedController, "runClip", null);
            AssignFloat(serializedController, "crouchingYOffset", 0f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            PlayerCombatController combatController = player.GetComponent<PlayerCombatController>();
            if (combatController == null)
            {
                throw new InvalidOperationException(
                    $"Player '{player.name}' is missing {nameof(PlayerCombatController)}; the Animator cannot receive locomotion parameters.");
            }

            SerializedObject serializedCombat = new SerializedObject(combatController);
            AssignObjectReference(serializedCombat, "legacyAnimationToPause", null);
            AssignObjectReference(serializedCombat, "animator", animator);
            AssignObjectReference(serializedCombat, "proceduralAnimationRoot", modelAnchor);
            AssignBoolean(serializedCombat, "enableAnimatorOnlyDuringAttack", false);
            AssignBoolean(serializedCombat, "enableAnimatorWhileCrouching", true);
            AssignBoolean(serializedCombat, "enableAnimatorWhileIdle", true);
            AssignBoolean(serializedCombat, "enableAnimatorWhileMoving", true);
            AssignBoolean(serializedCombat, "enableAnimatorWhileJumping", true);
            AssignBoolean(serializedCombat, "suspendLegacyAnimationDuringAttack", false);
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MigrateScene(string scenePath, bool removeStandaloneModel)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException($"Missing scene: '{scenePath}'.");
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForMigration = !scene.IsValid() || !scene.isLoaded;
            if (openedForMigration)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                Chapter1PlayerMotor[] motors = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Chapter1PlayerMotor>(true))
                    .ToArray();
                if (motors.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scenePath}' must contain exactly one {nameof(Chapter1PlayerMotor)}; found {motors.Length}.");
                }

                GameObject player = motors[0].gameObject;
                player.name = "Player";
                player.transform.localScale = Vector3.one;
                TryAssignPlayerTag(player);

                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(player);
                string prefabSourcePath = prefabSource != null ? AssetDatabase.GetAssetPath(prefabSource) : string.Empty;
                bool usesPlayerPrefab = string.Equals(prefabSourcePath, PlayerPrefabPath, StringComparison.OrdinalIgnoreCase);

                if (usesPlayerPrefab)
                {
                    player.layer = LayerMask.NameToLayer("Player");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(player);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                    PrefabUtility.RemoveUnusedOverrides(new[] { player }, InteractionMode.AutomatedAction);
                }
                else
                {
                    ConfigurePlayerVisual(player);
                    SetLayerRecursively(player, LayerMask.NameToLayer("Player"));
                }

                if (removeStandaloneModel)
                {
                    RemoveStandaloneModelDuplicate(scene, player);
                    if (FindStandaloneModelDuplicates(scene, player).Length > 0)
                    {
                        throw new InvalidOperationException(
                            $"Scene '{scenePath}' still contains a standalone duplicate of the player model.");
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException($"Failed to save migrated scene '{scenePath}'.");
                }
            }
            finally
            {
                if (openedForMigration && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
        }

        private static void RemoveStandaloneModelDuplicate(Scene scene, GameObject gameplayPlayer)
        {
            foreach (GameObject duplicate in FindStandaloneModelDuplicates(scene, gameplayPlayer))
            {
                Object.DestroyImmediate(duplicate);
            }
        }

        private static GameObject[] FindStandaloneModelDuplicates(Scene scene, GameObject gameplayPlayer)
        {
            return scene.GetRootGameObjects()
                .Where(root =>
                {
                    if (root == gameplayPlayer
                        || root.GetComponent<Chapter1PlayerMotor>() != null)
                    {
                        return false;
                    }

                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                    string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                    return string.Equals(sourcePath, ModelPath, StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();
        }

        private static GameObject FindModelInstance(Transform modelAnchor)
        {
            for (int i = 0; i < modelAnchor.childCount; i++)
            {
                GameObject child = modelAnchor.GetChild(i).gameObject;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (string.Equals(sourcePath, ModelPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(child.name, "Nam", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static Avatar LoadModelAvatar()
        {
            return AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Avatar>()
                .FirstOrDefault(candidate => candidate != null && candidate.isValid && candidate.isHuman);
        }

        private static void RemoveNestedAnimators(GameObject modelInstance)
        {
            foreach (Animator nestedAnimator in modelInstance.GetComponentsInChildren<Animator>(true))
            {
                Object.DestroyImmediate(nestedAnimator, true);
            }
        }

        private static void DestroyAllChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(parent.GetChild(index).gameObject);
            }
        }

        private static void DestroyOtherChildren(Transform parent, Transform modelInstance)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child != modelInstance)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static void DestroyDirectChildIfPresent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null && child.parent == parent)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void AssignObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized field '{propertyName}' was not found on '{serializedObject.targetObject.GetType().Name}'.");
            }

            property.objectReferenceValue = value;
        }

        private static void AssignFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized field '{propertyName}' was not found on '{serializedObject.targetObject.GetType().Name}'.");
            }

            property.floatValue = value;
        }

        private static void AssignBoolean(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized field '{propertyName}' was not found on '{serializedObject.targetObject.GetType().Name}'.");
            }

            property.boolValue = value;
        }

        private static bool HasObjectReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool HasObjectReferenceTo(
            SerializedObject serializedObject,
            string propertyName,
            Object expectedValue)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.objectReferenceValue == expectedValue;
        }

        private static bool HasNullObjectReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.objectReferenceValue == null;
        }

        private static bool HasBooleanValue(
            SerializedObject serializedObject,
            string propertyName,
            bool expectedValue)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.boolValue == expectedValue;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (layer < 0)
            {
                throw new InvalidOperationException("Required layer 'Player' does not exist.");
            }

            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
                if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                }
            }
        }

        private static void TryAssignPlayerTag(GameObject player)
        {
            try
            {
                player.tag = "Player";
            }
            catch (UnityException exception)
            {
                throw new InvalidOperationException("Required tag 'Player' does not exist.", exception);
            }
        }

        private static bool SerializedSceneContains(string scenePath, string value)
        {
            string physicalPath = Path.GetFullPath(scenePath);
            return File.Exists(physicalPath)
                && File.ReadAllText(physicalPath).Contains(value, StringComparison.Ordinal);
        }

        private static void EnsureTargetScenesAreSafeToSave()
        {
            foreach (string scenePath in new[] { DormitoryScenePath, PrototypeScenePath })
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scenePath}' has unsaved changes. Save it before running the Player migration.");
                }
            }
        }
    }
}
