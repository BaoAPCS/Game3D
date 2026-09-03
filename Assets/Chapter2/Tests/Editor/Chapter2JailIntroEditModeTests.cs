using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnit.Framework;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2JailIntroEditModeTests
    {
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";

        [Test]
        public void PoliceStationStartsWithOnlyGameplayCameraAndListener()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                GameObject desktop = FindSceneObject(
                    scene,
                    Chapter2JailIntroController
                        .EmbeddedOfficeCameraObjectName);
                GameObject bed = FindSceneObject(scene, "Bed_cam");
                GameObject main = FindSceneObject(scene, "Main Camera");

                Assert.NotNull(desktop);
                Assert.NotNull(bed);
                Assert.NotNull(main);
                Assert.IsFalse(desktop.GetComponent<Camera>().enabled);
                Assert.IsFalse(
                    desktop.GetComponent<AudioListener>().enabled);
                Assert.IsFalse(bed.GetComponent<Camera>().enabled);
                Assert.IsFalse(bed.GetComponent<AudioListener>().enabled);
                Assert.IsTrue(main.CompareTag("MainCamera"));
                Assert.IsTrue(main.GetComponent<Camera>().enabled);
                Assert.IsTrue(main.GetComponent<AudioListener>().enabled);

                Assert.AreEqual(1, CountEnabledComponents<Camera>(scene));
                Assert.AreEqual(
                    1,
                    CountEnabledComponents<AudioListener>(scene));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void RuntimeGuardTurnsImportedOfficeCameraBackOff()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                GameObject desktop = FindSceneObject(
                    scene,
                    Chapter2JailIntroController
                        .EmbeddedOfficeCameraObjectName);
                Assert.NotNull(desktop);

                Camera desktopCamera = desktop.GetComponent<Camera>();
                AudioListener desktopListener =
                    desktop.GetComponent<AudioListener>();
                desktopCamera.enabled = true;
                desktopListener.enabled = true;

                Chapter2JailIntroController
                    .DisableEmbeddedOfficeCamera(scene);

                Assert.IsFalse(desktopCamera.enabled);
                Assert.IsFalse(desktopListener.enabled);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void IntroShotHasReadableCinematicDuration()
        {
            Assert.That(
                Chapter2JailIntroController.ShotHoldDuration,
                Is.InRange(2f, 4f));
        }

        private static int CountEnabledComponents<T>(Scene scene)
            where T : Behaviour
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                T[] components = roots[rootIndex]
                    .GetComponentsInChildren<T>(true);
                for (int componentIndex = 0;
                     componentIndex < components.Length;
                     componentIndex++)
                {
                    if (components[componentIndex] != null &&
                        components[componentIndex].enabled)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms = roots[rootIndex]
                    .GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0;
                     childIndex < transforms.Length;
                     childIndex++)
                {
                    if (transforms[childIndex] != null &&
                        transforms[childIndex].name == objectName)
                    {
                        return transforms[childIndex].gameObject;
                    }
                }
            }

            return null;
        }
    }
}
