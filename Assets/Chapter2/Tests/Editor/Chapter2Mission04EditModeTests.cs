using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2Mission04EditModeTests
    {
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";
        private const string PhonePrefabPath =
            "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";

        [Test]
        public void FreshSaveStartsWithAllMission04ProgressLocked()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();

            Assert.IsFalse(data.Mission04ComputerUnlocked);
            Assert.IsFalse(data.Mission04WifiPasswordDiscovered);
            Assert.IsFalse(data.Mission04PoliceWifiConnected);
            Assert.IsFalse(data.Mission04MinhMessagesRead);
            Assert.IsFalse(data.Mission04Completed);
        }

        [Test]
        public void DeepCopyPreservesAndDetachesMission04Progress()
        {
            Chapter2SaveData source = Chapter2SaveData.CreateDefault();
            source.Mission04ComputerUnlocked = true;
            source.Mission04WifiPasswordDiscovered = true;
            source.Mission04PoliceWifiConnected = true;
            source.Mission04MinhMessagesRead = true;

            Chapter2SaveData copy = source.DeepCopy();
            source.Mission04ComputerUnlocked = false;
            source.Mission04WifiPasswordDiscovered = false;
            source.Mission04PoliceWifiConnected = false;
            source.Mission04MinhMessagesRead = false;

            Assert.IsTrue(copy.Mission04ComputerUnlocked);
            Assert.IsTrue(copy.Mission04WifiPasswordDiscovered);
            Assert.IsTrue(copy.Mission04PoliceWifiConnected);
            Assert.IsTrue(copy.Mission04MinhMessagesRead);
            Assert.IsTrue(copy.Mission04Completed);
        }

        [Test]
        public void ConnectedOrReadMission04SaveRepairsEveryPrerequisite()
        {
            Chapter2SaveData connected = new Chapter2SaveData
            {
                Mission04PoliceWifiConnected = true
            };
            connected.EnsureValidDefaults();

            Assert.IsTrue(connected.Mission04ComputerUnlocked);
            Assert.IsTrue(connected.Mission04WifiPasswordDiscovered);
            Assert.IsFalse(connected.Mission04Completed);
            Assert.IsTrue(connected.Mission03Completed);
            Assert.IsTrue(connected.HasPhone);
            Assert.IsTrue(connected.HasPoliceStationKey);
            Assert.IsTrue(connected.Mission02JailObstacleDisabled);
            Assert.IsTrue(connected.Mission01ServiceCardCollected);

            Chapter2SaveData read = new Chapter2SaveData
            {
                Mission04MinhMessagesRead = true
            };
            read.EnsureValidDefaults();

            Assert.IsTrue(read.Mission04PoliceWifiConnected);
            Assert.IsTrue(read.Mission04WifiPasswordDiscovered);
            Assert.IsTrue(read.Mission04ComputerUnlocked);
            Assert.IsTrue(read.Mission04Completed);
            Assert.IsTrue(read.Mission03Completed);
        }

        [Test]
        public void ComputerAndWifiPasswordsRequireExactValues()
        {
            GameObject owner = new GameObject("Mission04PasswordTest");
            try
            {
                Chapter2PoliceComputerUI ui =
                    Chapter2PoliceComputerUI.Create(owner.transform);
                ui.Show(false, false);

                Assert.IsFalse(ui.TryLogin("00000"));
                Assert.IsFalse(ui.TryLogin("12345 "));
                Assert.IsTrue(ui.TryLogin("12345"));

                Assert.IsFalse(
                    Chapter2PoliceComputerMission
                        .IsCorrectWifiPassword("abcd@@@1234"));
                Assert.IsFalse(
                    Chapter2PoliceComputerMission
                        .IsCorrectWifiPassword("ABCD@@@12345"));
                Assert.IsTrue(
                    Chapter2PoliceComputerMission
                        .IsCorrectWifiPassword("abcd@@@12345"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DeskPromptUsesRequestedInspectionText()
        {
            GameObject desk = new GameObject("Mission04DeskPromptTest");
            try
            {
                Chapter2DeskComputerInteractable interactable =
                    desk.AddComponent<Chapter2DeskComputerInteractable>();
                Assert.AreEqual(
                    "[F] Kiểm tra",
                    interactable.GetInteractionPrompt(default));
            }
            finally
            {
                Object.DestroyImmediate(desk);
            }
        }

        [Test]
        public void ComputerUiStartsHiddenAndRunsLoginMenuRevealOnce()
        {
            GameObject owner = new GameObject("Mission04ComputerUiTest");
            int unlockedCount = 0;
            int revealedCount = 0;
            try
            {
                Chapter2PoliceComputerUI ui =
                    Chapter2PoliceComputerUI.Create(owner.transform);
                ui.Configure(
                    () => unlockedCount++,
                    () => revealedCount++);

                Assert.IsFalse(ui.IsVisible);
                Assert.NotNull(FindChild(owner, "LoginScreen"));
                Assert.NotNull(FindChild(owner, "MenuScreen"));
                Assert.NotNull(
                    FindChild(owner, "RevealWifiPasswordButton"));

                ui.Show(false, false);
                Assert.IsTrue(ui.LoginVisible);
                Assert.IsFalse(ui.MenuVisible);
                Assert.IsFalse(ui.TryLogin("54321"));
                Assert.AreEqual(0, unlockedCount);

                Assert.IsTrue(ui.TryLogin("12345"));
                Assert.AreEqual(1, unlockedCount);
                Assert.IsFalse(ui.LoginVisible);
                Assert.IsTrue(ui.MenuVisible);
                Assert.IsFalse(ui.PasswordVisible);

                ui.RevealWifiPassword();
                ui.RevealWifiPassword();
                Assert.IsTrue(ui.PasswordVisible);
                Assert.AreEqual(1, revealedCount);
                Assert.IsTrue(HasExactText(
                    owner,
                    Chapter2PoliceComputerUI.WifiSsid));
                Assert.IsTrue(HasExactText(
                    owner,
                    Chapter2PoliceComputerUI.WifiPassword));

                ui.Hide();
                Assert.IsFalse(ui.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void PhoneDefaultsToConnectedKtxAndBuildsWifiAppButton()
        {
            GameObject phoneObject = CreatePhone();
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ShowHomeScreen();

                Assert.AreEqual(
                    PhoneUIController.DefaultWifiNetworkName,
                    phone.WifiNetworkName);
                Assert.IsTrue(phone.WifiConnected);
                Assert.IsTrue(phone.MessengerOnline);
                Assert.NotNull(FindChild(phoneObject, "WifiButton"));
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void OfflinePhoneKeepsMessengerUnavailable()
        {
            GameObject phoneObject = CreatePhone();
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureCarriedPhoneData(
                    Chapter1SaveData.CreateDefault(),
                    false);
                phone.OpenMessenger();

                Assert.IsFalse(phone.WifiConnected);
                Assert.IsFalse(phone.MessengerOnline);
                Assert.IsTrue(HasExactText(
                    phoneObject,
                    PhoneUIController.OfflineMessengerMessage));
                Assert.IsNull(FindChild(phoneObject, "Contact_Minh"));
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void PhoneWifiFormRejectsWrongAndConnectsWithExactPassword()
        {
            GameObject phoneObject = CreatePhone();
            int attempts = 0;
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureWifiNetwork(
                    Chapter2PoliceComputerUI.WifiSsid,
                    false,
                    true,
                    password =>
                    {
                        attempts++;
                        return Chapter2PoliceComputerMission
                            .IsCorrectWifiPassword(password);
                    });
                phone.OpenWifiSettings();

                TMP_InputField input = FindChildComponent<TMP_InputField>(
                    phoneObject,
                    "WifiPasswordInput");
                Button connect = FindChildComponent<Button>(
                    phoneObject,
                    "WifiConnectButton");
                Assert.NotNull(input);
                Assert.NotNull(connect);

                input.text = "wrong-password";
                connect.onClick.Invoke();
                Assert.AreEqual(1, attempts);
                Assert.IsFalse(phone.WifiConnected);
                Assert.IsFalse(phone.MessengerOnline);

                input.text = Chapter2PoliceComputerUI.WifiPassword;
                connect.onClick.Invoke();
                Assert.AreEqual(2, attempts);
                Assert.IsTrue(phone.WifiConnected);
                Assert.IsTrue(phone.MessengerOnline);
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void ConnectedMessengerShowsTwoExactMinhMessagesAndReadsOnce()
        {
            GameObject phoneObject = CreatePhone();
            int readCount = 0;
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureWifiNetwork(
                    Chapter2PoliceComputerUI.WifiSsid,
                    true,
                    true,
                    null);
                phone.ConfigureMinhMissionMessages(
                    true,
                    false,
                    () => readCount++);
                phone.OpenMessenger();

                Button minh = FindChildComponent<Button>(
                    phoneObject,
                    "Contact_Minh");
                Assert.NotNull(minh);
                Assert.IsTrue(HasExactText(minh.gameObject, "2"));
                minh.onClick.Invoke();

                Assert.IsTrue(HasExactText(
                    phoneObject,
                    PhoneUIController.MinhMissionMessageOne));
                Assert.IsTrue(HasExactText(
                    phoneObject,
                    PhoneUIController.MinhMissionMessageTwo));
                Assert.IsTrue(phone.MinhMissionMessagesRead);
                Assert.AreEqual(1, readCount);

                phone.OpenMessenger();
                minh = FindChildComponent<Button>(
                    phoneObject,
                    "Contact_Minh");
                Assert.NotNull(minh);
                minh.onClick.Invoke();
                Assert.AreEqual(1, readCount);
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void PoliceStationHasOneMatchingWorkstationWithCameraOff()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeAfterTest = !scene.IsValid() || !scene.isLoaded;
            if (closeAfterTest)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Assert.IsTrue(
                    Chapter2PoliceComputerMissionBootstrap
                        .TryFindWorkstation(
                            scene,
                            out GameObject desk,
                            out GameObject desktop,
                            out Camera desktopCamera));
                Assert.NotNull(desk);
                Assert.NotNull(desktop);
                Assert.NotNull(desktopCamera);
                Assert.AreEqual("DeskBase", desk.name.Trim());
                Assert.IsTrue(desktop.transform.IsChildOf(desk.transform));
                Assert.IsTrue(
                    desktopCamera.transform.IsChildOf(desk.transform));
                Assert.IsFalse(desktopCamera.enabled);

                AudioListener listener =
                    desktopCamera.GetComponent<AudioListener>();
                Assert.NotNull(listener);
                Assert.IsFalse(listener.enabled);

                GameObject office = FindRoot(
                    scene,
                    Chapter2PoliceComputerMissionBootstrap
                        .OfficeEnvironmentName);
                Assert.NotNull(office);
                int matchingCameraCount = 0;
                Camera[] cameras = office.GetComponentsInChildren<Camera>(
                    true);
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] != null &&
                        cameras[i].gameObject.name ==
                        Chapter2PoliceComputerMissionBootstrap
                            .DesktopCameraName)
                    {
                        matchingCameraCount++;
                    }
                }

                Assert.AreEqual(1, matchingCameraCount);
            }
            finally
            {
                if (closeAfterTest && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject CreatePhone()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhonePrefabPath);
            Assert.NotNull(prefab, "Missing PhonePanel prefab.");
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab)
                as GameObject;
            Assert.NotNull(instance);
            return instance;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null &&
                    transforms[i].name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static T FindChildComponent<T>(
            GameObject root,
            string objectName)
            where T : Component
        {
            GameObject child = FindChild(root, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static bool HasExactText(
            GameObject root,
            string expected)
        {
            TextMeshProUGUI[] texts =
                root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].text == expected)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
