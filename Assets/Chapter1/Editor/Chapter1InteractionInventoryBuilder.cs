using System;
using System.Collections.Generic;
using System.IO;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1InteractionInventoryBuilder
    {
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string CameraPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/ThirdPersonCameraRig.prefab";
        private const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string GameplayPrefabFolder = "Assets/Chapter1/Prefabs/Gameplay";
        private const string MaterialFolder = "Assets/Chapter1/Materials";
        private const string FlashlightPrefabPath = GameplayPrefabFolder + "/Pickup_Flashlight.prefab";
        private const string FusePrefabPath = GameplayPrefabFolder + "/Pickup_Fuse.prefab";
        private const string CanPrefabPath = GameplayPrefabFolder + "/Pickup_ThrowableCan.prefab";
        private const string TablePrefabPath = GameplayPrefabFolder + "/TestInspectableTable.prefab";
        private const string FlashlightMaterialPath = MaterialFolder + "/M_Pickup_Flashlight.mat";
        private const string FuseMaterialPath = MaterialFolder + "/M_Pickup_Fuse.mat";
        private const string CanMaterialPath = MaterialFolder + "/M_Pickup_Can.mat";
        private const string TableMaterialPath = MaterialFolder + "/M_Test_Table.mat";
        private const string HighlightMaterialPath = MaterialFolder + "/M_Interactable_Highlight.mat";
        private static bool repairScheduledAfterPlayMode;

        private static readonly string[] InputReferenceFields =
        {
            "moveActionReference",
            "lookActionReference",
            "sprintActionReference",
            "crouchActionReference",
            "interactActionReference",
            "toggleFlashlightActionReference",
            "throwCanActionReference",
            "pauseActionReference"
        };

        [MenuItem("Tools/Chapter 1/Add Interaction and Inventory Prototype")]
        public static void AddInteractionAndInventoryPrototype()
        {
            if (!SaveCurrentModifiedScenesOrCancel("Add Interaction and Inventory Prototype"))
            {
                Debug.LogWarning("[Chapter1 Interaction Builder] Canceled because current scene changes were not saved.");
                return;
            }

            EnsureFolders();
            EnsureLayer("Interactable");

            if (!ValidatePlayerInputReferences())
            {
                ShowFailure("Player input references are missing. Run Tools/Chapter 1/Repair Player Input References first.");
                return;
            }

            Material flashlightMaterial = CreateMaterialIfMissing(FlashlightMaterialPath, new Color(0.95f, 0.86f, 0.25f));
            Material fuseMaterial = CreateMaterialIfMissing(FuseMaterialPath, new Color(0.2f, 0.85f, 1f));
            Material canMaterial = CreateMaterialIfMissing(CanMaterialPath, new Color(0.85f, 0.18f, 0.16f));
            Material tableMaterial = CreateMaterialIfMissing(TableMaterialPath, new Color(0.45f, 0.32f, 0.2f));
            Material highlightMaterial = CreateMaterialIfMissing(HighlightMaterialPath, new Color(1f, 0.95f, 0.35f));

            CreatePickupPrefab(FlashlightPrefabPath, "Pickup_Flashlight", Chapter1ItemId.Flashlight, "Nhặt", "Đèn pin", "Đã nhặt đèn pin.", flashlightMaterial, PrimitiveType.Cylinder, new Vector3(0.25f, 0.45f, 0.25f), "prefab.pickup.flashlight");
            CreatePickupPrefab(FusePrefabPath, "Pickup_Fuse", Chapter1ItemId.Fuse, "Nhặt", "cầu chì", "Đã nhặt cầu chì.", fuseMaterial, PrimitiveType.Cube, new Vector3(0.28f, 0.12f, 0.18f), "prefab.pickup.fuse");
            CreatePickupPrefab(CanPrefabPath, "Pickup_ThrowableCan", Chapter1ItemId.ThrowableCan, "Nhặt", "lon nước", "Đã nhặt lon nước.", canMaterial, PrimitiveType.Cylinder, new Vector3(0.22f, 0.38f, 0.22f), "prefab.pickup.throwable_can");
            CreateInspectableTablePrefab(tableMaterial, highlightMaterial);

            if (!UpdatePlayerPrefab())
            {
                ShowFailure("Failed to update Player prefab.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                ShowFailure("Failed to open Chapter1_PlayerPrototype scene.");
                return;
            }

            GameObject sceneRoot = FindSceneObject(scene, "Chapter1_PlayerPrototype") ?? CreateSceneRoot(scene, "Chapter1_PlayerPrototype");
            GameObject player = FindScenePlayer(scene);
            GameObject cameraRig = FindSceneObject(scene, "CameraRig");
            Camera gameplayCamera = cameraRig != null ? cameraRig.GetComponentInChildren<Camera>(true) : Camera.main;
            Chapter1Manager chapterManager = GetFirstSceneComponent<Chapter1Manager>(scene);
            Chapter1GameplayBootstrap bootstrap = GetFirstSceneComponent<Chapter1GameplayBootstrap>(scene);

            if (player == null)
            {
                ShowFailure("Scene is missing Player.");
                return;
            }

            EnsurePlayerRuntimeComponents(player, gameplayCamera, chapterManager);
            CreateInteractionTestGroup(scene, sceneRoot.transform);
            Chapter1HUD hud = CreateHud(sceneRoot.transform, player, gameplayCamera, chapterManager);
            EnsureEventSystem(sceneRoot.transform);
            LinkBootstrap(bootstrap, player, cameraRig, gameplayCamera, chapterManager, hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Add Interaction and Inventory Prototype", "Interaction, inventory, flashlight, HUD, pickups, and test scene objects were added.", "OK");
            Debug.Log("[Chapter1 Interaction Builder] Completed interaction and inventory prototype setup.");
        }

        [MenuItem("Tools/Chapter 1/Repair and Validate Interaction and HUD")]
        public static void RepairAndValidateInteractionAndHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!repairScheduledAfterPlayMode)
                {
                    repairScheduledAfterPlayMode = true;
                    EditorApplication.ExitPlaymode();
                    EditorApplication.update += ContinueRepairAfterPlayMode;
                }

                Debug.LogWarning("[Chapter1 Interaction Builder] Play Mode đang bật, đã yêu cầu Unity thoát Play Mode trước khi repair.");
                return;
            }

            repairScheduledAfterPlayMode = false;
            if (!SaveCurrentModifiedScenesOrCancel("Repair and Validate Interaction and HUD"))
            {
                Debug.LogWarning("[Chapter1 Interaction Builder] Canceled because current scene changes were not saved.");
                return;
            }

            EnsureFolders();
            EnsureLayer("Player");
            EnsureLayer("Environment");
            EnsureLayer("Interactable");

            if (!Chapter1PlayerPrototypeBuilder.RepairPlayerInputReferencesForAutomation())
            {
                ShowFailure("Failed to repair persistent player input references.");
                return;
            }

            Material flashlightMaterial = CreateMaterialIfMissing(FlashlightMaterialPath, new Color(0.95f, 0.86f, 0.25f));
            Material fuseMaterial = CreateMaterialIfMissing(FuseMaterialPath, new Color(0.2f, 0.85f, 1f));
            Material canMaterial = CreateMaterialIfMissing(CanMaterialPath, new Color(0.85f, 0.18f, 0.16f));
            Material tableMaterial = CreateMaterialIfMissing(TableMaterialPath, new Color(0.45f, 0.32f, 0.2f));
            Material highlightMaterial = CreateMaterialIfMissing(HighlightMaterialPath, new Color(1f, 0.95f, 0.35f));

            CreatePickupPrefab(FlashlightPrefabPath, "Pickup_Flashlight", Chapter1ItemId.Flashlight, "Nhặt", "đèn pin", "Đã nhặt đèn pin.", flashlightMaterial, PrimitiveType.Cylinder, new Vector3(0.25f, 0.45f, 0.25f), "prefab.pickup.flashlight");
            CreatePickupPrefab(FusePrefabPath, "Pickup_Fuse", Chapter1ItemId.Fuse, "Nhặt", "cầu chì", "Đã nhặt cầu chì.", fuseMaterial, PrimitiveType.Cube, new Vector3(0.28f, 0.12f, 0.18f), "prefab.pickup.fuse");
            CreatePickupPrefab(CanPrefabPath, "Pickup_ThrowableCan", Chapter1ItemId.ThrowableCan, "Nhặt", "lon nước", "Đã nhặt lon nước.", canMaterial, PrimitiveType.Cylinder, new Vector3(0.22f, 0.38f, 0.22f), "prefab.pickup.throwable_can");
            CreateInspectableTablePrefab(tableMaterial, highlightMaterial);

            if (!UpdatePlayerPrefab())
            {
                ShowFailure("Failed to update Player prefab.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                ShowFailure("Failed to open Chapter1_PlayerPrototype scene.");
                return;
            }

            GameObject sceneRoot = FindSceneObject(scene, "Chapter1_PlayerPrototype") ?? CreateSceneRoot(scene, "Chapter1_PlayerPrototype");
            GameObject player = FindScenePlayer(scene);
            GameObject cameraRig = FindSceneObject(scene, "CameraRig");
            Camera gameplayCamera = cameraRig != null ? cameraRig.GetComponentInChildren<Camera>(true) : Camera.main;
            Chapter1Manager chapterManager = GetFirstSceneComponent<Chapter1Manager>(scene);
            Chapter1GameplayBootstrap bootstrap = GetFirstSceneComponent<Chapter1GameplayBootstrap>(scene);

            if (player == null)
            {
                ShowFailure("Scene is missing Player.");
                return;
            }

            RepairCamera(scene, cameraRig, gameplayCamera);
            EnsurePlayerRuntimeComponents(player, gameplayCamera, chapterManager);
            CreateInteractionTestGroup(scene, sceneRoot.transform);
            Chapter1HUD hud = CreateHud(sceneRoot.transform, player, gameplayCamera, chapterManager);
            EnsureEventSystem(sceneRoot.transform);
            LinkBootstrap(bootstrap, player, cameraRig, gameplayCamera, chapterManager, hud);
            LinkDebugOverlay(scene, player, cameraRig, gameplayCamera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene reloadedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Chapter1InteractionHudDiagnostic.DiagnosticResult result = Chapter1InteractionHudDiagnostic.RunDiagnosticForScene(reloadedScene, true);
            string message = $"PASS = {result.PassCount}\nWARNING = {result.WarningCount}\nERROR = {result.ErrorCount}";
            if (result.ErrorCount == 0)
            {
                EditorUtility.DisplayDialog("Repair and Validate Interaction and HUD", message, "OK");
                Debug.Log("[Chapter1 Interaction Builder] Repair and Validate Interaction and HUD completed successfully.");
            }
            else
            {
                EditorUtility.DisplayDialog("Repair and Validate Interaction and HUD", "Repair finished, but diagnostic still has errors.\n\n" + message, "OK");
                Debug.LogError("[Chapter1 Interaction Builder] Repair finished with diagnostic errors. Check Console lines prefixed with [Chapter1 Diagnostic] ERROR.");
            }
        }

        private static void ContinueRepairAfterPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= ContinueRepairAfterPlayMode;
            RepairAndValidateInteractionAndHud();
        }

        [MenuItem("Tools/Chapter 1/Delete Chapter 1 Test Save")]
        public static void DeleteChapter1TestSave()
        {
            if (!EditorUtility.DisplayDialog("Delete Chapter 1 Test Save", "Delete only the Chapter 1 JSON save file from persistentDataPath?", "Delete", "Cancel"))
            {
                return;
            }

            JsonChapter1SaveService saveService = new JsonChapter1SaveService();
            saveService.DeleteSave();
            Debug.Log($"[Chapter1 Interaction Builder] Deleted Chapter 1 save if present: {saveService.SavePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Chapter1", "Materials");
            EnsureFolder("Assets/Chapter1/Prefabs", "Gameplay");
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string path = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[Chapter1 Interaction Builder] Added layer '{layerName}' at slot {i}.");
                    return;
                }
            }

            Debug.LogWarning($"[Chapter1 Interaction Builder] No empty user layer slot for '{layerName}'.");
        }

        private static bool ValidatePlayerInputReferences()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Chapter1InputReader inputReader = playerPrefab != null ? playerPrefab.GetComponent<Chapter1InputReader>() : null;
            if (inputReader == null)
            {
                Debug.LogError("[Chapter1 Interaction Builder] Player prefab is missing Chapter1InputReader.");
                return false;
            }

            SerializedObject serializedReader = new SerializedObject(inputReader);
            bool valid = true;
            foreach (string fieldName in InputReferenceFields)
            {
                SerializedProperty property = serializedReader.FindProperty(fieldName);
                if (property == null || property.objectReferenceValue == null)
                {
                    Debug.LogError($"[Chapter1 Interaction Builder] Missing input reference field '{fieldName}'.");
                    valid = false;
                }
            }

            return valid;
        }

        private static Material CreateMaterialIfMissing(string assetPath, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color
            };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void CreatePickupPrefab(string prefabPath, string name, Chapter1ItemId itemId, string verb, string displayName, string message, Material material, PrimitiveType primitiveType, Vector3 visualScale, string persistentId)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            GameObject root = new GameObject(name);
            SetLayerRecursively(root, interactableLayer);

            GameObject body = GameObject.CreatePrimitive(primitiveType);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.up * Mathf.Max(0.1f, visualScale.y * 0.5f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = visualScale;
            SetLayerRecursively(body, interactableLayer);
            ApplyMaterial(body, material);

            GameObject interactionPoint = new GameObject("InteractionPoint");
            interactionPoint.transform.SetParent(root.transform);
            interactionPoint.transform.localPosition = Vector3.up * 0.45f;

            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.name = "Highlight";
            highlight.transform.SetParent(root.transform);
            highlight.transform.localPosition = Vector3.up * 0.45f;
            highlight.transform.localScale = Vector3.one * 0.55f;
            RemoveCollider(highlight);
            ApplyMaterial(highlight, AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath));
            highlight.SetActive(false);

            WorldPickupPersistence persistence = root.AddComponent<WorldPickupPersistence>();
            ItemPickup pickup = root.AddComponent<ItemPickup>();
            SetSerializedString(persistence, "persistentId", persistentId);
            SetSerializedObjectReference(pickup, "interactionPoint", interactionPoint.transform);
            SetSerializedObjectReference(pickup, "highlightObject", highlight);
            SetSerializedString(pickup, "displayName", displayName);
            SetSerializedString(pickup, "interactionVerb", verb);
            SetSerializedBool(pickup, "oneShot", true);
            SetSerializedInt(pickup, "itemId", (int)itemId);
            SetSerializedInt(pickup, "amount", 1);
            SetSerializedString(pickup, "pickupMessage", message);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateInspectableTablePrefab(Material tableMaterial, Material highlightMaterial)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            GameObject root = new GameObject("TestInspectableTable");
            SetLayerRecursively(root, interactableLayer);

            GameObject tabletop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabletop.name = "Tabletop";
            tabletop.transform.SetParent(root.transform);
            tabletop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            tabletop.transform.localScale = new Vector3(1.4f, 0.12f, 0.8f);
            SetLayerRecursively(tabletop, interactableLayer);
            ApplyMaterial(tabletop, tableMaterial);

            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i + 1:00}";
                leg.transform.SetParent(root.transform);
                float x = i < 2 ? -0.55f : 0.55f;
                float z = i % 2 == 0 ? -0.3f : 0.3f;
                leg.transform.localPosition = new Vector3(x, 0.35f, z);
                leg.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                SetLayerRecursively(leg, interactableLayer);
                ApplyMaterial(leg, tableMaterial);
            }

            GameObject interactionPoint = new GameObject("InteractionPoint");
            interactionPoint.transform.SetParent(root.transform);
            interactionPoint.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.name = "Highlight";
            highlight.transform.SetParent(root.transform);
            highlight.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            highlight.transform.localScale = new Vector3(1.55f, 0.16f, 0.95f);
            RemoveCollider(highlight);
            ApplyMaterial(highlight, highlightMaterial);
            highlight.SetActive(false);

            TestInspectableInteractable inspectable = root.AddComponent<TestInspectableInteractable>();
            SetSerializedObjectReference(inspectable, "interactionPoint", interactionPoint.transform);
            SetSerializedObjectReference(inspectable, "highlightObject", highlight);
            SetSerializedString(inspectable, "displayName", "chiếc bàn");
            SetSerializedString(inspectable, "interactionVerb", "Kiểm tra");

            PrefabUtility.SaveAsPrefabAsset(root, TablePrefabPath);
            Object.DestroyImmediate(root);
        }

        private static bool UpdatePlayerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogError($"[Chapter1 Interaction Builder] Missing player prefab: {PlayerPrefabPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                EnsurePlayerRuntimeComponents(root, null, null);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return saved != null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsurePlayerRuntimeComponents(GameObject player, Camera gameplayCamera, Chapter1Manager manager)
        {
            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
            PlayerInventory inventory = EnsureComponent<PlayerInventory>(player);
            Chapter1InteractionController interactionController = EnsureComponent<Chapter1InteractionController>(player);
            FlashlightController flashlightController = EnsureComponent<FlashlightController>(player);

            Transform pivot = EnsureChild(player.transform, "FlashlightPivot");
            pivot.localPosition = new Vector3(0.25f, 1.45f, 0.35f);
            Light flashlightLight = EnsureFlashlightLight(pivot);

            SetSerializedObjectReference(inventory, "chapterManager", manager);
            SetSerializedObjectReference(interactionController, "inputReader", inputReader);
            SetSerializedObjectReference(interactionController, "inputLock", inputLock);
            SetSerializedObjectReference(interactionController, "inventory", inventory);
            SetSerializedObjectReference(interactionController, "gameplayCamera", gameplayCamera);
            SetSerializedObjectReference(interactionController, "chapterManager", manager);
            SetSerializedInt(interactionController, "interactionMask", LayerMask.GetMask("Interactable"));
            SetSerializedInt(interactionController, "obstructionMask", LayerMask.GetMask("Environment"));
            SetSerializedInt(interactionController, "triggerInteraction", (int)QueryTriggerInteraction.Collide);
            SetSerializedFloat(interactionController, "interactionDistance", 3f);
            SetSerializedFloat(interactionController, "sphereRadius", 0.2f);
            SetSerializedBool(interactionController, "showInteractionDebug", true);
            SetSerializedBool(interactionController, "logFocusChanges", true);
            SetSerializedObjectReference(flashlightController, "inputReader", inputReader);
            SetSerializedObjectReference(flashlightController, "inputLock", inputLock);
            SetSerializedObjectReference(flashlightController, "inventory", inventory);
            SetSerializedObjectReference(flashlightController, "flashlightLight", flashlightLight);
            SetSerializedObjectReference(flashlightController, "flashlightPivot", pivot);
            SetSerializedObjectReference(flashlightController, "gameplayCamera", gameplayCamera);
            EditorUtility.SetDirty(player);
        }

        private static Light EnsureFlashlightLight(Transform pivot)
        {
            Transform lightTransform = pivot.Find("FlashlightLight") ?? EnsureChild(pivot, "FlashlightLight");
            lightTransform.localPosition = Vector3.zero;
            lightTransform.localRotation = Quaternion.identity;
            Light flashlightLight = lightTransform.GetComponent<Light>();
            if (flashlightLight == null)
            {
                flashlightLight = lightTransform.gameObject.AddComponent<Light>();
            }

            flashlightLight.type = LightType.Spot;
            flashlightLight.range = 12f;
            flashlightLight.spotAngle = 50f;
            flashlightLight.innerSpotAngle = 25f;
            flashlightLight.intensity = 3.5f;
            flashlightLight.shadows = LightShadows.Soft;
            flashlightLight.enabled = false;
            return flashlightLight;
        }

        private static void CreateInteractionTestGroup(Scene scene, Transform sceneRoot)
        {
            Transform group = sceneRoot.Find("InteractionInventoryTest") ?? FindChildRecursive(sceneRoot, "InteractionInventoryTest") ?? EnsureChild(sceneRoot, "InteractionInventoryTest");
            group.SetParent(sceneRoot, true);
            Chapter1InteractionRuntimeSelfTest selfTest = EnsureComponent<Chapter1InteractionRuntimeSelfTest>(group.gameObject);
            SetSerializedBool(selfTest, "runOnStart", true);
            SetSerializedBool(selfTest, "showDetailedLogs", true);

            InstantiateTestPrefab(scene, group, FlashlightPrefabPath, "Pickup_Flashlight_Test", new Vector3(1.1f, 0f, 1.9f), "pickup.flashlight.test");
            InstantiateTestPrefab(scene, group, FusePrefabPath, "Pickup_Fuse_Test", new Vector3(-1.1f, 0f, 2f), "pickup.fuse.test");
            InstantiateTestPrefab(scene, group, CanPrefabPath, "Pickup_Can_Test_01", new Vector3(0.35f, 0f, 2.45f), "pickup.can.test.01");
            InstantiateTestPrefab(scene, group, CanPrefabPath, "Pickup_Can_Test_02", new Vector3(0.85f, 0f, 2.55f), "pickup.can.test.02");
            InstantiateTestPrefab(scene, group, CanPrefabPath, "Pickup_Can_Test_03", new Vector3(1.35f, 0f, 2.65f), "pickup.can.test.03");
            InstantiateTestPrefab(scene, group, TablePrefabPath, "TestInspectableTable", new Vector3(-1.8f, 0f, 2.3f), "inspectable.table.test");
        }

        private static void InstantiateTestPrefab(Scene scene, Transform parent, string prefabPath, string name, Vector3 position, string persistentId)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Chapter1 Interaction Builder] Missing prefab: {prefabPath}");
                return;
            }

            GameObject instance = FindSceneObject(scene, name);
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            }

            if (instance == null)
            {
                return;
            }

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            instance.name = name;
            instance.SetActive(true);
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetLayerRecursively(instance, interactableLayer);
            EnsureColliderExists(instance);

            WorldPickupPersistence persistence = instance.GetComponent<WorldPickupPersistence>();
            if (persistence != null)
            {
                SetSerializedString(persistence, "persistentId", persistentId);
                PrefabUtility.RecordPrefabInstancePropertyModifications(persistence);
            }
        }

        private static Chapter1HUD CreateHud(Transform sceneRoot, GameObject player, Camera gameplayCamera, Chapter1Manager manager)
        {
            Transform uiRoot = EnsureChild(sceneRoot, "UI");
            Transform existingHud = uiRoot.Find("Chapter1HUD") ?? FindChildRecursive(sceneRoot, "Chapter1HUD");
            GameObject hudObject = existingHud != null
                ? existingHud.gameObject
                : new GameObject("Chapter1HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Chapter1HUD));
            hudObject.SetActive(true);
            hudObject.transform.SetParent(uiRoot, false);
            RectTransform hudRect = hudObject.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            Canvas canvas = EnsureComponent<Canvas>(hudObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = EnsureComponent<CanvasScaler>(hudObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            EnsureComponent<GraphicRaycaster>(hudObject);

            InteractionPromptUI promptUI = CreatePromptUI(hudRect);
            StaminaHUD staminaHUD = CreateStaminaUI(hudRect);
            InventoryHUD inventoryHUD = CreateInventoryUI(hudRect);
            NotificationUI notificationUI = CreateNotificationUI(hudRect);
            ObjectiveHUD objectiveHUD = CreateObjectiveUI(hudRect);
            CreateCrosshairUI(hudRect);

            Chapter1HUD hud = EnsureComponent<Chapter1HUD>(hudObject);
            SetSerializedObjectReference(hud, "interactionPromptUI", promptUI);
            SetSerializedObjectReference(hud, "staminaHUD", staminaHUD);
            SetSerializedObjectReference(hud, "inventoryHUD", inventoryHUD);
            SetSerializedObjectReference(hud, "notificationUI", notificationUI);
            SetSerializedObjectReference(hud, "objectiveHUD", objectiveHUD);

            Chapter1InteractionController interactionController = player.GetComponent<Chapter1InteractionController>();
            PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
            PlayerStamina stamina = player.GetComponent<PlayerStamina>();
            Chapter1PlayerMotor motor = player.GetComponent<Chapter1PlayerMotor>();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            FlashlightController flashlight = player.GetComponent<FlashlightController>();
            hud.Configure(manager, interactionController, inputLock, stamina, motor, inventory, flashlight);
            return hud;
        }

        private static InteractionPromptUI CreatePromptUI(RectTransform parent)
        {
            GameObject panel = CreateUiPanel(parent, "InteractionPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520f, 72f), new Vector2(0f, 155f));
            TextMeshProUGUI text = CreateText(panel.transform, "PromptText", "[F] Nhặt đèn pin", 30f, TextAlignmentOptions.Center);
            InteractionPromptUI promptUI = EnsureComponent<InteractionPromptUI>(panel);
            SetSerializedObjectReference(promptUI, "canvasGroup", panel.GetComponent<CanvasGroup>());
            SetSerializedObjectReference(promptUI, "promptText", text);
            return promptUI;
        }

        private static StaminaHUD CreateStaminaUI(RectTransform parent)
        {
            GameObject panel = CreateUiPanel(parent, "StaminaHUD", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(300f, 42f), new Vector2(190f, 88f));
            Slider slider = EnsureComponent<Slider>(panel);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Transform existingFill = panel.transform.Find("Fill");
            GameObject fill = existingFill != null ? existingFill.gameObject : new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(panel.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(8f, 8f);
            fillRect.offsetMax = new Vector2(-8f, -8f);
            Image fillImage = EnsureComponent<Image>(fill);
            fillImage.color = new Color(0.35f, 0.85f, 0.48f);
            fillImage.raycastTarget = false;
            slider.fillRect = fillRect;

            StaminaHUD staminaHUD = EnsureComponent<StaminaHUD>(panel);
            SetSerializedObjectReference(staminaHUD, "canvasGroup", panel.GetComponent<CanvasGroup>());
            SetSerializedObjectReference(staminaHUD, "staminaSlider", slider);
            return staminaHUD;
        }

        private static InventoryHUD CreateInventoryUI(RectTransform parent)
        {
            GameObject panel = CreateUiPanel(parent, "InventoryHUD", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(440f, 112f), new Vector2(-250f, 92f));
            TextMeshProUGUI flashlight = CreateText(panel.transform, "FlashlightText", "Đèn pin: Chưa có", 22f, TextAlignmentOptions.Center);
            TextMeshProUGUI fuse = CreateText(panel.transform, "FuseText", "Cầu chì: Chưa có", 22f, TextAlignmentOptions.Center);
            TextMeshProUGUI can = CreateText(panel.transform, "CanText", "Lon: x0", 22f, TextAlignmentOptions.Center);
            TextMeshProUGUI hardDrive = CreateText(panel.transform, "HardDriveText", "Ổ cứng: Chưa có", 22f, TextAlignmentOptions.Center);
            LayoutInventoryTexts(panel.transform);

            InventoryHUD inventoryHUD = EnsureComponent<InventoryHUD>(panel);
            SetSerializedObjectReference(inventoryHUD, "flashlightText", flashlight);
            SetSerializedObjectReference(inventoryHUD, "fuseText", fuse);
            SetSerializedObjectReference(inventoryHUD, "canText", can);
            SetSerializedObjectReference(inventoryHUD, "hardDriveText", hardDrive);
            return inventoryHUD;
        }

        private static NotificationUI CreateNotificationUI(RectTransform parent)
        {
            GameObject panel = CreateUiPanel(parent, "NotificationUI", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(720f, 72f), new Vector2(0f, 70f));
            TextMeshProUGUI text = CreateText(panel.transform, "NotificationText", string.Empty, 26f, TextAlignmentOptions.Center);
            NotificationUI notificationUI = EnsureComponent<NotificationUI>(panel);
            SetSerializedObjectReference(notificationUI, "canvasGroup", panel.GetComponent<CanvasGroup>());
            SetSerializedObjectReference(notificationUI, "notificationText", text);
            return notificationUI;
        }

        private static ObjectiveHUD CreateObjectiveUI(RectTransform parent)
        {
            GameObject panel = CreateUiPanel(parent, "ObjectiveHUD", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 100f), new Vector2(280f, -82f));
            TextMeshProUGUI label = CreateText(panel.transform, "ObjectiveLabel", "Mục tiêu", 22f, TextAlignmentOptions.Left);
            TextMeshProUGUI objective = CreateText(panel.transform, "ObjectiveText", "Nói chuyện với Nam.", 24f, TextAlignmentOptions.Left);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.55f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(18f, 0f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            RectTransform objectiveRect = objective.GetComponent<RectTransform>();
            objectiveRect.anchorMin = new Vector2(0f, 0f);
            objectiveRect.anchorMax = new Vector2(1f, 0.58f);
            objectiveRect.offsetMin = new Vector2(18f, 8f);
            objectiveRect.offsetMax = new Vector2(-18f, 0f);

            ObjectiveHUD objectiveHUD = EnsureComponent<ObjectiveHUD>(panel);
            SetSerializedObjectReference(objectiveHUD, "labelText", label);
            SetSerializedObjectReference(objectiveHUD, "objectiveText", objective);
            return objectiveHUD;
        }

        private static void CreateCrosshairUI(RectTransform parent)
        {
            Transform existing = parent.Find("Crosshair") ?? FindChildRecursive(parent, "Crosshair");
            GameObject crosshair = existing != null ? existing.gameObject : new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshair.transform.SetParent(parent, false);
            crosshair.SetActive(true);

            RectTransform rect = crosshair.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(8f, 8f);
            rect.anchoredPosition = Vector2.zero;

            Image image = EnsureComponent<Image>(crosshair);
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static GameObject CreateUiPanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
        {
            Transform existing = parent.Find(name) ?? FindChildRecursive(parent, name);
            GameObject panel = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.SetActive(true);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : rect.pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            Image image = EnsureComponent<Image>(panel);
            image.color = new Color(0f, 0f, 0f, 0.42f);
            image.raycastTarget = false;
            CanvasGroup canvasGroup = EnsureComponent<CanvasGroup>(panel);
            canvasGroup.blocksRaycasts = false;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            if (existing == null && name.EndsWith("Text", StringComparison.Ordinal))
            {
                existing = parent.Find(name.Substring(0, name.Length - 4));
            }

            GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.name = name;
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 8f);
            rect.offsetMax = new Vector2(-12f, -8f);
            TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(textObject);
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void LayoutInventoryTexts(Transform panel)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                RectTransform rect = panel.GetChild(i).GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = new Vector2(i * 0.25f, 0f);
                rect.anchorMax = new Vector2((i + 1) * 0.25f, 1f);
                rect.offsetMin = new Vector2(6f, 8f);
                rect.offsetMax = new Vector2(-6f, -8f);
            }
        }

        private static void EnsureEventSystem(Transform uiRoot)
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(uiRoot);
                return;
            }

            eventSystem.gameObject.SetActive(true);
            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.DestroyImmediate(oldModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void RepairCamera(Scene scene, GameObject cameraRig, Camera gameplayCamera)
        {
            if (gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.gameObject.SetActive(true);
            gameplayCamera.enabled = true;
            if (cameraRig != null && !gameplayCamera.transform.IsChildOf(cameraRig.transform))
            {
                gameplayCamera.transform.SetParent(cameraRig.transform, true);
            }

            try
            {
                gameplayCamera.tag = "MainCamera";
            }
            catch (UnityException exception)
            {
                Debug.LogWarning($"[Chapter1 Interaction Builder] Không thể gán tag MainCamera cho camera: {exception.Message}");
            }

            List<AudioListener> listeners = GetSceneComponents<AudioListener>(scene);
            if (gameplayCamera.GetComponent<AudioListener>() == null && listeners.Count == 0)
            {
                gameplayCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        private static void LinkBootstrap(Chapter1GameplayBootstrap bootstrap, GameObject player, GameObject cameraRig, Camera gameplayCamera, Chapter1Manager manager, Chapter1HUD hud)
        {
            if (bootstrap == null)
            {
                return;
            }

            SetSerializedObjectReference(bootstrap, "chapter1Manager", manager);
            SetSerializedObjectReference(bootstrap, "player", player.transform);
            SetSerializedObjectReference(bootstrap, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetSerializedObjectReference(bootstrap, "cameraRig", cameraRig != null ? cameraRig.GetComponent<ThirdPersonCameraRig>() : null);
            SetSerializedObjectReference(bootstrap, "playerInputLock", player.GetComponent<PlayerInputLock>());
            SetSerializedObjectReference(bootstrap, "cameraTarget", player.GetComponentInChildren<CameraTarget>(true));
            SetSerializedObjectReference(bootstrap, "playerInventory", player.GetComponent<PlayerInventory>());
            SetSerializedObjectReference(bootstrap, "interactionController", player.GetComponent<Chapter1InteractionController>());
            SetSerializedObjectReference(bootstrap, "flashlightController", player.GetComponent<FlashlightController>());
            SetSerializedObjectReference(bootstrap, "chapter1HUD", hud);
            SetSerializedObjectReference(bootstrap, "gameplayCamera", gameplayCamera);
        }

        private static void LinkDebugOverlay(Scene scene, GameObject player, GameObject cameraRig, Camera gameplayCamera)
        {
            PlayerDebugOverlay debugOverlay = GetFirstSceneComponent<PlayerDebugOverlay>(scene);
            if (debugOverlay == null || player == null)
            {
                return;
            }

            SetSerializedObjectReference(debugOverlay, "inputReader", player.GetComponent<Chapter1InputReader>());
            SetSerializedObjectReference(debugOverlay, "playerMotor", player.GetComponent<Chapter1PlayerMotor>());
            SetSerializedObjectReference(debugOverlay, "playerStamina", player.GetComponent<PlayerStamina>());
            SetSerializedObjectReference(debugOverlay, "inputLock", player.GetComponent<PlayerInputLock>());
            SetSerializedObjectReference(debugOverlay, "cameraRig", cameraRig != null ? cameraRig.GetComponent<ThirdPersonCameraRig>() : null);
            SetSerializedObjectReference(debugOverlay, "interactionController", player.GetComponent<Chapter1InteractionController>());
            SetSerializedObjectReference(debugOverlay, "gameplayCamera", gameplayCamera);
        }


        private static bool SaveCurrentModifiedScenesOrCancel(string title)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                }
            }

            return true;
        }

        private static GameObject FindScenePlayer(Scene scene)
        {
            return FindSceneObject(scene, "Player") ?? GetFirstSceneComponent<Chapter1InputReader>(scene)?.gameObject;
        }

        private static GameObject CreateSceneRoot(Scene scene, string name)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
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

        private static T GetFirstSceneComponent<T>(Scene scene) where T : Component
        {
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;
            return childObject.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void EnsureColliderExists(GameObject gameObject)
        {
            if (gameObject == null || gameObject.GetComponentInChildren<Collider>(true) != null)
            {
                return;
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (gameObject == null || layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void ApplyMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
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
                Debug.LogWarning($"[Chapter1 Interaction Builder] Missing serialized field '{propertyName}' on '{targetObject.name}'.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        private static void SetSerializedString(Object targetObject, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void SetSerializedBool(Object targetObject, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void SetSerializedInt(Object targetObject, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void SetSerializedFloat(Object targetObject, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void ShowFailure(string message)
        {
            Debug.LogError($"[Chapter1 Interaction Builder] ERROR: {message}");
            EditorUtility.DisplayDialog("Add Interaction and Inventory Prototype", message, "OK");
        }
    }
}
