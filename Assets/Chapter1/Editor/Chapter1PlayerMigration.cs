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
        private const string ModelPrefabPath = "Assets/Prefab/main_character/Man relax.prefab";
        private const string ModelFbxPath = "Assets/project upload edit/character man relax/Man relax.FBX";
        private const string DormitoryScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string PrototypeScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const float ModelScale = 1.8485f;
        private const float ModelYaw = 0f;

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
            Animation legacyAnimation = modelInstance != null
                ? modelInstance.GetComponentInChildren<Animation>(true)
                : null;
            if (modelAnchor == null
                || visualController == null
                || modelInstance == null
                || legacyAnimation == null
                || legacyAnimation.playAutomatically
                || Quaternion.Angle(
                    modelInstance.transform.localRotation,
                    Quaternion.Euler(0f, ModelYaw, 0f)) > 0.01f
                || Vector3.Distance(
                    modelInstance.transform.localScale,
                    Vector3.one * ModelScale) > 0.0001f)
            {
                return true;
            }

            SerializedObject serializedController = new SerializedObject(visualController);
            if (!HasObjectReference(serializedController, "visualRoot")
                || !HasObjectReference(serializedController, "legacyAnimation")
                || !HasObjectReference(serializedController, "animatedModelRoot")
                || !HasObjectReference(serializedController, "walkClip")
                || !HasObjectReference(serializedController, "runClip"))
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
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefabPath);
                if (modelPrefab == null)
                {
                    throw new InvalidOperationException($"Missing player model prefab: '{ModelPrefabPath}'.");
                }

                modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, modelAnchor) as GameObject;
                if (modelInstance == null)
                {
                    throw new InvalidOperationException($"Failed to instantiate player model prefab: '{ModelPrefabPath}'.");
                }
            }

            modelInstance.name = "Man relax";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
            modelInstance.transform.localScale = Vector3.one * ModelScale;
            AlignModelToAnchor(modelInstance, modelAnchor);
            SetLayerRecursively(modelInstance, LayerMask.NameToLayer("Player"));

            Animation legacyAnimation = modelInstance.GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null)
            {
                throw new InvalidOperationException($"Player model '{ModelPrefabPath}' is missing its Legacy Animation component.");
            }

            legacyAnimation.playAutomatically = false;
            legacyAnimation.Stop();
            if (PrefabUtility.IsPartOfPrefabInstance(legacyAnimation))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(legacyAnimation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(modelInstance.transform);
            }

            AnimationClip walkClip = LoadAnimationClip("walk 1");
            AnimationClip runClip = LoadAnimationClip("run");

            SerializedObject serializedController = new SerializedObject(visualController);
            AssignObjectReference(serializedController, "visualRoot", visual);
            AssignObjectReference(serializedController, "legacyAnimation", legacyAnimation);
            AssignObjectReference(serializedController, "animatedModelRoot", modelInstance.transform);
            AssignObjectReference(serializedController, "walkClip", walkClip);
            AssignObjectReference(serializedController, "runClip", runClip);
            AssignFloat(serializedController, "crouchingYOffset", 0f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
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
                        || !string.Equals(root.name, "Player", StringComparison.Ordinal)
                        || root.GetComponent<Chapter1PlayerMotor>() != null)
                    {
                        return false;
                    }

                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                    string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                    return string.Equals(sourcePath, ModelPrefabPath, StringComparison.OrdinalIgnoreCase);
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
                if (string.Equals(sourcePath, ModelPrefabPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(child.name, "Man relax", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static AnimationClip LoadAnimationClip(string clipName)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ModelFbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate =>
                    !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(candidate.name.Trim(), clipName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (clip == null)
            {
                throw new InvalidOperationException($"Cannot find animation clip '{clipName}' in '{ModelFbxPath}'.");
            }

            return clip;
        }

        private static void AlignModelToAnchor(GameObject modelInstance, Transform modelAnchor)
        {
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            if (!TryCalculateBoundsRelativeTo(modelAnchor, renderers, out Bounds bounds))
            {
                throw new InvalidOperationException($"Cannot calculate renderer bounds for '{modelInstance.name}'.");
            }

            modelInstance.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private static bool TryCalculateBoundsRelativeTo(Transform relativeTo, Renderer[] renderers, out Bounds combinedBounds)
        {
            combinedBounds = default;
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 worldCorner = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner = relativeTo.InverseTransformPoint(worldCorner);
                            if (!hasBounds)
                            {
                                combinedBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
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

        private static bool HasObjectReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.objectReferenceValue != null;
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
