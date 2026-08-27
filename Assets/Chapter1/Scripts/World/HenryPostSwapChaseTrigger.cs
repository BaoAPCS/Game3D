using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-installs onto the scene's ChaseTrigger. The trigger remains
    /// inert until the Henry battery swap has completed, then starts exactly
    /// one normal Henry chase when the player crosses it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class HenryPostSwapChaseTrigger : MonoBehaviour
    {
        private const string TriggerObjectName = "ChaseTrigger";
        private const string DetectionMessage =
            "Henry phát hiện ra bạn đã đánh tráo ắc quy, " +
            "hãy mau chóng chạy thoát";

        [SerializeField] private BoxCollider triggerCollider;

        private HenryChaseController chaseController;
        private bool consumed;
        private bool missingHenryLogged;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Mission2HeistProgress.EnsureScene(scene);
            Transform trigger = FindSceneTransform(
                scene,
                TriggerObjectName);
            if (trigger == null)
            {
                return;
            }

            BoxCollider boxCollider =
                trigger.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                Debug.LogWarning(
                    "[Henry] ChaseTrigger does not have a BoxCollider.",
                    trigger);
                return;
            }

            boxCollider.isTrigger = true;
            if (!trigger.TryGetComponent(
                    out HenryPostSwapChaseTrigger chaseTrigger))
            {
                chaseTrigger = trigger.gameObject.AddComponent<
                    HenryPostSwapChaseTrigger>();
            }

            chaseTrigger.Configure(boxCollider);
        }

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<BoxCollider>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryStartPostSwapChase(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // Also handles the edge case where the swap completes while the
            // player is already touching the edge of the trigger volume.
            TryStartPostSwapChase(other);
        }

        private void Configure(BoxCollider boxCollider)
        {
            triggerCollider = boxCollider;
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void TryStartPostSwapChase(Collider other)
        {
            if (consumed ||
                other == null ||
                !Mission2HeistProgress.HasHenryBattery ||
                Mission2HeistProgress.HasDeliveredEquipment)
            {
                return;
            }

            Transform player = ResolvePlayer(other);
            if (player == null)
            {
                return;
            }

            ResolveHenryChaseController();
            if (chaseController == null)
            {
                if (!missingHenryLogged)
                {
                    missingHenryLogged = true;
                    Debug.LogWarning(
                        "[Henry] ChaseTrigger could not find Henry.",
                        this);
                }

                return;
            }

            if (!chaseController.BeginPostSwapChase(player))
            {
                return;
            }

            consumed = true;
            Chapter1EventBus.RaiseUrgentNotification(DetectionMessage);
        }

        private void ResolveHenryChaseController()
        {
            if (chaseController != null)
            {
                return;
            }

            Transform henry = FindSceneTransform(
                gameObject.scene,
                HenryTheftInteractable.HenryObjectName);
            if (henry == null)
            {
                return;
            }

            chaseController = henry.GetComponent<HenryChaseController>();
            if (chaseController == null)
            {
                chaseController = henry.gameObject.AddComponent<
                    HenryChaseController>();
            }
        }

        private static Transform ResolvePlayer(Collider other)
        {
            Chapter1PlayerMotor motor =
                other.GetComponentInParent<Chapter1PlayerMotor>();
            if (motor != null)
            {
                return motor.transform;
            }

            Chapter1InputReader inputReader =
                other.GetComponentInParent<Chapter1InputReader>();
            return inputReader != null ? inputReader.transform : null;
        }

        private static Transform FindSceneTransform(
            Scene scene,
            string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] hierarchy =
                    roots[rootIndex]
                        .GetComponentsInChildren<Transform>(true);
                for (int childIndex = 0;
                     childIndex < hierarchy.Length;
                     childIndex++)
                {
                    Transform candidate = hierarchy[childIndex];
                    if (candidate != null &&
                        string.Equals(
                            candidate.name,
                            objectName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
