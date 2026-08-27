using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class PoliceOfficerArrestEditModeTests
    {
        private const string ControllerPath =
            "Assets/Chapter1/Resources/Police/Police_Auto.controller";
        private const string ChapterScenePath =
            "Assets/Chapter1/Scenes/Chapter1_Dormitory.unity";
        private const string PoliceControllerGuid =
            "003f1d478b5da264d8051dd9f54748aa";

        [Test]
        public void PoliceRunsFasterThanNamAndUsesApprovedCaptureDistance()
        {
            Assert.Greater(
                PoliceOfficerArrestController.PoliceRunSpeed,
                6f);
            Assert.AreEqual(
                1.2f,
                PoliceOfficerArrestController.CaptureDistance,
                0.001f);
        }

        [Test]
        public void PoliceControllerContainsDirectIdleAndRunStates()
        {
            Assert.IsTrue(File.Exists(ControllerPath), ControllerPath);
            string yaml = File.ReadAllText(ControllerPath);
            StringAssert.Contains("m_Name: Idle", yaml);
            StringAssert.Contains("m_Name: Run", yaml);
            StringAssert.Contains(
                "guid: 76161f6c46717ad4a8e46781cfb9df81",
                yaml);
            StringAssert.Contains(
                "guid: 8993dd87ffd53984da29bea407cbf890",
                yaml);
        }

        [Test]
        public void ChapterSceneKeepsPoliceInactiveAndDedicatedCameraOff()
        {
            Assert.IsTrue(File.Exists(ChapterScenePath), ChapterScenePath);
            string yaml = File.ReadAllText(ChapterScenePath);

            Assert.IsTrue(
                Regex.IsMatch(
                    yaml,
                    @"(?ms)propertyPath: m_Name\s+value: Police\s+" +
                    @"objectReference: \{fileID: 0\}.*?" +
                    @"propertyPath: m_IsActive\s+value: 0"),
                "The exact Police root must remain inactive before arrest.");
            StringAssert.Contains(
                $"guid: {PoliceControllerGuid}",
                yaml);

            Match cameraObject = Regex.Match(
                yaml,
                @"(?ms)^--- !u!1 &(?<id>\d+)\r?$\nGameObject:" +
                @"(?:(?!^--- !u!).)*?m_Name: Police_Camera\s+" +
                @"(?:(?!^--- !u!).)*(?=^--- !u!|\z)");
            Assert.IsTrue(cameraObject.Success);
            string gameObjectId = cameraObject.Groups["id"].Value;
            Assert.IsTrue(
                Regex.IsMatch(
                    yaml,
                    @"(?ms)^--- !u!20 &\d+\r?$\nCamera:" +
                    @"(?:(?!^--- !u!).)*?" +
                    $@"m_GameObject: \{{fileID: {gameObjectId}\}}" +
                    @"(?:(?!^--- !u!).)*?" +
                    @"m_Enabled: 0"),
                "Police_Camera must be serialized disabled.");
            Assert.IsTrue(
                Regex.IsMatch(
                    yaml,
                    @"(?ms)^--- !u!81 &\d+\r?$\nAudioListener:" +
                    @"(?:(?!^--- !u!).)*?" +
                    $@"m_GameObject: \{{fileID: {gameObjectId}\}}" +
                    @"(?:(?!^--- !u!).)*?" +
                    @"m_Enabled: 0"),
                "Police_Camera AudioListener must be serialized disabled.");
        }

        [Test]
        public void InstallerDisablesDedicatedCameraBeforePoliceSpawn()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject police = CreateRoot(scene, "Police");
                GameObject cameraObject = new GameObject("Police_Camera");
                cameraObject.transform.SetParent(police.transform, false);
                Camera dedicatedCamera =
                    cameraObject.AddComponent<Camera>();
                AudioListener dedicatedListener =
                    cameraObject.AddComponent<AudioListener>();

                GameObject car = CreateRoot(scene, "police_car");
                car.transform.position =
                    new Vector3(-80.01f, 0.01f, -9.56f);
                police.transform.position =
                    new Vector3(-79.21f, 0.01f, -8.72f);
                police.SetActive(false);
                car.SetActive(false);

                PoliceOfficerArrestController controller =
                    PoliceOfficerArrestController.GetOrInstall(scene);

                Assert.NotNull(controller);
                Assert.AreSame(police.transform, controller.PoliceRoot);
                Assert.AreSame(
                    dedicatedCamera,
                    controller.PoliceCamera);
                Assert.IsFalse(dedicatedCamera.enabled);
                Assert.IsFalse(dedicatedListener.enabled);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void InstallerHandlesMissingPoliceRootWithoutThrowing()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                PoliceOfficerArrestController controller = null;
                Assert.DoesNotThrow(() =>
                    controller =
                        PoliceOfficerArrestController.GetOrInstall(scene));
                Assert.NotNull(controller);
                Assert.IsNull(controller.PoliceRoot);
                Assert.IsNull(controller.PoliceCamera);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void AgentConfigurationReplacesDestroyedCachedComponent()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject police = CreateRoot(scene, "Police");
                police.SetActive(false);
                GameObject director = CreateRoot(
                    scene,
                    "PoliceOfficerArrestController");
                PoliceOfficerArrestController controller =
                    director.AddComponent<PoliceOfficerArrestController>();

                const BindingFlags Flags =
                    BindingFlags.NonPublic | BindingFlags.Instance;
                FieldInfo rootField =
                    typeof(PoliceOfficerArrestController).GetField(
                        "policeRoot",
                        Flags);
                FieldInfo agentField =
                    typeof(PoliceOfficerArrestController).GetField(
                        "policeAgent",
                        Flags);
                MethodInfo configureAgent =
                    typeof(PoliceOfficerArrestController).GetMethod(
                        "TryConfigureAgent",
                        Flags);
                Assert.NotNull(rootField);
                Assert.NotNull(agentField);
                Assert.NotNull(configureAgent);

                rootField.SetValue(controller, police.transform);
                NavMeshAgent destroyedAgent =
                    police.AddComponent<NavMeshAgent>();
                agentField.SetValue(controller, destroyedAgent);
                Object.DestroyImmediate(destroyedAgent);

                bool configured = false;
                Assert.DoesNotThrow(() =>
                    configured = (bool)configureAgent.Invoke(
                        controller,
                        null));

                Assert.IsTrue(configured);
                Assert.NotNull(police.GetComponent<NavMeshAgent>());
                Assert.AreSame(
                    police.GetComponent<NavMeshAgent>(),
                    agentField.GetValue(controller));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void PoliceDialogueMatchesMinhAndHenryPresentation()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject director = CreateRoot(
                    scene,
                    "PoliceOfficerArrestController");
                PoliceOfficerArrestController controller =
                    director.AddComponent<PoliceOfficerArrestController>();

                const BindingFlags Flags =
                    BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo ensureDialogue =
                    typeof(PoliceOfficerArrestController).GetMethod(
                        "EnsureDialogueUi",
                        Flags);
                Assert.NotNull(ensureDialogue);
                Assert.DoesNotThrow(() =>
                    ensureDialogue.Invoke(controller, null));

                Transform canvas = director.transform.Find(
                    "PoliceArrestDialogueCanvas");
                Assert.NotNull(canvas);
                Transform panel = canvas.Find("DialoguePanel");
                Assert.NotNull(panel);

                RectTransform panelRect =
                    panel.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0f, 0f), panelRect.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0f), panelRect.anchorMax);
                Assert.AreEqual(new Vector2(48f, 32f), panelRect.offsetMin);
                Assert.AreEqual(new Vector2(-48f, 232f), panelRect.offsetMax);

                Color panelColor = panel.GetComponent<Image>().color;
                Assert.AreEqual(0.02f, panelColor.r, 0.001f);
                Assert.AreEqual(0.03f, panelColor.g, 0.001f);
                Assert.AreEqual(0.05f, panelColor.b, 0.001f);
                Assert.AreEqual(0.94f, panelColor.a, 0.001f);

                TextMeshProUGUI speaker = panel.Find("SpeakerText")
                    .GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI line = panel.Find("LineText")
                    .GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI hint = panel.Find("AdvanceHintText")
                    .GetComponent<TextMeshProUGUI>();
                Assert.AreEqual(30f, speaker.fontSize, 0.001f);
                Assert.AreEqual(FontStyles.Bold, speaker.fontStyle);
                Assert.AreEqual(34f, line.fontSize, 0.001f);
                Assert.AreEqual(FontStyles.Normal, line.fontStyle);
                Assert.AreEqual(18f, hint.fontSize, 0.001f);
                Assert.AreEqual(FontStyles.Italic, hint.fontStyle);
                Assert.AreEqual(
                    "E / Space / Enter: hi\u1ec7n nhanh / ti\u1ebfp t\u1ee5c",
                    hint.text);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void ArrestDialogueTextMatchesChapterEndingScript()
        {
            const BindingFlags Flags =
                BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo first = typeof(PoliceOfficerArrestController)
                .GetField("FirstDialogueLine", Flags);
            FieldInfo second = typeof(PoliceOfficerArrestController)
                .GetField("SecondDialogueLine", Flags);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.AreEqual(
                "Nam, cậu đã bị bắt vì tội đốt quán ăn và đánh người.",
                first.GetRawConstantValue());
            Assert.AreEqual(
                "Mời cậu theo tôi về đồn.",
                second.GetRawConstantValue());
        }

        private static GameObject CreateRoot(Scene scene, string name)
        {
            GameObject result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }
    }
}
