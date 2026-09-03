using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter2.Editor
{
    /// <summary>
    /// Copies the playable environment from the ScifiOfficeLite demo into the
    /// Chapter 2 police station without bringing along the demo player or UI.
    /// </summary>
    [InitializeOnLoad]
    public static class ScifiOfficeSceneBuilder
    {
        private const string TargetScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";
        private const string SourceScenePath =
            "Assets/ScifiOfficeLite/Scene/Demo.unity";
        private const string GeneratedMaterialFolder =
            "Assets/Chapter2/Generated/ScifiOffice/Materials";
        private const string BuildRequestPath =
            "Temp/BuildScifiOfficeMap.request";

        private const string RootName = "ScifiOffice";
        private const string OfficeSourceName = "Office 1";
        private const string CeilingSourceName = "Ceiling";
        private const string BrokenDoorRelativePath =
            "Office 1/Door Wall Opaque/Broken Door";

        // The demo's 20 m floor is centered here. Offsetting both copied roots
        // by this value gives ScifiOffice a useful pivot in the map's center.
        private static readonly Vector3 SourceMapCenter =
            new Vector3(2.6383f, 0f, 39.327f);

        // JailObstacle is near (2.09, 0, 11.95) and its exit faces +Z. This
        // places the new office directly beyond it without covering mission 1.
        private static readonly Vector3 TargetPlacement =
            new Vector3(2.09f, 0f, 25f);

        private static bool waitingForEditor;

        static ScifiOfficeSceneBuilder()
        {
            EditorApplication.delayCall += ProcessOneShotBuildRequest;
        }

        [MenuItem("Tools/Chapter 2/Build ScifiOffice Map")]
        public static void BuildFromMenu()
        {
            BuildOrRefreshMap(selectBuiltRoot: true);
        }

        [MenuItem("Tools/Chapter 2/Select ScifiOffice Map")]
        public static void SelectMap()
        {
            Scene targetScene = FindLoadedScene(TargetScenePath);
            GameObject root = targetScene.IsValid()
                ? FindRoot(targetScene, RootName)
                : null;

            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "ScifiOffice",
                    "ScifiOffice is not loaded in Police_Station. Run Build ScifiOffice Map first.",
                    "OK");
                return;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        [MenuItem("Tools/Chapter 2/Validate ScifiOffice Map")]
        public static void ValidateFromMenu()
        {
            Scene targetScene = FindLoadedScene(TargetScenePath);
            bool openedTarget = false;
            if (!targetScene.IsValid())
            {
                targetScene = EditorSceneManager.OpenScene(
                    TargetScenePath,
                    OpenSceneMode.Additive);
                openedTarget = true;
            }

            try
            {
                GameObject root = FindRoot(targetScene, RootName);
                ValidateBuiltMap(root, logSuccess: true);
            }
            finally
            {
                if (openedTarget && targetScene.IsValid())
                {
                    EditorSceneManager.CloseScene(targetScene, removeScene: true);
                }
            }
        }

        /// <summary>
        /// Public entry point for Unity -executeMethod and automated tooling.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            BuildOrRefreshMap(selectBuiltRoot: false);
        }

        private static void ProcessOneShotBuildRequest()
        {
            if (!File.Exists(BuildRequestPath))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!waitingForEditor)
                {
                    waitingForEditor = true;
                    EditorApplication.update += WaitForEditorThenBuild;
                }

                return;
            }

            RunOneShotBuild();
        }

        private static void WaitForEditorThenBuild()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= WaitForEditorThenBuild;
            waitingForEditor = false;
            RunOneShotBuild();
        }

        private static void RunOneShotBuild()
        {
            if (!File.Exists(BuildRequestPath))
            {
                return;
            }

            // Consume first so a build exception cannot create a reload loop.
            File.Delete(BuildRequestPath);

            try
            {
                BuildOrRefreshMap(selectBuiltRoot: true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError(
                    "[ScifiOffice Builder] Automatic build failed. " +
                    "Use Tools/Chapter 2/Build ScifiOffice Map to retry.");
            }
        }

        private static void BuildOrRefreshMap(bool selectBuiltRoot)
        {
            Scene targetScene = FindLoadedScene(TargetScenePath);
            if (!targetScene.IsValid())
            {
                targetScene = EditorSceneManager.OpenScene(
                    TargetScenePath,
                    OpenSceneMode.Additive);
            }

            GameObject existingRoot = FindRoot(targetScene, RootName);
            if (existingRoot != null)
            {
                int upgradedCount = PrepareCopiedEnvironment(existingRoot);
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not save {TargetScenePath}.");
                }

                AssetDatabase.SaveAssets();
                ValidateBuiltMap(existingRoot, logSuccess: true);
                FinishSelection(targetScene, existingRoot, selectBuiltRoot);
                Debug.Log(
                    $"[ScifiOffice Builder] Existing ScifiOffice refreshed " +
                    $"({upgradedCount} renderer material slots checked).");
                return;
            }

            Scene sourceScene = FindLoadedScene(SourceScenePath);
            bool openedSource = false;
            if (!sourceScene.IsValid())
            {
                sourceScene = EditorSceneManager.OpenScene(
                    SourceScenePath,
                    OpenSceneMode.Additive);
                openedSource = true;
            }

            GameObject builtRoot = null;
            try
            {
                GameObject sourceOffice = RequireRoot(
                    sourceScene,
                    OfficeSourceName);
                GameObject sourceCeiling = RequireRoot(
                    sourceScene,
                    CeilingSourceName);

                builtRoot = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(builtRoot, targetScene);
                builtRoot.transform.SetPositionAndRotation(
                    TargetPlacement,
                    Quaternion.identity);
                builtRoot.transform.localScale = Vector3.one;

                CopySourceRoot(sourceOffice, builtRoot.transform, targetScene);
                CopySourceRoot(sourceCeiling, builtRoot.transform, targetScene);

                int upgradedCount = PrepareCopiedEnvironment(builtRoot);
                ValidateBuiltMap(builtRoot, logSuccess: false);

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not save {TargetScenePath}.");
                }

                AssetDatabase.SaveAssets();
                FinishSelection(targetScene, builtRoot, selectBuiltRoot);

                int rendererCount =
                    builtRoot.GetComponentsInChildren<Renderer>(true).Length;
                int colliderCount =
                    builtRoot.GetComponentsInChildren<Collider>(true).Length;
                int lightCount =
                    builtRoot.GetComponentsInChildren<Light>(true).Length;

                Debug.Log(
                    $"[ScifiOffice Builder] Built '{RootName}' directly in " +
                    $"{TargetScenePath}. Renderers={rendererCount}, " +
                    $"Colliders={colliderCount}, Lights={lightCount}, " +
                    $"URP material slots={upgradedCount}, " +
                    $"Position={builtRoot.transform.position}.",
                    builtRoot);
            }
            catch
            {
                if (builtRoot != null)
                {
                    Object.DestroyImmediate(builtRoot);
                }

                throw;
            }
            finally
            {
                if (openedSource && sourceScene.IsValid())
                {
                    EditorSceneManager.CloseScene(sourceScene, removeScene: true);
                }
            }
        }

        private static GameObject CopySourceRoot(
            GameObject source,
            Transform destination,
            Scene targetScene)
        {
            GameObject clone = Object.Instantiate(source);
            clone.name = source.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            clone.transform.SetParent(destination, worldPositionStays: false);
            clone.transform.localPosition -= SourceMapCenter;
            return clone;
        }

        private static int PrepareCopiedEnvironment(GameObject root)
        {
            Dictionary<Material, Material> convertedMaterials =
                new Dictionary<Material, Material>();
            int upgradedSlots = 0;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // The demo scene has baked lightmap indices that do not belong
                // to Police_Station. Clear those bindings before saving.
                renderer.lightmapIndex = -1;
                renderer.realtimeLightmapIndex = -1;
                renderer.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
                renderer.realtimeLightmapScaleOffset =
                    new Vector4(1f, 1f, 0f, 0f);

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material sourceMaterial = materials[index];
                    Material converted = GetOrCreateUrpMaterial(
                        sourceMaterial,
                        convertedMaterials);
                    if (converted == sourceMaterial)
                    {
                        continue;
                    }

                    materials[index] = converted;
                    upgradedSlots++;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }

                EditorUtility.SetDirty(renderer);
                if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(
                        renderer);
                }
            }

            // The copied ceiling lights were authored as Mixed for a different
            // baked scene. Realtime keeps the imported room usable immediately.
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                light.lightmapBakeType = LightmapBakeType.Realtime;
                light.shadows = LightShadows.None;
                EditorUtility.SetDirty(light);
                if (PrefabUtility.IsPartOfPrefabInstance(light))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                }
            }

            // The demo's Broken Door is the only environmental rigidbody. Keep
            // the assembled room stable until gameplay deliberately drives it.
            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.useGravity = false;
                body.isKinematic = true;
                body.constraints = RigidbodyConstraints.FreezeAll;
                EditorUtility.SetDirty(body);
                if (PrefabUtility.IsPartOfPrefabInstance(body))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(body);
                }
            }

            PrepareBrokenDoorAnimationObjects(root);

            return upgradedSlots;
        }

        private static void PrepareBrokenDoorAnimationObjects(
            GameObject root)
        {
            Transform brokenDoor =
                root.transform.Find(BrokenDoorRelativePath);
            if (brokenDoor == null)
            {
                return;
            }

            SetDynamic(brokenDoor);
            SetDynamic(brokenDoor.Find("Door1"));
            SetDynamic(brokenDoor.Find("Door1/Top"));
            SetDynamic(brokenDoor.Find("Door2"));
            SetDynamic(brokenDoor.Find("Door2/Top 2"));
        }

        private static void SetDynamic(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.gameObject.isStatic = false;
            EditorUtility.SetDirty(target.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(
                    target.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    target.gameObject);
            }
        }

        private static Material GetOrCreateUrpMaterial(
            Material source,
            IDictionary<Material, Material> cache)
        {
            if (source == null || source.shader == null)
            {
                return source;
            }

            if (cache.TryGetValue(source, out Material cached))
            {
                return cached;
            }

            string shaderName = source.shader.name;
            if (!shaderName.Equals("Standard", StringComparison.Ordinal) &&
                !shaderName.Equals(
                    "Standard (Specular setup)",
                    StringComparison.Ordinal))
            {
                cache[source] = source;
                return source;
            }

            EnsureAssetFolder(GeneratedMaterialFolder);

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string suffix = string.IsNullOrEmpty(sourceGuid)
                ? "embedded"
                : sourceGuid.Substring(0, 8);
            string fileName =
                $"{MakeSafeFileName(source.name)}_{suffix}_URP.mat";
            string outputPath = $"{GeneratedMaterialFolder}/{fileName}";

            Material upgraded = new Material(source)
            {
                name = source.name + " URP"
            };

            StandardUpgrader upgrader = new StandardUpgrader(shaderName);
            upgrader.Upgrade(upgraded, MaterialUpgrader.UpgradeFlags.None);

            Material asset = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(upgraded, outputPath);
                asset = upgraded;
            }
            else
            {
                EditorUtility.CopySerialized(upgraded, asset);
                Object.DestroyImmediate(upgraded);
                EditorUtility.SetDirty(asset);
            }

            cache[source] = asset;
            return asset;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "Material" : value;
        }

        private static void ValidateBuiltMap(
            GameObject root,
            bool logSuccess)
        {
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"'{RootName}' was not found in {TargetScenePath}.");
            }

            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(
                    $"'{RootName}' must be a scene root GameObject.");
            }

            Transform office = root.transform.Find(OfficeSourceName);
            Transform ceiling = root.transform.Find(CeilingSourceName);
            if (office == null || ceiling == null)
            {
                throw new InvalidOperationException(
                    $"'{RootName}' must contain both '{OfficeSourceName}' " +
                    $"and '{CeilingSourceName}'.");
            }

            int rendererCount =
                root.GetComponentsInChildren<Renderer>(true).Length;
            int colliderCount =
                root.GetComponentsInChildren<Collider>(true).Length;
            if (rendererCount == 0 || colliderCount == 0)
            {
                throw new InvalidOperationException(
                    $"'{RootName}' is incomplete: renderers={rendererCount}, " +
                    $"colliders={colliderCount}.");
            }

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null &&
                        material.shader != null &&
                        material.shader.name.StartsWith(
                            "Standard",
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Legacy material '{material.name}' remains on " +
                            $"'{renderer.name}'.");
                    }
                }
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"[ScifiOffice Builder] Validation passed. " +
                    $"Renderers={rendererCount}, Colliders={colliderCount}, " +
                    $"Lights={root.GetComponentsInChildren<Light>(true).Length}.",
                    root);
            }
        }

        private static void FinishSelection(
            Scene targetScene,
            GameObject root,
            bool selectBuiltRoot)
        {
            SceneManager.SetActiveScene(targetScene);
            if (!selectBuiltRoot)
            {
                return;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static Scene FindLoadedScene(string scenePath)
        {
            string normalizedPath = scenePath.Replace('\\', '/');
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.path.Replace('\\', '/').Equals(
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return scene;
                }
            }

            return default;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Root '{name}' was not found in {scene.path}.");
            }

            return root;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.Equals(name, StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }
    }
}
