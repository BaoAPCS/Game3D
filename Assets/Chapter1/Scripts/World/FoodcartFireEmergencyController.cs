using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Coordinates the one-shot response to FoodcartFireVfx: Foodseller
    /// escapes, Henry and NPC_man run to the cart, and Fire Truck arrives.
    /// Henry movement itself remains owned by HenryChaseController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoodcartFireEmergencyController : MonoBehaviour
    {
        private const string FoodsellerObjectName = "Foodseller";
        private const string NpcManObjectName = "NPC_man";
        private const string FireTruckObjectName = "Fire Truck";

        [SerializeField, Min(0.1f)] private float fireTruckSpeed = 9.5f;
        [SerializeField, Min(0.5f)]
        private float fireTruckStopOffset = 5.5f;
        [SerializeField, Min(0.01f)]
        private float fireTruckArrivalThreshold = 0.1f;

        private Scene responseScene;
        private FoodcartGrillIgnitionTarget ignitionTarget;
        private GameObject fireVfx;
        private Transform foodcart;
        private Transform foodseller;
        private Transform npcMan;
        private Transform henry;
        private Transform fireTruck;
        private Vector3 fireTruckDestination;
        private HenryChaseController henryChaseController;
        private bool configured;
        private bool responseStarted;
        private bool henryResponsePending;
        private bool henryBusyWarningLogged;
        private bool fireTruckMoving;
        private float nextHenryResponseRetryAt;

        internal static FoodcartFireEmergencyController InstallForScene(
            Scene scene,
            Transform foodcart,
            FoodcartGrillIgnitionTarget ignitionTarget,
            GameObject fireVfx)
        {
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                foodcart == null ||
                ignitionTarget == null ||
                fireVfx == null)
            {
                return null;
            }

            FoodcartFireEmergencyController controller =
                foodcart.GetComponent<
                    FoodcartFireEmergencyController>();
            if (controller == null)
            {
                controller = foodcart.gameObject.AddComponent<
                    FoodcartFireEmergencyController>();
            }

            controller.Configure(
                scene,
                foodcart,
                ignitionTarget,
                fireVfx);
            return controller;
        }

        private void OnEnable()
        {
            SubscribeToIgnition();
        }

        private void OnDisable()
        {
            UnsubscribeFromIgnition();
        }

        private void Update()
        {
            if (!responseStarted &&
                fireVfx != null &&
                fireVfx.activeInHierarchy)
            {
                BeginEmergencyResponse();
            }

            if (fireTruckMoving)
            {
                UpdateFireTruck();
            }

            if (henryResponsePending &&
                Time.unscaledTime >= nextHenryResponseRetryAt)
            {
                nextHenryResponseRetryAt = Time.unscaledTime + 0.25f;
                TrySendHenryToFoodcart();
            }
        }

        private void Configure(
            Scene scene,
            Transform cart,
            FoodcartGrillIgnitionTarget target,
            GameObject targetFireVfx)
        {
            UnsubscribeFromIgnition();
            responseScene = scene;
            foodcart = cart;
            ignitionTarget = target;
            fireVfx = targetFireVfx;
            configured = true;
            ResolveSceneReferences();

            if (!responseStarted && fireTruck != null)
            {
                fireTruck.gameObject.SetActive(false);
            }

            SubscribeToIgnition();
            if (ignitionTarget.IsIgnited || fireVfx.activeInHierarchy)
            {
                BeginEmergencyResponse();
            }
        }

        private void SubscribeToIgnition()
        {
            if (!configured || ignitionTarget == null)
            {
                return;
            }

            ignitionTarget.Ignited -= HandleIgnited;
            ignitionTarget.Ignited += HandleIgnited;
        }

        private void UnsubscribeFromIgnition()
        {
            if (ignitionTarget != null)
            {
                ignitionTarget.Ignited -= HandleIgnited;
            }
        }

        private void HandleIgnited()
        {
            BeginEmergencyResponse();
        }

        private void BeginEmergencyResponse()
        {
            if (responseStarted || foodcart == null)
            {
                return;
            }

            responseStarted = true;
            ResolveSceneReferences();
            StartFoodsellerEscape();
            SendHenryToFoodcart();
            SendNpcManToFoodcart();
            StartFireTruckArrival();

            Debug.Log(
                "[FoodcartFire] Emergency response started.",
                this);
        }

        private void ResolveSceneReferences()
        {
            if (!responseScene.IsValid())
            {
                return;
            }

            foodseller ??= FoodcartEmergencyUtility.FindSceneTransform(
                responseScene,
                FoodsellerObjectName);
            npcMan ??= FoodcartEmergencyUtility.FindSceneTransform(
                responseScene,
                NpcManObjectName);
            henry ??= FoodcartEmergencyUtility.FindSceneTransform(
                responseScene,
                HenryTheftInteractable.HenryObjectName);
            fireTruck ??= FoodcartEmergencyUtility.FindSceneTransform(
                responseScene,
                FireTruckObjectName);
        }

        private void StartFoodsellerEscape()
        {
            if (foodseller == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] Foodseller was not found.",
                    this);
                return;
            }

            FoodsellerFireEscapeController escapeController =
                FoodsellerFireEscapeController.GetOrInstall(
                    foodseller.gameObject);
            if (escapeController == null ||
                !escapeController.BeginEscape(foodcart))
            {
                Debug.LogWarning(
                    "[FoodcartFire] Foodseller could not start escaping.",
                    foodseller);
            }
        }

        private void SendHenryToFoodcart()
        {
            if (henry == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] Henry was not found.",
                    this);
                return;
            }

            henryChaseController =
                henry.GetComponent<HenryChaseController>();
            if (henryChaseController == null)
            {
                henryChaseController = henry.gameObject.AddComponent<
                    HenryChaseController>();
            }

            henryResponsePending = true;
            TrySendHenryToFoodcart();
        }

        private void TrySendHenryToFoodcart()
        {
            if (!henryResponsePending ||
                henryChaseController == null ||
                foodcart == null)
            {
                return;
            }

            if (henryChaseController.HasCaughtPlayer ||
                henryChaseController.HasEscaped)
            {
                henryResponsePending = false;
                return;
            }

            if (!henryChaseController.CanStartDistraction)
            {
                if (!henryBusyWarningLogged)
                {
                    henryBusyWarningLogged = true;
                    Debug.LogWarning(
                        "[FoodcartFire] Henry is busy; the fire response " +
                        "will start as soon as his current state allows it.",
                        henry);
                }

                return;
            }

            if (!henryChaseController.BeginFoodcartDistraction(foodcart))
            {
                henryResponsePending = false;
                Debug.LogWarning(
                    "[FoodcartFire] Henry could not find a route to " +
                    "the food cart.",
                    henry);
                return;
            }

            henryResponsePending = false;
        }

        private void SendNpcManToFoodcart()
        {
            if (npcMan == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] NPC_man was not found.",
                    this);
                return;
            }

            NpcPatrol patrol = npcMan.GetComponent<NpcPatrol>();
            if (patrol == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] NPC_man has no NpcPatrol component.",
                    npcMan);
                return;
            }

            Bounds cartBounds =
                FoodcartEmergencyUtility.GetWorldBounds(foodcart);
            Vector3 destination = new Vector3(
                cartBounds.center.x + 1.25f,
                npcMan.position.y,
                cartBounds.min.z - 0.75f);
            if (!patrol.BeginEmergencyRun(destination))
            {
                Debug.LogWarning(
                    "[FoodcartFire] NPC_man could not run to the cart.",
                    npcMan);
            }
        }

        private void StartFireTruckArrival()
        {
            if (fireTruck == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] Fire Truck was not found.",
                    this);
                return;
            }

            fireTruck.gameObject.SetActive(true);
            Rigidbody[] rigidbodies =
                fireTruck.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody body = rigidbodies[i];
                if (body == null)
                {
                    continue;
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }

            float approachSide = fireTruck.position.x >= foodcart.position.x
                ? 1f
                : -1f;
            fireTruckDestination = new Vector3(
                foodcart.position.x +
                approachSide * fireTruckStopOffset,
                fireTruck.position.y,
                fireTruck.position.z);
            fireTruckMoving = Mathf.Abs(
                fireTruck.position.x - fireTruckDestination.x) >
                fireTruckArrivalThreshold;
        }

        private void UpdateFireTruck()
        {
            if (fireTruck == null)
            {
                fireTruckMoving = false;
                return;
            }

            Vector3 currentPosition = fireTruck.position;
            float nextX = Mathf.MoveTowards(
                currentPosition.x,
                fireTruckDestination.x,
                fireTruckSpeed * Time.deltaTime);
            fireTruck.position = new Vector3(
                nextX,
                fireTruckDestination.y,
                fireTruckDestination.z);

            if (Mathf.Abs(nextX - fireTruckDestination.x) <=
                fireTruckArrivalThreshold)
            {
                fireTruck.position = fireTruckDestination;
                fireTruckMoving = false;
                Debug.Log(
                    "[FoodcartFire] Fire Truck arrived at the food cart.",
                    fireTruck);
            }
        }

        private void OnValidate()
        {
            fireTruckSpeed = Mathf.Max(0.1f, fireTruckSpeed);
            fireTruckStopOffset = Mathf.Max(0.5f, fireTruckStopOffset);
            fireTruckArrivalThreshold = Mathf.Max(
                0.01f,
                fireTruckArrivalThreshold);
        }
    }

    internal static class FoodcartEmergencyUtility
    {
        internal static Transform FindSceneTransform(
            Scene scene,
            string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] hierarchy =
                    roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < hierarchy.Length; j++)
                {
                    Transform candidate = hierarchy[j];
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

        internal static Bounds GetWorldBounds(Transform root)
        {
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            bool hasBounds = false;
            Collider[] colliders =
                root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null ||
                    !collider.enabled ||
                    collider.isTrigger)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            if (hasBounds)
            {
                return bounds;
            }

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (hasBounds)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return bounds;
        }
    }
}
