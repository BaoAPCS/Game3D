using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Menu.Tests
{
    public sealed class MenuSceneEditModeTests
    {
        private const string BackgroundPath =
            "Assets/Menu/Sprites/background.png";
        private const string TitlePath =
            "Assets/Menu/Sprites/title.png";
        private const string FontPath =
            "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        [Test]
        public void MenuSceneIsFirstBuildSceneAndHasConfiguredBootstrap()
        {
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;

            Assert.GreaterOrEqual(scenes.Length, 1);
            Assert.IsTrue(scenes[0].enabled);
            Assert.AreEqual(
                GameSessionFlow.MenuScenePath,
                scenes[0].path);
            Assert.AreEqual(1f, GameSessionFlow.EndingMenuDelay);

            Scene scene = SceneManager.GetSceneByPath(
                GameSessionFlow.MenuScenePath);
            bool closeAfterTest = !scene.isLoaded;
            if (closeAfterTest)
            {
                scene = EditorSceneManager.OpenScene(
                    GameSessionFlow.MenuScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                MainMenuRuntimeBootstrap bootstrap =
                    FindSceneComponent<MainMenuRuntimeBootstrap>(scene);

                Assert.NotNull(bootstrap);

                SerializedObject serialized =
                    new SerializedObject(bootstrap);
                Assert.AreEqual(
                    BackgroundPath,
                    AssetDatabase.GetAssetPath(
                        serialized.FindProperty("backgroundTexture")
                            .objectReferenceValue));
                Assert.AreEqual(
                    TitlePath,
                    AssetDatabase.GetAssetPath(
                        serialized.FindProperty("titleTexture")
                            .objectReferenceValue));
                Assert.AreEqual(
                    FontPath,
                    AssetDatabase.GetAssetPath(
                        serialized.FindProperty("menuFont")
                            .objectReferenceValue));
            }
            finally
            {
                if (closeAfterTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
