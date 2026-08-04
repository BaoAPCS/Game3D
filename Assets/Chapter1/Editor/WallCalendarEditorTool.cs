using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class WallCalendarEditorTool
    {
        private const string CalendarTexturePath = "Assets/Chapter1/Textures/Props/Calendar_March25.png";
        private const string CalendarPrefabPath = "Assets/Chapter1/Prefabs/WallCalendar.prefab";
        private const string MaterialFolderPath = "Assets/Chapter1/Materials/Props";
        private const string BackboardMaterialPath = MaterialFolderPath + "/WallCalendar_Backboard.mat";
        private const string BindingMaterialPath = MaterialFolderPath + "/WallCalendar_Binding.mat";
        private const string PinMaterialPath = MaterialFolderPath + "/WallCalendar_Pin.mat";
        private const string PrefabRootName = "WallCalendar";
        private const string SceneObjectName = "WallCalendar_March25";

        private const float BackboardWidth = 0.34f;
        private const float BackboardHeight = 0.40f;
        private const float BackboardDepth = 0.02f;
        private const float WallGap = 0.01f;
        private const float ImageForwardOffset = 0.003f;

        [MenuItem("Tools/Chapter 1/Place March 25 Calendar On Selected Wall")]
        public static void PlaceMarch25CalendarOnSelectedWall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                string message = "Please exit Play Mode before placing the March 25 calendar. Scene edits made during Play Mode are not permanent.";
                Debug.LogWarning("[WallCalendarEditorTool] " + message);
                EditorUtility.DisplayDialog("Place March 25 Calendar", message, "OK");
                return;
            }

            GameObject selectedWall = Selection.activeGameObject;
            if (selectedWall == null)
            {
                EditorUtility.DisplayDialog(
                    "Place March 25 Calendar",
                    "Please select a wall object in the Hierarchy before placing the March 25 calendar.",
                    "OK");
                return;
            }

            if (EditorUtility.IsPersistent(selectedWall) || !selectedWall.scene.IsValid())
            {
                string message = $"Selected object '{selectedWall.name}' is not a Scene object. Please select a wall object in the Hierarchy.";
                Debug.LogWarning("[WallCalendarEditorTool] " + message, selectedWall);
                EditorUtility.DisplayDialog("Place March 25 Calendar", message, "OK");
                return;
            }

            Renderer wallRenderer = selectedWall.GetComponentInChildren<Renderer>();
            Collider wallCollider = selectedWall.GetComponentInChildren<Collider>();
            if (wallRenderer == null || wallCollider == null)
            {
                string message =
                    $"Selected object '{selectedWall.name}' must have both a Renderer and a Collider so the tool can infer the wall face. " +
                    $"Renderer: {(wallRenderer != null ? "found" : "missing")}, Collider: {(wallCollider != null ? "found" : "missing")}.";
                Debug.LogWarning("[WallCalendarEditorTool] " + message, selectedWall);
                EditorUtility.DisplayDialog("Place March 25 Calendar", message, "OK");
                return;
            }

            Sprite calendarSprite = ConfigureCalendarTexture();
            if (calendarSprite == null)
            {
                string message = $"Could not load calendar Sprite at '{CalendarTexturePath}'.";
                Debug.LogWarning("[WallCalendarEditorTool] " + message);
                EditorUtility.DisplayDialog("Place March 25 Calendar", message, "OK");
                return;
            }

            CalendarMaterials materials = EnsureMaterials();
            bool prefabExisted = AssetDatabase.LoadAssetAtPath<GameObject>(CalendarPrefabPath) != null;
            SaveOrUpdatePrefab(calendarSprite, materials);

            Placement placement = CalculatePlacement(selectedWall, wallRenderer, wallCollider);
            GameObject calendar = FindExistingCalendar(selectedWall.transform);
            bool updatedExistingSceneObject = calendar != null;
            if (calendar == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CalendarPrefabPath);
                calendar = PrefabUtility.InstantiatePrefab(prefab, selectedWall.scene) as GameObject;
                if (calendar == null)
                {
                    calendar = new GameObject(PrefabRootName);
                    BuildCalendarStructure(calendar, calendarSprite, materials);
                }

                Undo.RegisterCreatedObjectUndo(calendar, "Place March 25 Calendar");
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(calendar, "Update March 25 Calendar");
                BuildCalendarStructure(calendar, calendarSprite, materials);
            }

            calendar.name = SceneObjectName;
            calendar.transform.SetParent(null, true);
            calendar.transform.localScale = Vector3.one;
            calendar.transform.SetPositionAndRotation(placement.WorldPosition, placement.WorldRotation);
            calendar.transform.SetParent(selectedWall.transform, true);

            Selection.activeGameObject = calendar;
            EditorGUIUtility.PingObject(calendar);

            MarkSceneDirty(selectedWall.scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[WallCalendarEditorTool] March 25 calendar placed.\n" +
                $"Texture: {CalendarTexturePath}\n" +
                $"Selected wall: {selectedWall.name}\n" +
                $"World Position: {FormatVector(calendar.transform.position)}\n" +
                $"World Rotation: {FormatVector(calendar.transform.eulerAngles)}\n" +
                $"Prefab: {(prefabExisted ? "updated" : "created")} at {CalendarPrefabPath}\n" +
                $"Scene object: {(updatedExistingSceneObject ? "updated existing" : "created new")} {SceneObjectName}",
                calendar);
        }

        private static Sprite ConfigureCalendarTexture()
        {
            TextureImporter importer = AssetImporter.GetAtPath(CalendarTexturePath) as TextureImporter;
            if (importer == null)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(CalendarTexturePath);
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CalendarTexturePath);
        }

        private static CalendarMaterials EnsureMaterials()
        {
            EnsureFolder("Assets/Chapter1/Materials");
            EnsureFolder(MaterialFolderPath);

            return new CalendarMaterials
            {
                Backboard = EnsureMaterial(BackboardMaterialPath, new Color(0.86f, 0.80f, 0.68f, 1f)),
                Binding = EnsureMaterial(BindingMaterialPath, new Color(0.20f, 0.14f, 0.09f, 1f)),
                Pin = EnsureMaterial(PinMaterialPath, new Color(0.10f, 0.09f, 0.08f, 1f))
            };
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindUrpCompatibleShader());
                AssetDatabase.CreateAsset(material, path);
            }

            SetMaterialColor(material, color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindUrpCompatibleShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Unlit");
            return shader != null ? shader : Shader.Find("Standard");
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SaveOrUpdatePrefab(Sprite calendarSprite, CalendarMaterials materials)
        {
            EnsureFolder("Assets/Chapter1/Prefabs");
            GameObject prefabRoot = new GameObject(PrefabRootName);
            try
            {
                BuildCalendarStructure(prefabRoot, calendarSprite, materials);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CalendarPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(prefabRoot);
            }
        }

        private static void BuildCalendarStructure(GameObject root, Sprite calendarSprite, CalendarMaterials materials)
        {
            RemoveExistingStructure(root);
            root.name = PrefabRootName;

            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "Calendar_Backboard";
            backboard.transform.SetParent(root.transform, false);
            backboard.transform.localPosition = Vector3.zero;
            backboard.transform.localRotation = Quaternion.identity;
            backboard.transform.localScale = new Vector3(BackboardWidth, BackboardHeight, BackboardDepth);
            SetRendererMaterial(backboard, materials.Backboard);
            RemoveCollider(backboard);

            GameObject image = new GameObject("Calendar_Image");
            image.transform.SetParent(root.transform, false);
            image.transform.localPosition = new Vector3(0f, -0.015f, BackboardDepth * 0.5f + ImageForwardOffset);
            image.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            SpriteRenderer spriteRenderer = image.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = calendarSprite;
            spriteRenderer.sortingOrder = 1;
            ScaleSpriteToFit(image.transform, calendarSprite, 0.30f, 0.30f);

            GameObject binding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            binding.name = "Top_Binding";
            binding.transform.SetParent(root.transform, false);
            binding.transform.localPosition = new Vector3(0f, BackboardHeight * 0.5f - 0.018f, BackboardDepth * 0.5f + 0.003f);
            binding.transform.localRotation = Quaternion.identity;
            binding.transform.localScale = new Vector3(0.36f, 0.035f, 0.026f);
            SetRendererMaterial(binding, materials.Binding);
            RemoveCollider(binding);

            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = "Hanging_Pin";
            pin.transform.SetParent(root.transform, false);
            pin.transform.localPosition = new Vector3(0f, BackboardHeight * 0.5f + 0.002f, BackboardDepth * 0.5f + 0.018f);
            pin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pin.transform.localScale = new Vector3(0.035f, 0.009f, 0.035f);
            SetRendererMaterial(pin, materials.Pin);
            RemoveCollider(pin);
        }

        private static void RemoveExistingStructure(GameObject root)
        {
            Component[] components = root.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] != null && !(components[i] is Transform))
                {
                    Object.DestroyImmediate(components[i]);
                }
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }
        }

        private static void SetRendererMaterial(GameObject target, Material material)
        {
            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
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

        private static void ScaleSpriteToFit(Transform imageTransform, Sprite sprite, float maxWidth, float maxHeight)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            {
                imageTransform.localScale = Vector3.one;
                return;
            }

            float scale = Mathf.Min(maxWidth / sprite.bounds.size.x, maxHeight / sprite.bounds.size.y);
            imageTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Placement CalculatePlacement(GameObject wall, Renderer wallRenderer, Collider wallCollider)
        {
            Vector3 viewerPosition = GetViewerPosition(wall.transform);
            Vector3 surfacePoint;
            Vector3 outwardNormal;

            if (!TryCalculateBoxColliderFace(wallCollider, viewerPosition, out surfacePoint, out outwardNormal))
            {
                Bounds bounds = wallCollider.bounds;
                bounds.Encapsulate(wallRenderer.bounds);
                CalculateBoundsFace(bounds, viewerPosition, out surfacePoint, out outwardNormal);
            }

            Vector3 up = ProjectUpOntoWallPlane(wall.transform.up, outwardNormal);
            Vector3 worldPosition = surfacePoint + outwardNormal * (WallGap + BackboardDepth * 0.5f);
            Quaternion worldRotation = Quaternion.LookRotation(outwardNormal, up);
            return new Placement(worldPosition, worldRotation);
        }

        private static bool TryCalculateBoxColliderFace(Collider collider, Vector3 viewerPosition, out Vector3 surfacePoint, out Vector3 outwardNormal)
        {
            surfacePoint = Vector3.zero;
            outwardNormal = Vector3.forward;

            BoxCollider box = collider as BoxCollider;
            if (box == null)
            {
                return false;
            }

            Transform transform = box.transform;
            Vector3 center = transform.TransformPoint(box.center);
            Vector3[] axes =
            {
                transform.TransformDirection(Vector3.right).normalized,
                transform.TransformDirection(Vector3.up).normalized,
                transform.TransformDirection(Vector3.forward).normalized
            };
            float[] halfExtents =
            {
                transform.TransformVector(Vector3.right * (box.size.x * 0.5f)).magnitude,
                transform.TransformVector(Vector3.up * (box.size.y * 0.5f)).magnitude,
                transform.TransformVector(Vector3.forward * (box.size.z * 0.5f)).magnitude
            };

            int normalAxis = FindSmallestPositiveExtent(halfExtents);
            if (normalAxis < 0)
            {
                return false;
            }

            outwardNormal = axes[normalAxis];
            if (Vector3.Dot(viewerPosition - center, outwardNormal) < 0f)
            {
                outwardNormal = -outwardNormal;
            }

            surfacePoint = center + outwardNormal * halfExtents[normalAxis];
            return true;
        }

        private static void CalculateBoundsFace(Bounds bounds, Vector3 viewerPosition, out Vector3 surfacePoint, out Vector3 outwardNormal)
        {
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] halfExtents = { bounds.extents.x, bounds.extents.y, bounds.extents.z };
            int normalAxis = FindSmallestPositiveExtent(halfExtents);
            if (normalAxis < 0)
            {
                normalAxis = 2;
            }

            outwardNormal = axes[normalAxis];
            if (Vector3.Dot(viewerPosition - bounds.center, outwardNormal) < 0f)
            {
                outwardNormal = -outwardNormal;
            }

            surfacePoint = bounds.center + outwardNormal * halfExtents[normalAxis];
        }

        private static int FindSmallestPositiveExtent(float[] halfExtents)
        {
            int best = -1;
            float bestValue = float.PositiveInfinity;
            for (int i = 0; i < halfExtents.Length; i++)
            {
                if (halfExtents[i] > 0.0001f && halfExtents[i] < bestValue)
                {
                    best = i;
                    bestValue = halfExtents[i];
                }
            }

            return best;
        }

        private static Vector3 ProjectUpOntoWallPlane(Vector3 preferredUp, Vector3 normal)
        {
            Vector3 up = Vector3.ProjectOnPlane(preferredUp, normal);
            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.ProjectOnPlane(Vector3.up, normal);
            }

            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }

            return up.normalized;
        }

        private static Vector3 GetViewerPosition(Transform wallTransform)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                return sceneView.camera.transform.position;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera.transform.position;
            }

            return wallTransform.position + wallTransform.forward;
        }

        private static GameObject FindExistingCalendar(Transform wall)
        {
            Transform[] children = wall.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != wall && children[i].name == SceneObjectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            int slash = assetFolderPath.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            string parent = assetFolderPath.Substring(0, slash);
            string folderName = assetFolderPath.Substring(slash + 1);
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetFolderPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void MarkSceneDirty(Scene scene)
        {
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
        }

        private readonly struct Placement
        {
            public Placement(Vector3 worldPosition, Quaternion worldRotation)
            {
                WorldPosition = worldPosition;
                WorldRotation = worldRotation;
            }

            public Vector3 WorldPosition { get; }
            public Quaternion WorldRotation { get; }
        }

        private sealed class CalendarMaterials
        {
            public Material Backboard;
            public Material Binding;
            public Material Pin;
        }
    }
}
