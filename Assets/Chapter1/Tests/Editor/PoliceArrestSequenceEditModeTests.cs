using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class PoliceArrestSequenceEditModeTests
    {
        [Test]
        public void ArrivalCapturesTargetXAndPreservesAuthoredLane()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject car = CreateRoot(
                    scene,
                    PoliceArrestSequenceController.PoliceCarObjectName);
                car.transform.position = new Vector3(-80.01f, 0.01f, -9.56f);
                Rigidbody body = car.AddComponent<Rigidbody>();
                car.SetActive(false);

                GameObject nam = CreateRoot(scene, "Nam");
                nam.transform.position = new Vector3(5.2f, 2f, 40f);

                PoliceArrestSequenceController controller =
                    PoliceArrestSequenceController.GetOrInstall(scene);
                Assert.NotNull(controller);

                LogAssert.Expect(
                    LogType.Warning,
                    "[PoliceArrest] police_car has no configured " +
                    "AudioSource/clip; arrival will continue silently.");
                bool callbackInvoked = false;
                bool callbackResult = false;
                Assert.IsTrue(controller.BeginArrest(
                    nam.transform,
                    arrived =>
                    {
                        callbackInvoked = true;
                        callbackResult = arrived;
                    }));

                Assert.IsTrue(car.activeSelf);
                Assert.AreEqual(
                    PoliceArrestSequenceController.SequenceState.Approaching,
                    controller.State);
                Assert.AreEqual(
                    5.2f -
                    PoliceArrestSequenceController.PoliceCarStopOffset,
                    controller.Destination.x,
                    0.001f);
                Assert.AreEqual(0.01f, controller.Destination.y, 0.001f);
                Assert.AreEqual(-9.56f, controller.Destination.z, 0.001f);
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse(body.useGravity);
                Assert.IsFalse(callbackInvoked);

                Assert.IsTrue(
                    controller.RestoreTerminalArrestState(nam.transform));
                Assert.IsTrue(callbackInvoked);
                Assert.IsTrue(callbackResult);
                Assert.AreEqual(controller.Destination, car.transform.position);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void CompletedSequenceIsIdempotent()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject car = CreateRoot(
                    scene,
                    PoliceArrestSequenceController.PoliceCarObjectName);
                car.transform.position = new Vector3(-20f, 1f, -9.56f);
                car.SetActive(false);
                GameObject nam = CreateRoot(scene, "Nam");
                nam.transform.position = new Vector3(10f, 0f, 0f);

                PoliceArrestSequenceController controller =
                    PoliceArrestSequenceController.GetOrInstall(scene);
                Assert.IsTrue(
                    controller.RestoreTerminalArrestState(nam.transform));

                int completionCount = 0;
                bool result = false;
                Assert.IsTrue(controller.BeginArrest(
                    nam.transform,
                    arrived =>
                    {
                        completionCount++;
                        result = arrived;
                    }));

                Assert.AreEqual(1, completionCount);
                Assert.IsTrue(result);
                Assert.AreEqual(
                    PoliceArrestSequenceController.SequenceState.Arrived,
                    controller.State);
                Assert.AreEqual(-9.56f, car.transform.position.z, 0.001f);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void MissingExactRootCompletesWithFailureInsteadOfSoftlock()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject wrapper = CreateRoot(scene, "Wrapper");
                GameObject nestedCar = new GameObject(
                    PoliceArrestSequenceController.PoliceCarObjectName);
                nestedCar.transform.SetParent(wrapper.transform, false);
                GameObject nam = CreateRoot(scene, "Nam");

                PoliceArrestSequenceController controller =
                    PoliceArrestSequenceController.GetOrInstall(scene);
                bool callbackInvoked = false;
                bool callbackResult = true;

                LogAssert.Expect(
                    LogType.Error,
                    "[PoliceArrest] Cannot begin: exact inactive root " +
                    "'police_car' was not found in the scene.");
                Assert.IsFalse(controller.BeginArrest(
                    nam.transform,
                    arrived =>
                    {
                        callbackInvoked = true;
                        callbackResult = arrived;
                    }));

                Assert.IsTrue(callbackInvoked);
                Assert.IsFalse(callbackResult);
                Assert.AreEqual(
                    PoliceArrestSequenceController.SequenceState.Failed,
                    controller.State);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void TuningMatchesApprovedPoliceArrivalValues()
        {
            Assert.AreEqual(
                12f,
                PoliceArrestSequenceController.PoliceCarSpeed);
            Assert.AreEqual(
                4.1f,
                PoliceArrestSequenceController.PoliceCarStopOffset);
            Assert.AreEqual(
                15f,
                PoliceArrestSequenceController.PoliceCarTimeout);
        }

        private static GameObject CreateRoot(Scene scene, string name)
        {
            GameObject result = new GameObject(name);
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }
    }
}
