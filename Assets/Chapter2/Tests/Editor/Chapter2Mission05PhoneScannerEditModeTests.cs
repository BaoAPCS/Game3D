using System.Collections.Generic;
using System.Reflection;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2Mission05PhoneScannerEditModeTests
    {
        private const string PhonePrefabPath =
            "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";
        private const string ScenePath =
            "Assets/Chapter2/Scenes/Police_Station.unity";

        [TestCase(30f, 1)]
        [TestCase(14f, 2)]
        [TestCase(9f, 3)]
        [TestCase(5.5f, 4)]
        [TestCase(2.5f, 5)]
        [TestCase(0f, 5)]
        public void SignalBandsMatchTheFiveLevelDesign(
            float distance,
            int expectedBars)
        {
            Assert.AreEqual(
                expectedBars,
                Chapter2WifiSignalModel.GetRawBars(distance));
        }

        [Test]
        public void SignalIgnoresHeightAndUsesHysteresisAtFiveBarBoundary()
        {
            Assert.AreEqual(
                5f,
                Chapter2WifiSignalModel.HorizontalDistance(
                    new Vector3(0f, -100f, 0f),
                    new Vector3(3f, 100f, 4f)),
                0.0001f);

            Chapter2WifiSignalModel model =
                new Chapter2WifiSignalModel(0f, 0.4f);
            Assert.AreEqual(5, model.Reset(2.4f));
            Assert.AreEqual(5, model.Update(2.7f, 0.2f));
            Assert.AreEqual(4, model.Update(3f, 0.2f));

            model.Reset(2.7f);
            Assert.AreEqual(4, model.Update(2.3f, 0.2f));
            Assert.AreEqual(5, model.Update(2f, 0.2f));
        }

        [Test]
        public void ScannerIsHiddenUntilAChapterConfiguresIt()
        {
            GameObject phoneObject = CreatePhone();
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();

                phone.OpenWifiSettings();
                Assert.IsFalse(phone.WifiSignalScannerAvailable);
                Assert.IsNull(FindChild(
                    phoneObject,
                    "WifiStartSignalScannerButton"));

                phone.ConfigureWifiSignalScanner(true, () => 2, null);
                phone.OpenWifiSettings();

                Assert.IsTrue(phone.WifiSignalScannerAvailable);
                Assert.NotNull(FindChild(
                    phoneObject,
                    "WifiStartSignalScannerButton"));
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void ScannerHudStaysIndependentUntilStoppedFromPhone()
        {
            GameObject phoneObject = CreatePhone();
            GameObject playerObject = new GameObject("ScannerTestPlayer");
            List<bool> activeChanges = new List<bool>();
            try
            {
                PlayerInputLock inputLock =
                    playerObject.AddComponent<PlayerInputLock>();
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.Configure(inputLock);
                phone.ConfigureWifiNetwork(
                    "Police_Station_Wifi",
                    true,
                    true,
                    null);
                phone.ConfigureWifiSignalScanner(
                    true,
                    () => 4,
                    active => activeChanges.Add(active));

                phone.StartWifiSignalScanner();

                Assert.IsTrue(phone.IsSignalScannerActive);
                Assert.IsTrue(phone.IsScannerWalkMode);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsTrue(phoneObject.activeSelf);
                Assert.AreEqual(4, phone.CurrentWifiSignalBars);
                Assert.IsFalse(inputLock.IsLocked);
                Assert.AreEqual(new[] { true }, activeChanges);

                GameObject compact = phone.WifiSignalScannerHudRoot;
                Assert.NotNull(compact);
                Assert.IsTrue(compact.activeSelf);
                Assert.IsFalse(
                    compact.transform.IsChildOf(phoneObject.transform));
                Assert.IsFalse(phoneObject.GetComponent<Image>().enabled);
                Assert.AreEqual(
                    0f,
                    phoneObject.GetComponent<CanvasGroup>().alpha);
                Assert.IsFalse(phoneObject.GetComponent<CanvasGroup>()
                    .blocksRaycasts);
                Assert.IsFalse(FindChild(phoneObject, "PhoneFrame")
                    .activeSelf);

                RectTransform compactRect =
                    compact.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(0f, 1f), compactRect.anchorMin);
                Assert.AreEqual(new Vector2(0f, 1f), compactRect.anchorMax);
                Assert.AreEqual(new Vector2(0f, 1f), compactRect.pivot);
                Assert.Greater(compactRect.anchoredPosition.x, 0f);
                Assert.Less(compactRect.anchoredPosition.y, 0f);

                CanvasGroup compactGroup =
                    compact.GetComponent<CanvasGroup>();
                Assert.NotNull(compactGroup);
                Assert.IsFalse(compactGroup.interactable);
                Assert.IsFalse(compactGroup.blocksRaycasts);

                phone.ExpandScanner();

                Assert.IsFalse(phone.IsScannerWalkMode);
                Assert.IsTrue(inputLock.IsLocked);
                Assert.That(
                    inputLock.ActiveLocks,
                    Does.Contain(PlayerInputLock.PhoneReason));
                Assert.IsTrue(phoneObject.GetComponent<Image>().enabled);
                Assert.IsTrue(phoneObject.GetComponent<CanvasGroup>()
                    .blocksRaycasts);
                Assert.IsFalse(compact.activeSelf);

                phone.ClosePhone();

                Assert.IsTrue(phone.IsSignalScannerActive);
                Assert.IsTrue(phone.IsScannerWalkMode);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsTrue(compact.activeSelf);
                Assert.IsFalse(inputLock.IsLocked);
                Assert.AreEqual(new[] { true }, activeChanges);

                phone.ExpandScanner();
                phone.SuspendScannerForModal();

                Assert.IsTrue(phone.IsSignalScannerActive);
                Assert.IsTrue(phone.IsSignalScannerSuspended);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsFalse(inputLock.IsLocked);
                Assert.AreEqual(1, activeChanges.Count);

                phone.ResumeScannerWalkMode();
                Assert.IsTrue(phone.IsScannerWalkMode);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsFalse(inputLock.IsLocked);

                phone.StopScanner();

                Assert.IsFalse(phone.IsSignalScannerActive);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsFalse(phoneObject.activeSelf);
                Assert.IsFalse(inputLock.IsLocked);
                Assert.AreEqual(new[] { true, false }, activeChanges);
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void EscapeClosesPhoneWithoutStoppingScannerHud()
        {
            GameObject phoneObject = CreatePhone();
            GameObject inputObject = new GameObject("ScannerInputTest");
            inputObject.SetActive(false);
            try
            {
                BackpackPhoneInputController inputController =
                    inputObject.AddComponent<BackpackPhoneInputController>();
                FieldInfo createRuntimeUiIfMissing =
                    typeof(BackpackPhoneInputController).GetField(
                        "createRuntimeUiIfMissing",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(createRuntimeUiIfMissing);
                createRuntimeUiIfMissing.SetValue(inputController, false);

                PlayerInputLock inputLock =
                    inputObject.GetComponent<PlayerInputLock>();
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                inputController.Configure(
                    inputObject.GetComponent<InventoryController>(),
                    null,
                    phone,
                    null,
                    inputLock);
                phone.ConfigureWifiNetwork(
                    "Police_Station_Wifi",
                    true,
                    true,
                    null);
                phone.ConfigureWifiSignalScanner(true, () => 3, null);
                phone.StartWifiSignalScanner();
                phone.ExpandScanner();

                MethodInfo handlePausePressed =
                    typeof(BackpackPhoneInputController).GetMethod(
                        "HandlePausePressed",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(handlePausePressed);
                handlePausePressed.Invoke(inputController, null);

                Assert.IsTrue(phone.IsSignalScannerActive);
                Assert.IsTrue(phone.IsScannerWalkMode);
                Assert.IsFalse(phone.IsOpen);
                Assert.IsTrue(phone.WifiSignalScannerHudRoot.activeSelf);
                Assert.IsFalse(inputLock.IsLocked);

                handlePausePressed.Invoke(inputController, null);
                Assert.IsTrue(phone.IsSignalScannerActive);
                Assert.IsTrue(phone.WifiSignalScannerHudRoot.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
                Object.DestroyImmediate(inputObject);
            }
        }

        [TestCase(1, "XA")]
        [TestCase(2, "GẦN HƠN")]
        [TestCase(3, "KHÁ GẦN")]
        [TestCase(4, "RẤT GẦN")]
        [TestCase(5, "ROUTER Ở NGAY KHU VỰC NÀY")]
        public void ScannerMapsEveryBarCountToVietnameseFeedback(
            int bars,
            string expected)
        {
            Assert.AreEqual(
                expected,
                PhoneUIController.GetWifiSignalLabel(bars));
        }

        [Test]
        public void PoliceStationContainsExactRouterSetupWithCameraOff()
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
                    Chapter2WifiSignalScannerBootstrap.TryFindRouterSetup(
                        scene,
                        out GameObject router,
                        out Collider signalCollider,
                        out Camera routerCamera));
                Assert.NotNull(router);
                Assert.NotNull(signalCollider);
                Assert.NotNull(routerCamera);
                Assert.AreEqual("3d_router", router.name);
                Assert.AreEqual("router_cam", routerCamera.name);
                Assert.IsTrue(routerCamera.transform.IsChildOf(
                    router.transform));
                Assert.IsFalse(routerCamera.enabled);

                AudioListener listener =
                    routerCamera.GetComponent<AudioListener>();
                Assert.NotNull(listener);
                Assert.IsFalse(listener.enabled);
            }
            finally
            {
                if (closeAfterTest && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void RouterPromptAndInspectionUiUseExpectedControls()
        {
            GameObject owner = new GameObject("Mission05PromptTest");
            try
            {
                Chapter2RouterInteractable interactable =
                    owner.AddComponent<Chapter2RouterInteractable>();
                Assert.AreEqual(
                    "[F] Kiểm tra thiết bị Wi-Fi",
                    interactable.GetInteractionPrompt(default));

                Chapter2RouterInspectionUI ui =
                    Chapter2RouterInspectionUI.Create(owner.transform);
                Assert.IsFalse(ui.IsVisible);
                ui.Show(true);
                Assert.IsTrue(ui.IsVisible);
                Assert.NotNull(FindChild(owner, "RouterInspectionPrompt"));
                ui.Hide();
                Assert.IsFalse(ui.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(owner);
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

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null &&
                    children[i].name == objectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }
    }
}
