using System.Reflection;
using DormitoryMystery.Chapter1;
using NavKeypad;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2BrokenDoorEditModeTests
    {
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";
        private const string PhonePrefabPath =
            "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";

        [Test]
        public void InteractionsUseTheRequestedPrompt()
        {
            GameObject owner =
                new GameObject("BrokenDoorPromptTest");
            try
            {
                Chapter2BrokenDoorInteractable interactable =
                    owner.AddComponent<
                        Chapter2BrokenDoorInteractable>();
                Assert.AreEqual(
                    "[F] Kiểm tra",
                    interactable.GetInteractionPrompt(default));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BrokenDoorSaveRepairsMission04AndDeepCopies()
        {
            Chapter2SaveData source = new Chapter2SaveData
            {
                Mission05BrokenDoorUnlocked = true
            };

            source.EnsureValidDefaults();
            Chapter2SaveData copy = source.DeepCopy();
            source.Mission05BrokenDoorUnlocked = false;

            Assert.IsTrue(copy.Mission05BrokenDoorUnlocked);
            Assert.IsTrue(copy.Mission04MinhMessagesRead);
            Assert.IsTrue(copy.Mission04PoliceWifiConnected);
            Assert.IsTrue(copy.Mission04Completed);
        }

        [Test]
        public void PoliceStationContainsSafeBrokenDoorSetup()
        {
            Scene scene = OpenScene(out bool closeAfterTest);
            try
            {
                Assert.IsTrue(
                    Chapter2BrokenDoorBootstrap.TryFindSetup(
                        scene,
                        out Camera contractCamera,
                        out SphereCollider contractCollider,
                        out Transform keypadTransform,
                        out Keypad keypad,
                        out SphereCollider keypadCollider,
                        out Camera keypadCamera,
                        out Transform doorOnePanel,
                        out Transform doorTwoPanel,
                        out BoxCollider doorwayHeaderCollider));

                Assert.AreEqual(
                    "contract_camera",
                    contractCamera.name);
                Assert.AreEqual("Keypad", keypadTransform.name);
                Assert.AreEqual("Keypad_cam", keypadCamera.name);
                Assert.That(
                    doorOnePanel.name,
                    Does.StartWith("Top"));
                Assert.That(
                    doorTwoPanel.name,
                    Does.StartWith("Top"));
                Assert.IsFalse(
                    doorOnePanel.gameObject.isStatic,
                    "Door1/Top must remain dynamic so its renderer follows the opening animation.");
                Assert.IsFalse(
                    doorTwoPanel.gameObject.isStatic,
                    "Door2/Top must remain dynamic so its renderer follows the opening animation.");
                Assert.AreEqual(
                    Chapter2BrokenDoorMission
                        .CorrectKeypadCombo,
                    keypad.KeypadCombo);
                Assert.IsTrue(contractCollider.isTrigger);
                Assert.IsTrue(keypadCollider.isTrigger);
                Assert.IsFalse(contractCollider.enabled);
                Assert.IsFalse(keypadCollider.enabled);
                Assert.IsFalse(
                    doorwayHeaderCollider.isTrigger);
                Assert.Greater(
                    doorwayHeaderCollider.size.x,
                    doorwayHeaderCollider.size.y);
                Assert.IsTrue(doorwayHeaderCollider.enabled);
                AssertCameraDisabled(contractCamera);
                AssertCameraDisabled(keypadCamera);
            }
            finally
            {
                if (closeAfterTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void MissionGatesCollidersAndOwnsCameraTransitions()
        {
            Scene scene = OpenScene(out bool closeAfterTest);
            GameObject missionObject =
                new GameObject("BrokenDoorMissionTest");
            GameObject managerObject =
                new GameObject("BrokenDoorSaveTest");
            managerObject.SetActive(false);
            GameObject playerObject =
                new GameObject("BrokenDoorPlayerTest");
            GameObject gameplayCameraObject =
                new GameObject(
                    "BrokenDoorGameplayCameraTest",
                    typeof(Camera),
                    typeof(AudioListener));
            GameObject contractZoneObject =
                new GameObject("ContractZoneTest");
            GameObject keypadZoneObject =
                new GameObject("KeypadZoneTest");
            GameObject phoneObject = CreatePhone();

            Camera contractCamera = null;
            Camera keypadCamera = null;
            SphereCollider contractCollider = null;
            SphereCollider keypadCollider = null;
            BoxCollider doorwayHeaderCollider = null;
            Keypad keypad = null;
            Transform firstPanel = null;
            Transform secondPanel = null;
            Vector3 firstClosedPosition = default;
            Vector3 secondClosedPosition = default;
            int originalCombo = 0;
            try
            {
                Assert.IsTrue(
                    Chapter2BrokenDoorBootstrap.TryFindSetup(
                        scene,
                        out contractCamera,
                        out contractCollider,
                        out _,
                        out keypad,
                        out keypadCollider,
                        out keypadCamera,
                        out firstPanel,
                        out secondPanel,
                        out doorwayHeaderCollider));
                firstClosedPosition = firstPanel.position;
                secondClosedPosition = secondPanel.position;
                originalCombo = keypad.KeypadCombo;

                playerObject.SetActive(false);
                Chapter1InputReader inputReader =
                    playerObject.AddComponent<
                        Chapter1InputReader>();
                PlayerInputLock inputLock =
                    playerObject.AddComponent<PlayerInputLock>();
                Chapter1InteractionController interaction =
                    playerObject.AddComponent<
                        Chapter1InteractionController>();
                inputReader.enabled = false;
                interaction.enabled = false;

                Camera gameplayCamera =
                    gameplayCameraObject.GetComponent<Camera>();
                AudioListener gameplayListener =
                    gameplayCameraObject.GetComponent<
                        AudioListener>();
                interaction.SetGameplayCamera(gameplayCamera);
                playerObject.SetActive(true);

                Chapter2SaveData data =
                    Chapter2SaveData.CreateDefault();
                data.Mission04PoliceWifiConnected = true;
                data.EnsureValidDefaults();
                Chapter2SaveManager saveManager =
                    managerObject.AddComponent<
                        Chapter2SaveManager>();
                SetPrivateField(
                    saveManager,
                    "currentData",
                    data);
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureWifiNetwork(
                    "Police_Station_Wifi",
                    true,
                    true,
                    null);
                phone.ConfigureWifiSignalScanner(
                    true,
                    () => 3,
                    null);

                Chapter2MissionTriggerZone contractZone =
                    contractZoneObject.AddComponent<
                        Chapter2MissionTriggerZone>();
                Chapter2MissionTriggerZone keypadZone =
                    keypadZoneObject.AddComponent<
                        Chapter2MissionTriggerZone>();
                contractZone.Configure(
                    contractCollider,
                    inputReader);
                keypadZone.Configure(
                    keypadCollider,
                    inputReader);

                Chapter2BrokenDoorMission mission =
                    missionObject.AddComponent<
                        Chapter2BrokenDoorMission>();
                ConfigureMission(
                    mission,
                    saveManager,
                    contractCollider,
                    contractZone,
                    contractCamera,
                    keypadCollider,
                    keypadZone,
                    keypad,
                    keypadCamera,
                    firstPanel,
                    secondPanel,
                    doorwayHeaderCollider,
                    inputReader,
                    phone);

                Assert.IsFalse(data.Mission04Completed);
                Assert.IsFalse(contractCollider.enabled);
                Assert.IsFalse(keypadCollider.enabled);
                Assert.IsTrue(
                    doorwayHeaderCollider.enabled);

                data.Mission04MinhMessagesRead = true;
                data.EnsureValidDefaults();
                ConfigureMission(
                    mission,
                    saveManager,
                    contractCollider,
                    contractZone,
                    contractCamera,
                    keypadCollider,
                    keypadZone,
                    keypad,
                    keypadCamera,
                    firstPanel,
                    secondPanel,
                    doorwayHeaderCollider,
                    inputReader,
                    phone);

                Assert.IsFalse(contractCollider.enabled);
                Assert.IsFalse(keypadCollider.enabled);
                Assert.IsFalse(mission.InteractionsAvailable);

                phone.StartWifiSignalScanner();
                data.Mission05ScannerActivated = true;
                SynchronizeProgress(mission);

                Assert.IsTrue(contractCollider.enabled);
                Assert.IsTrue(keypadCollider.enabled);
                Assert.IsTrue(mission.InteractionsAvailable);
                Assert.IsTrue(
                    doorwayHeaderCollider.enabled);
                Assert.AreEqual(
                    Chapter2BrokenDoorMission
                        .CorrectKeypadCombo,
                    keypad.KeypadCombo);

                InteractionContext context =
                    new InteractionContext(
                        playerObject,
                        playerObject.transform,
                        null,
                        null,
                        interaction);
                Assert.IsTrue(mission.TryBeginInspection(
                    Chapter2BrokenDoorInspection.Contract,
                    context));
                Assert.IsFalse(playerObject.activeSelf);
                Assert.IsFalse(gameplayCamera.enabled);
                Assert.IsFalse(gameplayListener.enabled);
                Assert.IsTrue(contractCamera.enabled);
                Assert.IsTrue(
                    contractCamera
                        .GetComponent<AudioListener>().enabled);
                AssertCameraDisabled(keypadCamera);
                Assert.That(
                    inputLock.ActiveLocks,
                    Does.Contain(
                        Chapter2BrokenDoorMission
                            .InputLockReason));

                mission.EndInspection();
                Assert.IsTrue(playerObject.activeSelf);
                Assert.IsTrue(gameplayCamera.enabled);
                Assert.IsTrue(gameplayListener.enabled);
                AssertCameraDisabled(contractCamera);
                Assert.IsFalse(inputLock.IsLocked);

                Assert.IsTrue(mission.TryBeginInspection(
                    Chapter2BrokenDoorInspection.Keypad,
                    context));
                Assert.IsFalse(playerObject.activeSelf);
                Assert.IsTrue(keypadCamera.enabled);
                Assert.IsTrue(
                    keypadCamera
                        .GetComponent<AudioListener>().enabled);
                AssertCameraDisabled(contractCamera);
                Assert.AreEqual(
                    CursorLockMode.None,
                    Cursor.lockState);
                Assert.IsTrue(Cursor.visible);
                mission.EndInspection();

                phone.StopScanner();
                SynchronizeProgress(mission);
                Assert.IsTrue(contractCollider.enabled);
                Assert.IsTrue(keypadCollider.enabled);
                Assert.IsTrue(mission.InteractionsAvailable);

                phone.StartWifiSignalScanner();
                SynchronizeProgress(mission);

                data.Mission05BrokenDoorUnlocked = true;
                data.EnsureValidDefaults();
                ConfigureMission(
                    mission,
                    saveManager,
                    contractCollider,
                    contractZone,
                    contractCamera,
                    keypadCollider,
                    keypadZone,
                    keypad,
                    keypadCamera,
                    firstPanel,
                    secondPanel,
                    doorwayHeaderCollider,
                    inputReader,
                    phone);

                Assert.IsTrue(contractCollider.enabled);
                Assert.IsFalse(keypadCollider.enabled);
                Assert.IsFalse(
                    doorwayHeaderCollider.enabled);
                Assert.AreEqual(
                    firstClosedPosition.x,
                    firstPanel.position.x,
                    0.0001f);
                Assert.AreEqual(
                    firstClosedPosition.z,
                    firstPanel.position.z,
                    0.0001f);
                Assert.AreEqual(
                    firstClosedPosition.y +
                    Chapter2BrokenDoorMission
                        .DefaultDoorOpenHeight,
                    firstPanel.position.y,
                    0.0001f);
                Assert.AreEqual(
                    secondClosedPosition.y +
                    Chapter2BrokenDoorMission
                        .DefaultDoorOpenHeight,
                    secondPanel.position.y,
                    0.0001f);
            }
            finally
            {
                if (firstPanel != null)
                {
                    firstPanel.position = firstClosedPosition;
                }

                if (secondPanel != null)
                {
                    secondPanel.position = secondClosedPosition;
                }

                if (keypad != null)
                {
                    keypad.SetCombo(originalCombo);
                }

                if (contractCollider != null)
                {
                    contractCollider.enabled = false;
                }

                if (keypadCollider != null)
                {
                    keypadCollider.enabled = false;
                }

                if (doorwayHeaderCollider != null)
                {
                    doorwayHeaderCollider.enabled = true;
                }

                Chapter2BrokenDoorBootstrap.DisableCamera(
                    contractCamera);
                Chapter2BrokenDoorBootstrap.DisableCamera(
                    keypadCamera);
                Object.DestroyImmediate(missionObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(gameplayCameraObject);
                Object.DestroyImmediate(contractZoneObject);
                Object.DestroyImmediate(keypadZoneObject);
                Object.DestroyImmediate(phoneObject);

                if (closeAfterTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ConfigureMission(
            Chapter2BrokenDoorMission mission,
            Chapter2SaveManager saveManager,
            SphereCollider contractCollider,
            Chapter2MissionTriggerZone contractZone,
            Camera contractCamera,
            SphereCollider keypadCollider,
            Chapter2MissionTriggerZone keypadZone,
            Keypad keypad,
            Camera keypadCamera,
            Transform firstPanel,
            Transform secondPanel,
            BoxCollider doorwayHeaderCollider,
            Chapter1InputReader inputReader,
            PhoneUIController phone)
        {
            mission.Configure(
                saveManager,
                contractCollider,
                contractZone,
                contractCamera,
                keypadCollider,
                keypadZone,
                keypad,
                keypadCamera,
                firstPanel,
                secondPanel,
                doorwayHeaderCollider,
                inputReader,
                phone);
        }

        private static void SynchronizeProgress(
            Chapter2BrokenDoorMission mission)
        {
            MethodInfo method =
                typeof(Chapter2BrokenDoorMission).GetMethod(
                    "SynchronizeProgress",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(mission, new object[] { false });
        }

        private static GameObject CreatePhone()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PhonePrefabPath);
            Assert.NotNull(prefab, "Missing PhonePanel prefab.");
            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab)
                as GameObject;
            Assert.NotNull(instance);
            return instance;
        }

        private static Scene OpenScene(
            out bool closeAfterTest)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            closeAfterTest = !scene.IsValid() || !scene.isLoaded;
            return closeAfterTest
                ? EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive)
                : scene;
        }

        private static void AssertCameraDisabled(
            Camera camera)
        {
            Assert.NotNull(camera);
            Assert.IsFalse(camera.enabled);
            AudioListener listener =
                camera.GetComponent<AudioListener>();
            Assert.NotNull(listener);
            Assert.IsFalse(listener.enabled);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(target, value);
        }
    }
}
