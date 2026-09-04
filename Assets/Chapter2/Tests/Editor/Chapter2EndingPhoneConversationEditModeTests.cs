using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2EndingPhoneConversationEditModeTests
    {
        private const string PhonePrefabPath =
            "Assets/Chapter1/UI/Phone/Prefabs/PhonePanel.prefab";

        [Test]
        public void EndingConversationRestoresAndAdvancesInFourReplies()
        {
            GameObject phoneObject = CreatePhone();
            List<int> changedSteps = new List<int>();
            int openedCount = 0;
            int completedCount = 0;
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureWifiNetwork(
                    "Police_Station_Wifi",
                    true,
                    true,
                    null);
                phone.ConfigureChapter2EndingConversation(
                    true,
                    false,
                    0,
                    () => openedCount++,
                    step => changedSteps.Add(step),
                    () => completedCount++);

                phone.OpenMessenger();
                FindRequired<Button>(phoneObject, "Contact_Minh")
                    .onClick.Invoke();

                Assert.AreEqual(1, openedCount);
                Assert.AreEqual(0, phone.Chapter2EndingConversationStep);
                AssertTextExists(
                    phoneObject,
                    PhoneUIController.Chapter2EndingMinhQuestion);

                for (int step = 0;
                     step < PhoneUIController
                         .Chapter2EndingConversationFinalStep;
                     step++)
                {
                    FindRequired<Button>(
                            phoneObject,
                            "Chapter2EndingReply_" + step)
                        .onClick.Invoke();
                    Assert.AreEqual(
                        step + 1,
                        phone.Chapter2EndingConversationStep);
                }

                CollectionAssert.AreEqual(
                    new[] { 1, 2, 3, 4 },
                    changedSteps);
                Assert.AreEqual(1, completedCount);
                AssertTextExists(
                    phoneObject,
                    PhoneUIController.Chapter2EndingNamFinalReply);
                Assert.IsNull(FindChild(
                    phoneObject,
                    "Chapter2EndingReply_4"));
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
            }
        }

        [Test]
        public void EndingConversationCanRestoreAnOpenedPartialStep()
        {
            GameObject phoneObject = CreatePhone();
            int openedCount = 0;
            try
            {
                PhoneUIController phone =
                    phoneObject.GetComponent<PhoneUIController>();
                phone.ConfigureWifiNetwork(
                    "Police_Station_Wifi",
                    true,
                    true,
                    null);
                phone.ConfigureChapter2EndingConversation(
                    true,
                    true,
                    2,
                    () => openedCount++,
                    null,
                    null);

                phone.OpenMessenger();
                FindRequired<Button>(phoneObject, "Contact_Minh")
                    .onClick.Invoke();

                Assert.AreEqual(0, openedCount);
                Assert.AreEqual(2, phone.Chapter2EndingConversationStep);
                AssertTextExists(
                    phoneObject,
                    PhoneUIController.Chapter2EndingMinhHospital);
                Assert.NotNull(FindChild(
                    phoneObject,
                    "Chapter2EndingReply_2"));
            }
            finally
            {
                Object.DestroyImmediate(phoneObject);
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

        private static T FindRequired<T>(
            GameObject root,
            string objectName)
            where T : Component
        {
            GameObject child = FindChild(root, objectName);
            Assert.NotNull(child, $"Missing '{objectName}'.");
            T component = child.GetComponent<T>();
            Assert.NotNull(
                component,
                $"'{objectName}' is missing {typeof(T).Name}.");
            return component;
        }

        private static GameObject FindChild(
            GameObject root,
            string objectName)
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == objectName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void AssertTextExists(
            GameObject root,
            string expected)
        {
            TextMeshProUGUI[] texts =
                root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null &&
                    texts[i].text.Contains(expected))
                {
                    return;
                }
            }

            Assert.Fail($"Missing text: {expected}");
        }
    }
}
