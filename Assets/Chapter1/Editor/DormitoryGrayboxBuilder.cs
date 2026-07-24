using System;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class DormitoryGrayboxBuilder
    {
        private const string PrototypeScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string DormitoryScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string PlayerPrefabPath = "Assets/Chapter1/Prefabs/Characters/Player.prefab";
        private const string CameraRigPrefabPath = "Assets/Chapter1/Prefabs/Gameplay/ThirdPersonCameraRig.prefab";
        private const string MaterialFolderPath = "Assets/Chapter1/Materials/Graybox";
        private const string TmpDefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static readonly string[] RootChildNames =
        {
            "Environment",
            "Rooms",
            "DebugLabels",
            "SpawnPoints",
            "ObjectiveMarkers",
            "NavigationMarkers",
            "MapGuide",
            "Lighting",
            "PlayerSetup",
            "Managers"
        };

        private static readonly string[] EnvironmentChildNames =
        {
            "Floors",
            "Walls",
            "Ceilings",
            "Doors",
            "Stairs",
            "Railings",
            "DarkHallway",
            "Rooftop",
            "RestaurantExterior"
        };

        private static readonly string[] RequiredAreas =
        {
            "Room_Nam",
            "Room_Minh",
            "ComputerRoom",
            "EquipmentStorage",
            "Restroom",
            "MainHallway",
            "DarkHallway",
            "Staircase",
            "Rooftop",
            "Restaurant_Opposite"
        };

        private static readonly string[] SpawnNames =
        {
            "Spawn_ChapterStart",
            "Spawn_RoomNam",
            "Spawn_RoomMinh",
            "Spawn_ComputerRoom",
            "Spawn_EquipmentStorage",
            "Spawn_MainHallway",
            "Spawn_DarkHallway",
            "Spawn_Staircase",
            "Spawn_Rooftop",
            "Spawn_Restaurant"
        };

        private static readonly string[] ObjectiveNames =
        {
            "Objective_LeaveNamRoom",
            "Objective_EnterMainHallway",
            "Objective_ComputerRoom",
            "Objective_DarkHallwayEntry",
            "Objective_EquipmentStorage",
            "Objective_Staircase",
            "Objective_Rooftop",
            "Objective_Restaurant"
        };

        private static readonly string[] DebugLabelNames =
        {
            "Label_Room_Nam",
            "Label_Room_Minh",
            "Label_ComputerRoom",
            "Label_EquipmentStorage",
            "Label_Restroom",
            "Label_MainHallway",
            "Label_DarkHallway",
            "Label_Staircase",
            "Label_Rooftop",
            "Label_Restaurant"
        };

        private static readonly string[] GrayboxMaterialFileNames =
        {
            "MAT_Graybox_Floor.mat",
            "MAT_Graybox_Wall.mat",
            "MAT_Graybox_NamRoom.mat",
            "MAT_Graybox_MinhRoom.mat",
            "MAT_Graybox_ComputerRoom.mat",
            "MAT_Graybox_Storage.mat",
            "MAT_Graybox_Restroom.mat",
            "MAT_Graybox_MainHallway.mat",
            "MAT_Graybox_DarkHallway.mat",
            "MAT_Graybox_Staircase.mat",
            "MAT_Graybox_Rooftop.mat",
            "MAT_Graybox_Restaurant.mat",
            "MAT_Graybox_Objective.mat",
            "MAT_Graybox_Spawn.mat",
            "MAT_Graybox_DoorDebug.mat"
        };

        private static readonly string[] DefaultObjectNames =
        {
            "Cube",
            "GameObject",
            "New Game Object"
        };

        private sealed class BuildContext
        {
            public Scene Scene;
            public Transform Root;
            public Transform Environment;
            public Transform Floors;
            public Transform Walls;
            public Transform Ceilings;
            public Transform Doors;
            public Transform Stairs;
            public Transform Railings;
            public Transform DarkHallway;
            public Transform Rooftop;
            public Transform RestaurantExterior;
            public Transform Rooms;
            public Transform DebugLabels;
            public Transform SpawnPoints;
            public Transform ObjectiveMarkers;
            public Transform NavigationMarkers;
            public Transform MapGuide;
            public Transform Lighting;
            public Transform PlayerSetup;
            public Transform Managers;
            public Dictionary<string, Material> Materials;
        }

        [MenuItem("Tools/Chapter 1/Build Dormitory Graybox")]
        public static void BuildDormitoryGraybox()
        {
            BuildDormitoryGrayboxInternal(true);
        }

        public static bool BuildDormitoryGrayboxForAutomation()
        {
            return BuildDormitoryGrayboxInternal(false);
        }

        private static bool BuildDormitoryGrayboxInternal(bool showDialog)
        {
            if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Dormitory Graybox] Build canceled because current scene changes were not saved.");
                return false;
            }

            if (!ValidateSourceAssets())
            {
                return false;
            }

            EnsureFolders();
            EnsureLayer("Environment");
            EnsureLayer("Player");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Chapter1_Dormitory";
            BuildContext context = CreateContext(scene);
            CreateMaterials(context);
            BuildDormitoryLayout(context);
            GameObject player = CreatePlayerSetup(context);
            GameObject cameraRig = CreateCameraRig(context, player);
            CopyManagersFromPrototype(context, player, cameraRig);
            CreateLighting(context);
            NormalizeGameplayCameraAndAudio(context.Scene, cameraRig.GetComponentInChildren<Camera>(true));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, DormitoryScenePath))
            {
                Debug.LogError($"[Dormitory Graybox] Failed to save scene: {DormitoryScenePath}");
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Dormitory Graybox] Built and saved {DormitoryScenePath}");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Build Dormitory Graybox", $"Built and saved:\n{DormitoryScenePath}", "OK");
            }

            return true;
        }

        [MenuItem("Tools/Chapter 1/Validate Dormitory Graybox")]
        public static void ValidateDormitoryGraybox()
        {
            ValidateDormitoryGrayboxInternal(true);
        }

        public static bool ValidateDormitoryGrayboxForAutomation()
        {
            return ValidateDormitoryGrayboxInternal(false);
        }

        private static bool ValidateDormitoryGrayboxInternal(bool showDialog)
        {
            int passed = 0;
            int failed = 0;

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(DormitoryScenePath);
            Check(sceneAsset != null, "Scene Chapter1_Dormitory exists", ref passed, ref failed);
            if (sceneAsset == null)
            {
                Debug.LogError($"Dormitory validation: {passed} passed, {failed} failed.");
                return false;
            }

            bool openedByValidator = false;
            Scene scene = GetLoadedScene(DormitoryScenePath);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(DormitoryScenePath, OpenSceneMode.Additive);
                openedByValidator = true;
            }

            try
            {
                Check(scene.IsValid() && scene.isLoaded, "Scene loaded", ref passed, ref failed);
                Check(!string.IsNullOrWhiteSpace(scene.path), "Scene has saved path", ref passed, ref failed);
                CheckSceneMaterials(ref passed, ref failed);

                GameObject root = GetRootObject(scene, "Chapter1_Dormitory");
                Check(root != null, "Root Chapter1_Dormitory found", ref passed, ref failed);
                Check(CountSceneObjects(scene, "Chapter1_Dormitory") == 1, "Only one Chapter1_Dormitory root", ref passed, ref failed);
                if (root != null)
                {
                    CheckRequiredDirectChildren(root.transform, RootChildNames, "Root hierarchy", ref passed, ref failed);
                    Transform environment = root.transform.Find("Environment");
                    if (environment != null)
                    {
                        CheckRequiredDirectChildren(environment, EnvironmentChildNames, "Environment hierarchy", ref passed, ref failed);
                    }
                }

                GameObject player = FindSceneObject(scene, "Player");
                Check(player != null, "Player found", ref passed, ref failed);
                Check(player != null && PrefabUtility.GetCorrespondingObjectFromSource(player) != null, "Player is a prefab instance", ref passed, ref failed);
                Check(player != null && player.GetComponent<Chapter1PlayerMotor>() != null, "Player has Chapter1PlayerMotor", ref passed, ref failed);
                Check(player != null && player.GetComponent<Chapter1InputReader>() != null, "Player has Chapter1InputReader", ref passed, ref failed);
                Check(player != null && player.GetComponent<PlayerStamina>() != null, "Player keeps stamina system", ref passed, ref failed);
                Check(player != null && player.GetComponent<PlayerInputLock>() != null, "Player keeps input lock", ref passed, ref failed);
                Check(CountSceneComponents<Chapter1PlayerMotor>(scene) == 1, "Only one gameplay player motor", ref passed, ref failed);

                Camera gameplayCamera = GetMainGameplayCamera(scene);
                ThirdPersonCameraRig cameraRig = GetFirstSceneComponent<ThirdPersonCameraRig>(scene);
                Check(gameplayCamera != null, "Gameplay Main Camera found", ref passed, ref failed);
                Check(cameraRig != null, "ThirdPersonCameraRig found", ref passed, ref failed);
                Check(cameraRig != null && PrefabUtility.GetCorrespondingObjectFromSource(cameraRig.gameObject) != null, "CameraRig is a prefab instance", ref passed, ref failed);
                Check(CountActiveMainCameras(scene) == 1, "Only one active MainCamera", ref passed, ref failed);
                Check(CountActiveAudioListeners(scene) == 1, "Only one active AudioListener", ref passed, ref failed);
                Check(cameraRig != null && gameplayCamera != null && HasSerializedObjectReference(cameraRig, "controlledCamera", gameplayCamera), "CameraRig controlledCamera linked", ref passed, ref failed);
                Check(cameraRig != null && player != null && HasSerializedObjectReference(cameraRig, "target", GetCameraTargetTransform(player)), "CameraRig target linked to player CameraTarget", ref passed, ref failed);
                Check(cameraRig != null && SerializedLayerMaskContains(cameraRig, "collisionMask", "Environment"), "CameraRig collisionMask includes Environment", ref passed, ref failed);

                Chapter1Manager manager = GetFirstSceneComponent<Chapter1Manager>(scene);
                Chapter1GameplayBootstrap bootstrap = GetFirstSceneComponent<Chapter1GameplayBootstrap>(scene);
                PlayerDebugOverlay debugOverlay = GetFirstSceneComponent<PlayerDebugOverlay>(scene);
                Check(manager != null, "Chapter1Manager found", ref passed, ref failed);
                Check(bootstrap != null, "Chapter1GameplayBootstrap found", ref passed, ref failed);
                Check(debugOverlay != null, "PlayerDebugOverlay found", ref passed, ref failed);
                Check(CountSceneComponents<Chapter1Manager>(scene) == 1, "Only one Chapter1Manager", ref passed, ref failed);
                Check(CountSceneComponents<Chapter1GameplayBootstrap>(scene) == 1, "Only one Chapter1GameplayBootstrap", ref passed, ref failed);
                Check(bootstrap != null && manager != null && HasSerializedObjectReference(bootstrap, "chapter1Manager", manager), "Bootstrap manager reference linked", ref passed, ref failed);
                Check(bootstrap != null && player != null && HasSerializedObjectReference(bootstrap, "player", player.transform), "Bootstrap player reference linked", ref passed, ref failed);
                Check(bootstrap != null && cameraRig != null && HasSerializedObjectReference(bootstrap, "cameraRig", cameraRig), "Bootstrap cameraRig reference linked", ref passed, ref failed);
                Check(bootstrap != null && gameplayCamera != null && HasSerializedObjectReference(bootstrap, "gameplayCamera", gameplayCamera), "Bootstrap gameplayCamera reference linked", ref passed, ref failed);

                for (int i = 0; i < RequiredAreas.Length; i++)
                {
                    CheckRequiredArea(scene, RequiredAreas[i], ref passed, ref failed);
                }

                for (int i = 0; i < SpawnNames.Length; i++)
                {
                    CheckSpawnPoint(scene, SpawnNames[i], ref passed, ref failed);
                }

                for (int i = 0; i < ObjectiveNames.Length; i++)
                {
                    CheckObjectiveMarker(scene, ObjectiveNames[i], ref passed, ref failed);
                }
                CheckObjectiveMarkerIdsUnique(scene, ref passed, ref failed);

                CheckEnvironmentColliders(scene, "Floor", ref passed, ref failed);
                CheckEnvironmentColliders(scene, "Wall", ref passed, ref failed);
                CheckNamedBoxCollider(scene, "Staircase_Ramp", "Staircase ramp has BoxCollider", ref passed, ref failed);
                CheckNamedBoxCollider(scene, "Floor_Rooftop", "Rooftop floor has BoxCollider", ref passed, ref failed);
                CheckNamedBoxCollider(scene, "Floor_Restaurant_Opposite", "Restaurant exterior floor has BoxCollider", ref passed, ref failed);
                CheckNamedBoxCollider(scene, "Floor_DormitoryEntrance", "Dormitory entrance connector has BoxCollider", ref passed, ref failed);
                CheckNamedBoxCollider(scene, "Railing_Staircase_Left", "Staircase railings have colliders", ref passed, ref failed);
                CheckNoCollider(scene, "Door_Room_Nam", "Nam room door debug does not block player", ref passed, ref failed);
                CheckNoCollider(scene, "Door_EquipmentStorage", "Equipment storage door debug does not block player", ref passed, ref failed);
                CheckRequiredRoute(scene, ref passed, ref failed);
                CheckDarkHallwayLighting(scene, ref passed, ref failed);
                CheckDebugLabels(scene, ref passed, ref failed);
                CheckMapGuide(scene, ref passed, ref failed);
                Check(!IsPlayerInsideCollider(player), "Player does not start inside collider", ref passed, ref failed);
                Check(!HasMissingScripts(scene), "Scene has no Missing Script components", ref passed, ref failed);
                Check(!HasDefaultNamedObjects(scene), "Scene has no default Cube/GameObject names", ref passed, ref failed);
                Check(!HasMissingOrInvalidRendererMaterials(scene), "Scene renderers have valid materials and shaders", ref passed, ref failed);
                Check(TextMeshProEssentialsAreAvailable(), "TextMeshPro Essentials font asset available", ref passed, ref failed);
                Check(AllTextMeshProLabelsHaveFonts(scene), "Text labels have no missing TMP font asset", ref passed, ref failed);
            }
            finally
            {
                if (openedByValidator && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Debug.Log($"Dormitory validation: {passed} passed, {failed} failed.");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Validate Dormitory Graybox", $"Dormitory validation: {passed} passed, {failed} failed.", "OK");
            }

            return failed == 0;
        }

        private static bool ValidateSourceAssets()
        {
            bool valid = true;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScenePath) == null)
            {
                Debug.LogError($"[Dormitory Graybox] Missing prototype scene: {PrototypeScenePath}");
                valid = false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogError($"[Dormitory Graybox] Missing player prefab: {PlayerPrefabPath}");
                valid = false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath) == null)
            {
                Debug.LogError($"[Dormitory Graybox] Missing camera rig prefab: {CameraRigPrefabPath}");
                valid = false;
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                if (playerPrefab.GetComponent<Chapter1PlayerMotor>() == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Player prefab is missing {nameof(Chapter1PlayerMotor)}: {PlayerPrefabPath}");
                    valid = false;
                }

                if (playerPrefab.GetComponent<Chapter1InputReader>() == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Player prefab is missing {nameof(Chapter1InputReader)}: {PlayerPrefabPath}");
                    valid = false;
                }
            }

            GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath);
            if (cameraPrefab != null)
            {
                if (cameraPrefab.GetComponent<ThirdPersonCameraRig>() == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Camera prefab is missing {nameof(ThirdPersonCameraRig)}: {CameraRigPrefabPath}");
                    valid = false;
                }

                if (cameraPrefab.GetComponentInChildren<Camera>(true) == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Camera prefab has no child Camera: {CameraRigPrefabPath}");
                    valid = false;
                }
            }

            if (!ValidatePrototypeSceneContents())
            {
                valid = false;
            }

            return valid;
        }

        private static bool ValidatePrototypeSceneContents()
        {
            bool valid = true;
            bool openedByValidation = false;
            Scene prototypeScene = GetLoadedScene(PrototypeScenePath);
            if (!prototypeScene.IsValid())
            {
                prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
                openedByValidation = true;
            }

            try
            {
                if (!prototypeScene.IsValid() || !prototypeScene.isLoaded)
                {
                    Debug.LogError($"[Dormitory Graybox] Could not open prototype scene: {PrototypeScenePath}");
                    return false;
                }

                if (GetFirstSceneComponent<Chapter1Manager>(prototypeScene) == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Prototype scene is missing {nameof(Chapter1Manager)}.");
                    valid = false;
                }

                if (GetFirstSceneComponent<Chapter1GameplayBootstrap>(prototypeScene) == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Prototype scene is missing {nameof(Chapter1GameplayBootstrap)}.");
                    valid = false;
                }

                if (GetFirstSceneComponent<Chapter1PlayerMotor>(prototypeScene) == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Prototype scene is missing a gameplay player with {nameof(Chapter1PlayerMotor)}.");
                    valid = false;
                }

                if (GetFirstSceneComponent<ThirdPersonCameraRig>(prototypeScene) == null)
                {
                    Debug.LogError($"[Dormitory Graybox] Prototype scene is missing {nameof(ThirdPersonCameraRig)}.");
                    valid = false;
                }

                if (GetFirstSceneComponent<Camera>(prototypeScene) == null)
                {
                    Debug.LogError("[Dormitory Graybox] Prototype scene is missing a gameplay Camera.");
                    valid = false;
                }
            }
            finally
            {
                if (openedByValidation && prototypeScene.IsValid())
                {
                    EditorSceneManager.CloseScene(prototypeScene, true);
                }
            }

            return valid;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Chapter1");
            EnsureFolder("Assets/Chapter1", "Scenes");
            EnsureFolder("Assets/Chapter1", "Materials");
            EnsureFolder("Assets/Chapter1/Materials", "Graybox");
            EnsureFolder("Assets/Chapter1", "Editor");
            EnsureFolder("Assets/Chapter1/Scripts", "World");
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
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[Dormitory Graybox] Added layer {layerName} at slot {i}.");
                    return;
                }
            }

            Debug.LogWarning($"[Dormitory Graybox] No empty layer slot for {layerName}.");
        }

        private static BuildContext CreateContext(Scene scene)
        {
            GameObject root = new GameObject("Chapter1_Dormitory");
            SceneManager.MoveGameObjectToScene(root, scene);

            BuildContext context = new BuildContext
            {
                Scene = scene,
                Root = root.transform,
                Materials = new Dictionary<string, Material>(StringComparer.Ordinal)
            };

            context.Environment = CreateChild(context.Root, "Environment");
            context.Floors = CreateChild(context.Environment, "Floors");
            context.Walls = CreateChild(context.Environment, "Walls");
            context.Ceilings = CreateChild(context.Environment, "Ceilings");
            context.Doors = CreateChild(context.Environment, "Doors");
            context.Stairs = CreateChild(context.Environment, "Stairs");
            context.Railings = CreateChild(context.Environment, "Railings");
            context.DarkHallway = CreateChild(context.Environment, "DarkHallway");
            context.Rooftop = CreateChild(context.Environment, "Rooftop");
            context.RestaurantExterior = CreateChild(context.Environment, "RestaurantExterior");
            context.Rooms = CreateChild(context.Root, "Rooms");
            context.DebugLabels = CreateChild(context.Root, "DebugLabels");
            context.SpawnPoints = CreateChild(context.Root, "SpawnPoints");
            context.ObjectiveMarkers = CreateChild(context.Root, "ObjectiveMarkers");
            context.NavigationMarkers = CreateChild(context.Root, "NavigationMarkers");
            context.MapGuide = CreateChild(context.Root, "MapGuide");
            context.Lighting = CreateChild(context.Root, "Lighting");
            context.PlayerSetup = CreateChild(context.Root, "PlayerSetup");
            context.Managers = CreateChild(context.Root, "Managers");
            return context;
        }

        private static void CreateMaterials(BuildContext context)
        {
            AddMaterial(context, "Floor", "MAT_Graybox_Floor", new Color(0.45f, 0.45f, 0.48f));
            AddMaterial(context, "Wall", "MAT_Graybox_Wall", new Color(0.7f, 0.72f, 0.74f));
            AddMaterial(context, "NamRoom", "MAT_Graybox_NamRoom", new Color(0.18f, 0.45f, 0.75f));
            AddMaterial(context, "MinhRoom", "MAT_Graybox_MinhRoom", new Color(0.18f, 0.65f, 0.45f));
            AddMaterial(context, "ComputerRoom", "MAT_Graybox_ComputerRoom", new Color(0.45f, 0.33f, 0.76f));
            AddMaterial(context, "Storage", "MAT_Graybox_Storage", new Color(0.75f, 0.58f, 0.2f));
            AddMaterial(context, "Restroom", "MAT_Graybox_Restroom", new Color(0.24f, 0.68f, 0.78f));
            AddMaterial(context, "MainHallway", "MAT_Graybox_MainHallway", new Color(0.58f, 0.58f, 0.62f));
            AddMaterial(context, "DarkHallway", "MAT_Graybox_DarkHallway", new Color(0.12f, 0.13f, 0.18f));
            AddMaterial(context, "Staircase", "MAT_Graybox_Staircase", new Color(0.6f, 0.46f, 0.32f));
            AddMaterial(context, "Rooftop", "MAT_Graybox_Rooftop", new Color(0.42f, 0.5f, 0.58f));
            AddMaterial(context, "Restaurant", "MAT_Graybox_Restaurant", new Color(0.72f, 0.36f, 0.26f));
            AddMaterial(context, "Objective", "MAT_Graybox_Objective", new Color(1f, 0.82f, 0.12f));
            AddMaterial(context, "Spawn", "MAT_Graybox_Spawn", new Color(0.1f, 0.9f, 0.35f));
            AddMaterial(context, "Door", "MAT_Graybox_DoorDebug", new Color(0.2f, 0.2f, 0.24f, 0.45f));
        }

        private static void AddMaterial(BuildContext context, string key, string fileName, Color color)
        {
            string path = $"{MaterialFolderPath}/{fileName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindCompatibleShader());
                AssetDatabase.CreateAsset(material, path);
            }

            ApplyColor(material, color);
            context.Materials[key] = material;
        }

        private static Shader FindCompatibleShader()
        {
            Material prototypeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Chapter1/Materials/M_Ground_Prototype.mat");
            if (prototypeMaterial != null && prototypeMaterial.shader != null)
            {
                return prototypeMaterial.shader;
            }

            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
        }

        private static void BuildDormitoryLayout(BuildContext context)
        {
            CreateAreaRoots(context);
            CreateMainFloor(context);
            CreateRooms(context);
            CreateDarkHallwayAndStorage(context);
            CreateStaircaseAndRooftop(context);
            CreateRestaurantExterior(context);
            CreateSpawnPoints(context);
            CreateObjectiveMarkers(context);
            CreateNavigationMarkers(context);
            CreateMapGuide(context);
        }

        private static void CreateAreaRoots(BuildContext context)
        {
            for (int i = 0; i < RequiredAreas.Length; i++)
            {
                CreateChild(context.Rooms, RequiredAreas[i]);
            }
        }

        private static void CreateMainFloor(BuildContext context)
        {
            CreateCube(context.Floors, "Floor_MainHallway", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 4f), context.Materials["MainHallway"], true);
            CreateWall(context.Walls, "Wall_MainHallway_SouthWest_End", new Vector3(-14.4f, 1.5f, -2.1f), new Vector3(1.2f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, "Wall_MainHallway_SouthWest", new Vector3(-6.9f, 1.5f, -2.1f), new Vector3(9.8f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, "Wall_MainHallway_SouthEast", new Vector3(8.5f, 1.5f, -2.1f), new Vector3(13f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, "Wall_MainHallway_WestEnd", new Vector3(-15f, 1.5f, 0f), new Vector3(0.25f, 3f, 4f), context.Materials["Wall"]);
            CreateDebugLabel(context, "Label_MainHallway", "MAIN HALLWAY", new Vector3(0f, 2.6f, 0f));
        }

        private static void CreateRooms(BuildContext context)
        {
            CreateNorthRoom(context, "Room_Nam", "NamRoom", "NAM ROOM", new Vector3(-10f, 0f, 5f), new Vector2(5f, 6f));
            CreateNorthRoom(context, "Room_Minh", "MinhRoom", "MINH ROOM", new Vector3(-4f, 0f, 5f), new Vector2(5f, 6f));
            CreateNorthRoom(context, "ComputerRoom", "ComputerRoom", "COMPUTER ROOM", new Vector3(3.5f, 0f, 6f), new Vector2(7f, 8f));
            CreateNorthRoom(context, "Restroom", "Restroom", "RESTROOM", new Vector3(10.5f, 0f, 4.5f), new Vector2(4f, 5f));
        }

        private static void CreateNorthRoom(BuildContext context, string roomName, string materialKey, string label, Vector3 center, Vector2 size)
        {
            CreateCube(context.Floors, $"Floor_{roomName}", new Vector3(center.x, -0.1f, center.z), new Vector3(size.x, 0.2f, size.y), context.Materials[materialKey], true);
            CreateWall(context.Walls, $"Wall_{roomName}_North", new Vector3(center.x, 1.5f, center.z + size.y * 0.5f), new Vector3(size.x, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, $"Wall_{roomName}_West", new Vector3(center.x - size.x * 0.5f, 1.5f, center.z), new Vector3(0.25f, 3f, size.y), context.Materials["Wall"]);
            CreateWall(context.Walls, $"Wall_{roomName}_East", new Vector3(center.x + size.x * 0.5f, 1.5f, center.z), new Vector3(0.25f, 3f, size.y), context.Materials["Wall"]);
            CreateWallWithDoorAlongX(context.Walls, $"Wall_{roomName}_South", center.x, center.z - size.y * 0.5f, size.x, center.x, 1.6f, context.Materials["Wall"]);
            CreateCube(context.Ceilings, $"Ceiling_{roomName}", new Vector3(center.x, 3.1f, center.z), new Vector3(size.x, 0.1f, size.y), context.Materials["Wall"], true);
            CreateDoorDebug(context.Doors, $"Door_{roomName}", new Vector3(center.x, 1.1f, center.z - size.y * 0.5f - 0.02f), new Vector3(1.4f, 2.2f, 0.08f));
            CreateDebugLabel(context, $"Label_{roomName}", label, new Vector3(center.x, 2.55f, center.z));
        }

        private static void CreateDarkHallwayAndStorage(BuildContext context)
        {
            CreateCube(context.Floors, "Floor_DarkHallway", new Vector3(18f, -0.1f, 0f), new Vector3(10f, 0.2f, 3f), context.Materials["DarkHallway"], true);
            CreateWall(context.DarkHallway, "Wall_DarkHallway_North", new Vector3(18f, 1.5f, 1.65f), new Vector3(10f, 3f, 0.25f), context.Materials["DarkHallway"]);
            CreateWall(context.DarkHallway, "Wall_DarkHallway_South", new Vector3(18f, 1.5f, -1.65f), new Vector3(10f, 3f, 0.25f), context.Materials["DarkHallway"]);
            CreateDebugLabel(context, "Label_DarkHallway", "DARK HALLWAY", new Vector3(18f, 2.4f, 0f));

            CreateCube(context.Floors, "Floor_EquipmentStorage", new Vector3(25f, -0.1f, 0f), new Vector3(5f, 0.2f, 5f), context.Materials["Storage"], true);
            CreateWall(context.Walls, "Wall_EquipmentStorage_North", new Vector3(25f, 1.5f, 2.5f), new Vector3(5f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, "Wall_EquipmentStorage_South", new Vector3(25f, 1.5f, -2.5f), new Vector3(5f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Walls, "Wall_EquipmentStorage_East", new Vector3(27.5f, 1.5f, 0f), new Vector3(0.25f, 3f, 5f), context.Materials["Wall"]);
            CreateWallWithDoorAlongZ(context.Walls, "Wall_EquipmentStorage_West", 22.5f, 0f, 5f, 0f, 1.6f, context.Materials["Wall"]);
            CreateDoorDebug(context.Doors, "Door_EquipmentStorage", new Vector3(22.48f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 1.4f));
            CreateDebugLabel(context, "Label_EquipmentStorage", "EQUIPMENT STORAGE", new Vector3(25f, 2.5f, 0f));

            CreatePointLight(context.DarkHallway, "Light_DarkHallway_01", new Vector3(16.5f, 2.4f, 0f), new Color(0.35f, 0.42f, 0.62f), 0.8f, 7f);
            CreatePointLight(context.DarkHallway, "Light_DarkHallway_02", new Vector3(20.5f, 2.4f, 0f), new Color(0.35f, 0.42f, 0.62f), 0.6f, 6f);
        }

        private static void CreateStaircaseAndRooftop(BuildContext context)
        {
            CreateCube(context.Floors, "Floor_StairLanding", new Vector3(-12.8f, -0.1f, -3.6f), new Vector3(4.4f, 0.2f, 3f), context.Materials["Staircase"], true);
            GameObject ramp = CreateCube(context.Stairs, "Staircase_Ramp", new Vector3(-12.8f, 2.05f, -6.65f), new Vector3(3f, 0.3f, 6.4f), context.Materials["Staircase"], true);
            ramp.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            CreateWall(context.Railings, "Railing_Staircase_Left", new Vector3(-14.6f, 2.1f, -6.65f), new Vector3(0.18f, 1.2f, 6.8f), context.Materials["Wall"]);
            CreateWall(context.Railings, "Railing_Staircase_Right", new Vector3(-11f, 2.1f, -6.65f), new Vector3(0.18f, 1.2f, 6.8f), context.Materials["Wall"]);
            CreateDebugLabel(context, "Label_Staircase", "STAIRCASE", new Vector3(-12.8f, 2.8f, -3.8f));

            CreateCube(context.Rooftop, "Floor_Rooftop", new Vector3(-15f, 4.1f, -14f), new Vector3(12f, 0.2f, 10f), context.Materials["Rooftop"], true);
            CreateWall(context.Rooftop, "Railing_Rooftop_North_West", new Vector3(-17.55f, 4.8f, -9f), new Vector3(6.9f, 1.2f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Rooftop, "Railing_Rooftop_North_East", new Vector3(-10.3f, 4.8f, -9f), new Vector3(2.6f, 1.2f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Rooftop, "Railing_Rooftop_South", new Vector3(-15f, 4.8f, -19f), new Vector3(12f, 1.2f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.Rooftop, "Railing_Rooftop_West", new Vector3(-21f, 4.8f, -14f), new Vector3(0.25f, 1.2f, 10f), context.Materials["Wall"]);
            CreateWall(context.Rooftop, "Railing_Rooftop_East", new Vector3(-9f, 4.8f, -14f), new Vector3(0.25f, 1.2f, 10f), context.Materials["Wall"]);
            CreateDebugLabel(context, "Label_Rooftop", "ROOFTOP", new Vector3(-15f, 5.3f, -14f));
        }

        private static void CreateRestaurantExterior(BuildContext context)
        {
            CreateCube(context.Floors, "Floor_DormitoryEntrance", new Vector3(0f, -0.1f, -2.6f), new Vector3(4.2f, 0.2f, 1.2f), context.Materials["Floor"], true);
            CreateCube(context.Floors, "Floor_CourtyardRoad", new Vector3(0f, -0.1f, -12f), new Vector3(14f, 0.2f, 18f), context.Materials["Floor"], true);
            CreateCube(context.RestaurantExterior, "Floor_Restaurant_Opposite", new Vector3(0f, -0.1f, -25f), new Vector3(12f, 0.2f, 8f), context.Materials["Restaurant"], true);
            CreateWall(context.RestaurantExterior, "Wall_Restaurant_Back", new Vector3(0f, 1.5f, -29f), new Vector3(12f, 3f, 0.25f), context.Materials["Wall"]);
            CreateWall(context.RestaurantExterior, "Wall_Restaurant_Left", new Vector3(-6f, 1.5f, -25f), new Vector3(0.25f, 3f, 8f), context.Materials["Wall"]);
            CreateWall(context.RestaurantExterior, "Wall_Restaurant_Right", new Vector3(6f, 1.5f, -25f), new Vector3(0.25f, 3f, 8f), context.Materials["Wall"]);
            CreateWallWithDoorAlongX(context.RestaurantExterior, "Wall_Restaurant_Front", 0f, -21f, 12f, 0f, 2.4f, context.Materials["Wall"]);
            CreateDebugLabel(context, "Label_Restaurant", "RESTAURANT", new Vector3(0f, 2.8f, -25f));
            CreateDebugLabel(context, "Label_RestaurantSign", "QUAN AN DOI DIEN", new Vector3(0f, 2.6f, -21.3f));
        }

        private static void CreateSpawnPoints(BuildContext context)
        {
            CreateSpawn(context, "Spawn_ChapterStart", new Vector3(-10f, 0.05f, 3.4f), 180f);
            CreateSpawn(context, "Spawn_RoomNam", new Vector3(-10f, 0.05f, 5f), 180f);
            CreateSpawn(context, "Spawn_RoomMinh", new Vector3(-4f, 0.05f, 5f), 180f);
            CreateSpawn(context, "Spawn_ComputerRoom", new Vector3(3.5f, 0.05f, 6f), 180f);
            CreateSpawn(context, "Spawn_EquipmentStorage", new Vector3(25f, 0.05f, 0f), -90f);
            CreateSpawn(context, "Spawn_MainHallway", new Vector3(-7f, 0.05f, 0f), 90f);
            CreateSpawn(context, "Spawn_DarkHallway", new Vector3(18f, 0.05f, 0f), 90f);
            CreateSpawn(context, "Spawn_Staircase", new Vector3(-12.8f, 0.05f, -3.6f), 180f);
            CreateSpawn(context, "Spawn_Rooftop", new Vector3(-15f, 4.35f, -14f), 0f);
            CreateSpawn(context, "Spawn_Restaurant", new Vector3(0f, 0.05f, -24f), 0f);
        }

        private static void CreateObjectiveMarkers(BuildContext context)
        {
            CreateObjective(context, "Objective_LeaveNamRoom", "Leave Nam Room", "Door from Nam Room to Main Hallway", new Vector3(-10f, 0.05f, 2.2f));
            CreateObjective(context, "Objective_EnterMainHallway", "Enter Main Hallway", "Main center corridor", new Vector3(-8f, 0.05f, 0f));
            CreateObjective(context, "Objective_ComputerRoom", "Computer Room", "Computer room objective placeholder", new Vector3(3.5f, 0.05f, 4f));
            CreateObjective(context, "Objective_DarkHallwayEntry", "Dark Hallway Entry", "Entry from main hallway to dark hallway", new Vector3(13.5f, 0.05f, 0f));
            CreateObjective(context, "Objective_EquipmentStorage", "Equipment Storage", "Storage endpoint for Chapter 1 graybox route", new Vector3(24f, 0.05f, 0f));
            CreateObjective(context, "Objective_Staircase", "Staircase", "Route to rooftop", new Vector3(-12.8f, 0.05f, -3.6f));
            CreateObjective(context, "Objective_Rooftop", "Rooftop", "Rooftop objective placeholder", new Vector3(-15f, 4.35f, -14f));
            CreateObjective(context, "Objective_Restaurant", "Restaurant", "Opposite restaurant objective placeholder", new Vector3(0f, 0.05f, -23f));
        }

        private static void CreateNavigationMarkers(BuildContext context)
        {
            CreateDebugLabel(context, "Route_Nam_To_MainHallway", "ROUTE: NAM -> MAIN HALLWAY", new Vector3(-9f, 2.25f, 1.1f));
            CreateDebugLabel(context, "Route_Main_To_DarkHallway", "ROUTE: MAIN -> DARK HALLWAY", new Vector3(12f, 2.25f, 0f));
            CreateDebugLabel(context, "Route_Dark_To_Storage", "ROUTE: DARK -> STORAGE", new Vector3(22f, 2.25f, 0f));
        }

        private static void CreateMapGuide(BuildContext context)
        {
            GameObject board = CreateCube(context.MapGuide, "DormitoryMapGuide_Board", new Vector3(-12.2f, 1.5f, 6.9f), new Vector3(0.1f, 2.2f, 2.6f), context.Materials["Wall"], false);
            RemoveCollider(board);
            GameObject guide = new GameObject("DormitoryMapGuide");
            guide.transform.SetParent(context.MapGuide, false);
            guide.transform.position = new Vector3(-12.12f, 1.55f, 6.9f);
            guide.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            CreateWorldText(
                guide,
                "DORMITORY MAP\n\nNam Room\nMain Hallway\nComputer Room\nRestroom\nDark Hallway\nEquipment Storage\nStaircase\nRooftop\nRestaurant",
                0.18f,
                new Vector2(2.2f, 2.2f));
        }

        private static GameObject CreatePlayerSetup(BuildContext context)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject player = PrefabUtility.InstantiatePrefab(prefab, context.Scene) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Failed to instantiate player prefab.");
            }

            player.name = "Player";
            player.transform.SetParent(context.PlayerSetup, true);
            player.transform.position = new Vector3(-10f, 0.05f, 3.4f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            TrySetTag(player, "Player");
            SetLayerRecursively(player, LayerMask.NameToLayer("Player"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            return player;
        }

        private static GameObject CreateCameraRig(BuildContext context, GameObject player)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath);
            GameObject cameraRig = PrefabUtility.InstantiatePrefab(prefab, context.Scene) as GameObject;
            if (cameraRig == null)
            {
                throw new InvalidOperationException("Failed to instantiate camera rig prefab.");
            }

            cameraRig.name = "CameraRig";
            cameraRig.transform.SetParent(context.PlayerSetup, true);
            cameraRig.transform.position = player.transform.position + new Vector3(0f, 1.4f, -4f);

            ThirdPersonCameraRig rig = cameraRig.GetComponent<ThirdPersonCameraRig>();
            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
            CameraTarget cameraTarget = player.GetComponentInChildren<CameraTarget>(true);
            Camera camera = cameraRig.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                TrySetTag(camera.gameObject, "MainCamera");
            }

            SetSerializedObjectReference(rig, "target", cameraTarget != null ? cameraTarget.transform : player.transform);
            SetSerializedObjectReference(rig, "inputReader", inputReader);
            SetSerializedObjectReference(rig, "inputLock", inputLock);
            SetSerializedObjectReference(rig, "controlledCamera", camera);
            SetSerializedInt(rig, "collisionMask", LayerMask.GetMask("Environment"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(cameraRig);
            return cameraRig;
        }

        private static void CopyManagersFromPrototype(BuildContext context, GameObject player, GameObject cameraRig)
        {
            Scene prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
            try
            {
                CopyManagerObject<Chapter1Manager>(prototypeScene, context);
                CopyManagerObject<Chapter1GameplayBootstrap>(prototypeScene, context);
                CopyManagerObject<PlayerDebugOverlay>(prototypeScene, context);
            }
            finally
            {
                if (prototypeScene.IsValid())
                {
                    EditorSceneManager.CloseScene(prototypeScene, true);
                }
            }

            LinkCopiedManagers(context, player, cameraRig);
        }

        private static void CopyManagerObject<T>(Scene prototypeScene, BuildContext context) where T : Component
        {
            T source = GetFirstSceneComponent<T>(prototypeScene);
            if (source == null)
            {
                Debug.LogWarning($"[Dormitory Graybox] Prototype scene is missing {typeof(T).Name}.");
                return;
            }

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = source.gameObject.name;
            SceneManager.MoveGameObjectToScene(clone, context.Scene);
            clone.transform.SetParent(context.Managers, true);
        }

        private static void LinkCopiedManagers(BuildContext context, GameObject player, GameObject cameraRig)
        {
            Chapter1Manager manager = GetFirstSceneComponent<Chapter1Manager>(context.Scene);
            Chapter1GameplayBootstrap bootstrap = GetFirstSceneComponent<Chapter1GameplayBootstrap>(context.Scene);
            PlayerDebugOverlay debugOverlay = GetFirstSceneComponent<PlayerDebugOverlay>(context.Scene);
            ThirdPersonCameraRig rig = cameraRig.GetComponent<ThirdPersonCameraRig>();
            Camera camera = cameraRig.GetComponentInChildren<Camera>(true);
            Chapter1InputReader inputReader = player.GetComponent<Chapter1InputReader>();
            PlayerInputLock inputLock = player.GetComponent<PlayerInputLock>();
            CameraTarget cameraTarget = player.GetComponentInChildren<CameraTarget>(true);

            SetSerializedObjectReference(bootstrap, "chapter1Manager", manager);
            SetSerializedObjectReference(bootstrap, "player", player.transform);
            SetSerializedObjectReference(bootstrap, "inputReader", inputReader);
            SetSerializedObjectReference(bootstrap, "cameraRig", rig);
            SetSerializedObjectReference(bootstrap, "playerInputLock", inputLock);
            SetSerializedObjectReference(bootstrap, "cameraTarget", cameraTarget);
            SetSerializedObjectReference(bootstrap, "playerInventory", player.GetComponent<PlayerInventory>());
            SetSerializedObjectReference(bootstrap, "interactionController", player.GetComponent<Chapter1InteractionController>());
            SetSerializedObjectReference(bootstrap, "flashlightController", player.GetComponent<FlashlightController>());
            SetSerializedObjectReference(bootstrap, "chapter1HUD", null);
            SetSerializedObjectReference(bootstrap, "gameplayCamera", camera);

            SetSerializedObjectReference(debugOverlay, "inputReader", inputReader);
            SetSerializedObjectReference(debugOverlay, "playerMotor", player.GetComponent<Chapter1PlayerMotor>());
            SetSerializedObjectReference(debugOverlay, "playerStamina", player.GetComponent<PlayerStamina>());
            SetSerializedObjectReference(debugOverlay, "inputLock", inputLock);
            SetSerializedObjectReference(debugOverlay, "cameraRig", rig);
            SetSerializedObjectReference(debugOverlay, "interactionController", player.GetComponent<Chapter1InteractionController>());
            SetSerializedObjectReference(debugOverlay, "gameplayCamera", camera);
        }

        private static void NormalizeGameplayCameraAndAudio(Scene scene, Camera gameplayCamera)
        {
            if (gameplayCamera == null)
            {
                Debug.LogWarning("[Dormitory Graybox] No gameplay camera found while normalizing camera/audio.");
                return;
            }

            TrySetTag(gameplayCamera.gameObject, "MainCamera");
            gameplayCamera.enabled = true;
            AudioListener gameplayListener = gameplayCamera.GetComponent<AudioListener>();
            if (gameplayListener == null)
            {
                gameplayListener = gameplayCamera.gameObject.AddComponent<AudioListener>();
            }

            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                if (camera == null)
                {
                    continue;
                }

                if (camera != gameplayCamera && camera.CompareTag("MainCamera"))
                {
                    TrySetTag(camera.gameObject, "Untagged");
                }
            }

            List<AudioListener> listeners = GetSceneComponents<AudioListener>(scene);
            for (int i = 0; i < listeners.Count; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null)
                {
                    continue;
                }

                listener.enabled = listener == gameplayListener;
                EditorUtility.SetDirty(listener);
            }

            EditorUtility.SetDirty(gameplayCamera);
            EditorUtility.SetDirty(gameplayCamera.gameObject);
        }

        private static void CreateLighting(BuildContext context)
        {
            GameObject sun = new GameObject("DirectionalLight_Dormitory");
            sun.transform.SetParent(context.Lighting, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 1.1f;

            CreatePointLight(context.Lighting, "Light_MainHallway_01", new Vector3(-7f, 2.6f, 0f), Color.white, 1.5f, 9f);
            CreatePointLight(context.Lighting, "Light_MainHallway_02", new Vector3(2f, 2.6f, 0f), Color.white, 1.5f, 9f);
            CreatePointLight(context.Lighting, "Light_MainHallway_03", new Vector3(10f, 2.6f, 0f), Color.white, 1.4f, 8f);
            RenderSettings.ambientLight = new Color(0.35f, 0.36f, 0.38f);
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("Environment"));
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                RemoveCollider(gameObject);
            }

            return gameObject;
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            CreateCube(parent, name, position, scale, material, true);
        }

        private static void CreateWallWithDoorAlongX(Transform parent, string name, float centerX, float z, float totalWidth, float doorCenterX, float doorWidth, Material material)
        {
            float leftEnd = doorCenterX - doorWidth * 0.5f;
            float rightStart = doorCenterX + doorWidth * 0.5f;
            float min = centerX - totalWidth * 0.5f;
            float max = centerX + totalWidth * 0.5f;
            float leftWidth = Mathf.Max(0f, leftEnd - min);
            float rightWidth = Mathf.Max(0f, max - rightStart);

            if (leftWidth > 0.05f)
            {
                CreateWall(parent, name + "_Left", new Vector3(min + leftWidth * 0.5f, 1.5f, z), new Vector3(leftWidth, 3f, 0.25f), material);
            }

            if (rightWidth > 0.05f)
            {
                CreateWall(parent, name + "_Right", new Vector3(rightStart + rightWidth * 0.5f, 1.5f, z), new Vector3(rightWidth, 3f, 0.25f), material);
            }
        }

        private static void CreateWallWithDoorAlongZ(Transform parent, string name, float x, float centerZ, float totalDepth, float doorCenterZ, float doorWidth, Material material)
        {
            float nearEnd = doorCenterZ - doorWidth * 0.5f;
            float farStart = doorCenterZ + doorWidth * 0.5f;
            float min = centerZ - totalDepth * 0.5f;
            float max = centerZ + totalDepth * 0.5f;
            float nearDepth = Mathf.Max(0f, nearEnd - min);
            float farDepth = Mathf.Max(0f, max - farStart);

            if (nearDepth > 0.05f)
            {
                CreateWall(parent, name + "_Near", new Vector3(x, 1.5f, min + nearDepth * 0.5f), new Vector3(0.25f, 3f, nearDepth), material);
            }

            if (farDepth > 0.05f)
            {
                CreateWall(parent, name + "_Far", new Vector3(x, 1.5f, farStart + farDepth * 0.5f), new Vector3(0.25f, 3f, farDepth), material);
            }
        }

        private static void CreateDoorDebug(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject door = CreateCube(parent, name, position, scale, FindMaterial("Door"), false);
            door.layer = 0;
        }

        private static Material FindMaterial(string key)
        {
            string path = $"{MaterialFolderPath}/MAT_Graybox_{key}Debug.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            return material != null ? material : AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolderPath}/MAT_Graybox_Wall.mat");
        }

        private static void CreateDebugLabel(BuildContext context, string name, string text, Vector3 position)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(context.DebugLabels, false);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            CreateWorldText(label, text, 0.35f, new Vector2(5f, 1f));
        }

        private static void CreateWorldText(GameObject target, string text, float fontSize, Vector2 size)
        {
            TMP_FontAsset fontAsset = GetTextMeshProFontAsset();
            if (fontAsset != null)
            {
                TextMeshPro textMesh = target.AddComponent<TextMeshPro>();
                textMesh.font = fontAsset;
                textMesh.text = text;
                textMesh.fontSize = fontSize;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.color = Color.white;
                textMesh.textWrappingMode = TextWrappingModes.Normal;
                textMesh.raycastTarget = false;
                textMesh.rectTransform.sizeDelta = size;
                return;
            }

            TextMesh fallback = target.AddComponent<TextMesh>();
            fallback.text = text;
            fallback.fontSize = 48;
            fallback.characterSize = Mathf.Max(0.01f, fontSize * 0.08f);
            fallback.anchor = TextAnchor.MiddleCenter;
            fallback.alignment = TextAlignment.Center;
            fallback.color = Color.white;
        }

        private static TMP_FontAsset GetTextMeshProFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpDefaultFontPath);
            if (fontAsset != null)
            {
                return fontAsset;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static void CreateSpawn(BuildContext context, string name, Vector3 position, float yaw)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(context.SpawnPoints, false);
            spawn.transform.position = position;
            spawn.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject visual = CreateCube(spawn.transform, "SpawnMarkerVisual", position + Vector3.up * 0.25f, new Vector3(0.35f, 0.5f, 0.35f), context.Materials["Spawn"], false);
            visual.transform.localPosition = Vector3.up * 0.25f;
            visual.layer = 0;
        }

        private static void CreateObjective(BuildContext context, string markerId, string displayName, string note, Vector3 position)
        {
            GameObject marker = new GameObject(markerId);
            marker.transform.SetParent(context.ObjectiveMarkers, false);
            marker.transform.position = position;
            Chapter1ObjectiveMarker objectiveMarker = marker.AddComponent<Chapter1ObjectiveMarker>();
            objectiveMarker.Configure(markerId, displayName, note, 1.5f);
            GameObject visual = CreateCube(marker.transform, "ObjectiveMarkerVisual", position + Vector3.up * 0.25f, new Vector3(0.4f, 0.4f, 0.4f), context.Materials["Objective"], false);
            visual.transform.localPosition = Vector3.up * 0.25f;
            visual.layer = 0;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
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

        private static void TrySetTag(GameObject gameObject, string tag)
        {
            try
            {
                gameObject.tag = tag;
            }
            catch (UnityException exception)
            {
                Debug.LogWarning($"[Dormitory Graybox] Could not set tag {tag} on {gameObject.name}: {exception.Message}");
            }
        }

        private static void SetSerializedObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerializedInt(UnityEngine.Object target, string propertyName, int value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void CheckSceneMaterials(ref int passed, ref int failed)
        {
            for (int i = 0; i < GrayboxMaterialFileNames.Length; i++)
            {
                string path = $"{MaterialFolderPath}/{GrayboxMaterialFileNames[i]}";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Check(material != null && material.shader != null && !IsErrorShader(material.shader), $"Graybox material valid: {GrayboxMaterialFileNames[i]}", ref passed, ref failed);
            }
        }

        private static void CheckRequiredDirectChildren(Transform parent, string[] childNames, string groupName, ref int passed, ref int failed)
        {
            for (int i = 0; i < childNames.Length; i++)
            {
                Transform child = parent.Find(childNames[i]);
                Check(child != null, $"{groupName} has {childNames[i]}", ref passed, ref failed);
            }
        }

        private static void CheckRequiredArea(Scene scene, string areaName, ref int passed, ref int failed)
        {
            GameObject area = FindSceneObject(scene, areaName);
            Check(area != null, $"{areaName} found", ref passed, ref failed);
            if (area == null)
            {
                return;
            }

            switch (areaName)
            {
                case "Room_Nam":
                    CheckNamedBoxCollider(scene, "Floor_Room_Nam", "Room_Nam has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Room_Nam", "Room_Nam debug label has text", ref passed, ref failed);
                    CheckNoCollider(scene, "Door_Room_Nam", "Room_Nam door debug is non-blocking", ref passed, ref failed);
                    break;
                case "Room_Minh":
                    CheckNamedBoxCollider(scene, "Floor_Room_Minh", "Room_Minh has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Room_Minh", "Room_Minh debug label has text", ref passed, ref failed);
                    CheckNoCollider(scene, "Door_Room_Minh", "Room_Minh door debug is non-blocking", ref passed, ref failed);
                    break;
                case "ComputerRoom":
                    CheckNamedBoxCollider(scene, "Floor_ComputerRoom", "ComputerRoom has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_ComputerRoom", "ComputerRoom debug label has text", ref passed, ref failed);
                    CheckNoCollider(scene, "Door_ComputerRoom", "ComputerRoom door debug is non-blocking", ref passed, ref failed);
                    break;
                case "EquipmentStorage":
                    CheckNamedBoxCollider(scene, "Floor_EquipmentStorage", "EquipmentStorage has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_EquipmentStorage", "EquipmentStorage debug label has text", ref passed, ref failed);
                    CheckNoCollider(scene, "Door_EquipmentStorage", "EquipmentStorage door debug is non-blocking", ref passed, ref failed);
                    break;
                case "Restroom":
                    CheckNamedBoxCollider(scene, "Floor_Restroom", "Restroom has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Restroom", "Restroom debug label has text", ref passed, ref failed);
                    CheckNoCollider(scene, "Door_Restroom", "Restroom door debug is non-blocking", ref passed, ref failed);
                    break;
                case "MainHallway":
                    CheckNamedBoxCollider(scene, "Floor_MainHallway", "MainHallway has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_MainHallway", "MainHallway debug label has text", ref passed, ref failed);
                    break;
                case "DarkHallway":
                    CheckNamedBoxCollider(scene, "Floor_DarkHallway", "DarkHallway has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_DarkHallway", "DarkHallway debug label has text", ref passed, ref failed);
                    break;
                case "Staircase":
                    CheckNamedBoxCollider(scene, "Staircase_Ramp", "Staircase has ramp collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Staircase", "Staircase debug label has text", ref passed, ref failed);
                    break;
                case "Rooftop":
                    CheckNamedBoxCollider(scene, "Floor_Rooftop", "Rooftop has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Rooftop", "Rooftop debug label has text", ref passed, ref failed);
                    break;
                case "Restaurant_Opposite":
                    CheckNamedBoxCollider(scene, "Floor_Restaurant_Opposite", "Restaurant_Opposite has walkable floor collider", ref passed, ref failed);
                    CheckSceneObjectHasText(scene, "Label_Restaurant", "Restaurant debug label has text", ref passed, ref failed);
                    break;
            }
        }

        private static void CheckSpawnPoint(Scene scene, string spawnName, ref int passed, ref int failed)
        {
            GameObject spawn = FindSceneObject(scene, spawnName);
            Check(spawn != null, $"{spawnName} found", ref passed, ref failed);
            if (spawn == null)
            {
                return;
            }

            Check(CountSceneObjects(scene, spawnName) == 1, $"{spawnName} is unique", ref passed, ref failed);
            Check(spawn.GetComponentsInChildren<Collider>(true).Length == 0, $"{spawnName} has no collider", ref passed, ref failed);
            Vector3 position = spawn.transform.position;
            bool inReasonableBounds = position.x >= -25f && position.x <= 30f
                && position.y >= -0.05f && position.y <= 5.5f
                && position.z >= -32f && position.z <= 10f;
            Check(inReasonableBounds, $"{spawnName} is inside dormitory map bounds", ref passed, ref failed);
        }

        private static void CheckObjectiveMarker(Scene scene, string objectiveName, ref int passed, ref int failed)
        {
            GameObject markerObject = FindSceneObject(scene, objectiveName);
            Check(markerObject != null, $"{objectiveName} found", ref passed, ref failed);
            if (markerObject == null)
            {
                return;
            }

            Chapter1ObjectiveMarker marker = markerObject.GetComponent<Chapter1ObjectiveMarker>();
            Check(CountSceneObjects(scene, objectiveName) == 1, $"{objectiveName} is unique", ref passed, ref failed);
            Check(marker != null, $"{objectiveName} has Chapter1ObjectiveMarker", ref passed, ref failed);
            Check(marker != null && string.Equals(marker.MarkerId, objectiveName, StringComparison.Ordinal), $"{objectiveName} marker ID matches object name", ref passed, ref failed);
            Check(markerObject.GetComponentsInChildren<Collider>(true).Length == 0, $"{objectiveName} has no collider", ref passed, ref failed);
        }

        private static void CheckObjectiveMarkerIdsUnique(Scene scene, ref int passed, ref int failed)
        {
            HashSet<string> markerIds = new HashSet<string>(StringComparer.Ordinal);
            List<Chapter1ObjectiveMarker> markers = GetSceneComponents<Chapter1ObjectiveMarker>(scene);
            bool valid = true;
            for (int i = 0; i < markers.Count; i++)
            {
                Chapter1ObjectiveMarker marker = markers[i];
                if (marker == null || string.IsNullOrWhiteSpace(marker.MarkerId) || !markerIds.Add(marker.MarkerId))
                {
                    valid = false;
                    break;
                }
            }

            Check(valid && markers.Count == ObjectiveNames.Length, "Objective marker IDs are unique and complete", ref passed, ref failed);
        }

        private static void CheckRequiredRoute(Scene scene, ref int passed, ref int failed)
        {
            bool namToStorage =
                HasNamedBoxCollider(scene, "Floor_Room_Nam")
                && HasNamedBoxCollider(scene, "Floor_MainHallway")
                && HasNamedBoxCollider(scene, "Floor_DarkHallway")
                && HasNamedBoxCollider(scene, "Floor_EquipmentStorage")
                && ObjectHasNoCollider(scene, "Door_Room_Nam")
                && ObjectHasNoCollider(scene, "Door_EquipmentStorage");
            Check(namToStorage, "Route Room_Nam -> MainHallway -> DarkHallway -> EquipmentStorage has floors and non-blocking doors", ref passed, ref failed);

            bool secondaryRoutes =
                ObjectHasNoCollider(scene, "Door_ComputerRoom")
                && ObjectHasNoCollider(scene, "Door_Restroom")
                && HasNamedBoxCollider(scene, "Floor_StairLanding")
                && HasNamedBoxCollider(scene, "Staircase_Ramp")
                && HasNamedBoxCollider(scene, "Floor_Rooftop")
                && HasNamedBoxCollider(scene, "Floor_DormitoryEntrance")
                && HasNamedBoxCollider(scene, "Floor_CourtyardRoad")
                && HasNamedBoxCollider(scene, "Floor_Restaurant_Opposite");
            Check(secondaryRoutes, "Secondary routes to ComputerRoom, Restroom, Rooftop, and Restaurant have required floor/collider structure", ref passed, ref failed);
        }

        private static void CheckDarkHallwayLighting(Scene scene, ref int passed, ref int failed)
        {
            Light mainHallwayLight = FindSceneObject(scene, "Light_MainHallway_01")?.GetComponent<Light>();
            Light darkLightA = FindSceneObject(scene, "Light_DarkHallway_01")?.GetComponent<Light>();
            Light darkLightB = FindSceneObject(scene, "Light_DarkHallway_02")?.GetComponent<Light>();
            Check(mainHallwayLight != null, "Main hallway test light found", ref passed, ref failed);
            Check(darkLightA != null && darkLightB != null, "DarkHallway low test lights found", ref passed, ref failed);
            Check(mainHallwayLight != null && darkLightA != null && darkLightB != null && darkLightA.intensity < mainHallwayLight.intensity && darkLightB.intensity < mainHallwayLight.intensity, "DarkHallway lights are dimmer than MainHallway", ref passed, ref failed);
            Check(darkLightA != null && darkLightB != null && darkLightA.intensity > 0.05f && darkLightB.intensity > 0.05f, "DarkHallway is dim but still test-visible", ref passed, ref failed);
        }

        private static void CheckDebugLabels(Scene scene, ref int passed, ref int failed)
        {
            for (int i = 0; i < DebugLabelNames.Length; i++)
            {
                CheckSceneObjectHasText(scene, DebugLabelNames[i], $"{DebugLabelNames[i]} has visible text component", ref passed, ref failed);
                CheckNoCollider(scene, DebugLabelNames[i], $"{DebugLabelNames[i]} has no collider", ref passed, ref failed);
            }
        }

        private static void CheckMapGuide(Scene scene, ref int passed, ref int failed)
        {
            CheckSceneObjectHasText(scene, "DormitoryMapGuide", "DormitoryMapGuide has map text", ref passed, ref failed);
            CheckNoCollider(scene, "DormitoryMapGuide", "DormitoryMapGuide text has no collider", ref passed, ref failed);
            CheckNoCollider(scene, "DormitoryMapGuide_Board", "DormitoryMapGuide board does not block path", ref passed, ref failed);
        }

        private static void CheckNamedBoxCollider(Scene scene, string objectName, string message, ref int passed, ref int failed)
        {
            Check(HasNamedBoxCollider(scene, objectName), message, ref passed, ref failed);
        }

        private static bool HasNamedBoxCollider(Scene scene, string objectName)
        {
            GameObject gameObject = FindSceneObject(scene, objectName);
            if (gameObject == null)
            {
                return false;
            }

            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            return collider != null && collider.enabled && !collider.isTrigger;
        }

        private static void CheckNoCollider(Scene scene, string objectName, string message, ref int passed, ref int failed)
        {
            Check(ObjectHasNoCollider(scene, objectName), message, ref passed, ref failed);
        }

        private static bool ObjectHasNoCollider(Scene scene, string objectName)
        {
            GameObject gameObject = FindSceneObject(scene, objectName);
            return gameObject != null && gameObject.GetComponentsInChildren<Collider>(true).Length == 0;
        }

        private static void CheckSceneObjectHasText(Scene scene, string objectName, string message, ref int passed, ref int failed)
        {
            GameObject gameObject = FindSceneObject(scene, objectName);
            Check(gameObject != null && HasWorldText(gameObject), message, ref passed, ref failed);
        }

        private static bool HasWorldText(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            TMP_Text tmpText = gameObject.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                return !string.IsNullOrWhiteSpace(tmpText.text) && tmpText.font != null && tmpText.fontSize > 0f;
            }

            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            return textMesh != null && !string.IsNullOrWhiteSpace(textMesh.text) && textMesh.characterSize > 0f;
        }

        private static Transform GetCameraTargetTransform(GameObject player)
        {
            CameraTarget cameraTarget = player != null ? player.GetComponentInChildren<CameraTarget>(true) : null;
            return cameraTarget != null ? cameraTarget.transform : player != null ? player.transform : null;
        }

        private static bool HasSerializedObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object expected)
        {
            if (target == null || expected == null)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.objectReferenceValue == expected;
        }

        private static bool SerializedLayerMaskContains(UnityEngine.Object target, string propertyName, string layerName)
        {
            if (target == null)
            {
                return false;
            }

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && (property.intValue & (1 << layer)) != 0;
        }

        private static Camera GetMainGameplayCamera(Scene scene)
        {
            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy && camera.CompareTag("MainCamera"))
                {
                    return camera;
                }
            }

            return cameras.Count > 0 ? cameras[0] : null;
        }

        private static int CountActiveMainCameras(Scene scene)
        {
            int count = 0;
            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy && camera.CompareTag("MainCamera"))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveAudioListeners(Scene scene)
        {
            int count = 0;
            List<AudioListener> listeners = GetSceneComponents<AudioListener>(scene);
            for (int i = 0; i < listeners.Count; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSceneComponents<T>(Scene scene) where T : Component
        {
            return GetSceneComponents<T>(scene).Count;
        }

        private static bool HasMissingScripts(Scene scene)
        {
            List<GameObject> gameObjects = GetSceneGameObjects(scene);
            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObjects[i]) > 0)
                {
                    Debug.LogError($"[FAIL] Missing Script on {GetHierarchyPath(gameObjects[i].transform)}");
                    return true;
                }
            }

            return false;
        }

        private static bool HasDefaultNamedObjects(Scene scene)
        {
            List<GameObject> gameObjects = GetSceneGameObjects(scene);
            for (int i = 0; i < gameObjects.Count; i++)
            {
                string objectName = gameObjects[i].name;
                for (int j = 0; j < DefaultObjectNames.Length; j++)
                {
                    string defaultName = DefaultObjectNames[j];
                    if (string.Equals(objectName, defaultName, StringComparison.Ordinal)
                        || objectName.StartsWith(defaultName + " (", StringComparison.Ordinal))
                    {
                        Debug.LogError($"[FAIL] Default object name found: {GetHierarchyPath(gameObjects[i].transform)}");
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasMissingOrInvalidRendererMaterials(Scene scene)
        {
            List<Renderer> renderers = GetSceneComponents<Renderer>(scene);
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    Debug.LogError($"[FAIL] Renderer has no material: {GetHierarchyPath(renderer.transform)}");
                    return true;
                }

                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null || material.shader == null || IsErrorShader(material.shader))
                    {
                        Debug.LogError($"[FAIL] Renderer material/shader invalid: {GetHierarchyPath(renderer.transform)}");
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TextMeshProEssentialsAreAvailable()
        {
            return GetTextMeshProFontAsset() != null;
        }

        private static bool AllTextMeshProLabelsHaveFonts(Scene scene)
        {
            List<TMP_Text> texts = GetSceneComponents<TMP_Text>(scene);
            for (int i = 0; i < texts.Count; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.font == null)
                {
                    Debug.LogError($"[FAIL] TMP text has missing font: {GetHierarchyPath(text.transform)}");
                    return false;
                }
            }

            return true;
        }

        private static bool IsErrorShader(Shader shader)
        {
            return shader == null || string.Equals(shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal);
        }

        private static void CheckEnvironmentColliders(Scene scene, string namePrefix, ref int passed, ref int failed)
        {
            List<Collider> colliders = GetSceneComponents<Collider>(scene);
            bool found = false;
            bool missing = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                found = true;
                if (!collider.enabled || collider.isTrigger || collider as BoxCollider == null)
                {
                    missing = true;
                }
            }

            Check(found && !missing, $"{namePrefix} objects have enabled BoxColliders", ref passed, ref failed);
        }

        private static bool IsPlayerInsideCollider(GameObject player)
        {
            if (player == null)
            {
                return true;
            }

            Vector3 position = player.transform.position + Vector3.up * 0.5f;
            List<Collider> colliders = GetSceneComponents<Collider>(player.scene);
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider != null
                    && collider.enabled
                    && !collider.isTrigger
                    && collider.gameObject.layer == LayerMask.NameToLayer("Environment")
                    && collider.bounds.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Check(bool condition, string message, ref int passed, ref int failed)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"[PASS] {message}");
            }
            else
            {
                failed++;
                Debug.LogError($"[FAIL] {message}");
            }
        }

        private static Scene GetLoadedScene(string assetPath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (string.Equals(scene.path, assetPath, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return default;
        }

        private static GameObject GetRootObject(Scene scene, string objectName)
        {
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                if (string.Equals(roots[i].name, objectName, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static int CountSceneObjects(Scene scene, string objectName)
        {
            int count = 0;
            List<GameObject> gameObjects = GetSceneGameObjects(scene);
            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (string.Equals(gameObjects[i].name, objectName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static T GetFirstSceneComponent<T>(Scene scene) where T : Component
        {
            List<T> components = GetSceneComponents<T>(scene);
            return components.Count > 0 ? components[0] : null;
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

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
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
    }
}
