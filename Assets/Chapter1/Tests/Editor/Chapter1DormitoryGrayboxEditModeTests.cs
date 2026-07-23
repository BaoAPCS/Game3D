using System;
using System.Collections.Generic;
using DormitoryMystery.Chapter1.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class Chapter1DormitoryGrayboxEditModeTests
    {
        private const string DormitoryScenePath = "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";

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

        [Test]
        public void DormitorySceneContainsRequiredGrayboxStructure()
        {
            Assert.NotNull(typeof(DormitoryGrayboxBuilder), "DormitoryGrayboxBuilder must compile.");
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(DormitoryScenePath), "Dormitory scene asset missing.");

            bool openedByTest = false;
            Scene scene = GetLoadedScene(DormitoryScenePath);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(DormitoryScenePath, OpenSceneMode.Additive);
                openedByTest = true;
            }

            try
            {
                Assert.IsTrue(scene.IsValid() && scene.isLoaded, "Dormitory scene did not open.");

                for (int i = 0; i < RequiredAreas.Length; i++)
                {
                    Assert.NotNull(FindSceneObject(scene, RequiredAreas[i]), $"{RequiredAreas[i]} missing.");
                }

                for (int i = 0; i < SpawnNames.Length; i++)
                {
                    GameObject spawn = FindSceneObject(scene, SpawnNames[i]);
                    Assert.NotNull(spawn, $"{SpawnNames[i]} missing.");
                    Assert.AreEqual(0, spawn.GetComponentsInChildren<Collider>(true).Length, $"{SpawnNames[i]} should not block the player.");
                }

                HashSet<string> markerIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < ObjectiveNames.Length; i++)
                {
                    GameObject objective = FindSceneObject(scene, ObjectiveNames[i]);
                    Assert.NotNull(objective, $"{ObjectiveNames[i]} missing.");
                    Chapter1ObjectiveMarker marker = objective.GetComponent<Chapter1ObjectiveMarker>();
                    Assert.NotNull(marker, $"{ObjectiveNames[i]} missing Chapter1ObjectiveMarker.");
                    Assert.AreEqual(ObjectiveNames[i], marker.MarkerId, $"{ObjectiveNames[i]} marker ID mismatch.");
                    Assert.IsTrue(markerIds.Add(marker.MarkerId), $"{ObjectiveNames[i]} marker ID duplicated.");
                    Assert.AreEqual(0, objective.GetComponentsInChildren<Collider>(true).Length, $"{ObjectiveNames[i]} should not block the player.");
                }

                Assert.IsTrue(HasEnabledBoxCollider(scene, "Floor_MainHallway"), "Main hallway floor collider missing.");
                Assert.IsTrue(HasEnabledBoxCollider(scene, "Wall_MainHallway_WestEnd"), "Main hallway wall collider missing.");
                Assert.IsTrue(HasEnabledBoxCollider(scene, "Staircase_Ramp"), "Staircase ramp collider missing.");
                Assert.IsFalse(HasMissingScripts(scene), "Scene contains Missing Script components.");
            }
            finally
            {
                if (openedByTest && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool HasEnabledBoxCollider(Scene scene, string objectName)
        {
            GameObject gameObject = FindSceneObject(scene, objectName);
            if (gameObject == null)
            {
                return false;
            }

            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            return collider != null && collider.enabled && !collider.isTrigger;
        }

        private static bool HasMissingScripts(Scene scene)
        {
            List<GameObject> objects = GetSceneGameObjects(scene);
            for (int i = 0; i < objects.Count; i++)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]) > 0)
                {
                    return true;
                }
            }

            return false;
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
    }
}
