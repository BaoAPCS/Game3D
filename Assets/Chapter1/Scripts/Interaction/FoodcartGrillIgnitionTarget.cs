using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    /// <summary>
    /// Runtime-installed ignition target for the food cart grill. Only a
    /// thrown PetrolCan touching the world-facing top of the grill collider
    /// can enable the cart fire VFX.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoodcartGrillIgnitionTarget : MonoBehaviour
    {
        private const string FoodcartObjectName =
            "avika_street_food_cart";
        private const string GrillObjectName = "grill";
        private const string FireVfxObjectName = "FoodcartFireVfx";

        [SerializeField] private BoxCollider grillCollider;
        [SerializeField] private GameObject fireVfx;
        [SerializeField, Range(0.01f, 0.35f)]
        private float topFaceDepthRatio = 0.12f;
        [SerializeField, Range(0f, 1f)]
        private float minimumTopNormalAlignment = 0.55f;

        private bool configured;
        private bool ignited;

        internal bool IsIgnited => ignited;
        internal event Action Ignited;

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

        internal bool TryIgnite(
            Collider contactedCollider,
            ContactPoint contact)
        {
            if (!configured ||
                ignited ||
                grillCollider == null ||
                fireVfx == null ||
                contactedCollider != grillCollider ||
                !IsTopSurfaceContact(contact.point, contact.normal))
            {
                return false;
            }

            ignited = true;
            fireVfx.SetActive(true);
            Ignited?.Invoke();
            Debug.Log(
                "[FoodcartFire] PetrolCan hit the top of the grill; " +
                "FoodcartFireVfx enabled.",
                this);
            return true;
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Transform foodcart = FindSceneTransform(
                scene,
                FoodcartObjectName);
            if (foodcart == null)
            {
                return;
            }

            BoxCollider targetCollider = null;
            GameObject targetFireVfx = null;
            Transform[] hierarchy =
                foodcart.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < hierarchy.Length; i++)
            {
                Transform candidate = hierarchy[i];
                if (candidate == null)
                {
                    continue;
                }

                if (targetCollider == null &&
                    string.Equals(
                        candidate.name,
                        GrillObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    targetCollider = candidate.GetComponent<BoxCollider>();
                }

                if (targetFireVfx == null &&
                    string.Equals(
                        candidate.name,
                        FireVfxObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    targetFireVfx = candidate.gameObject;
                }
            }

            if (targetCollider == null || targetFireVfx == null)
            {
                Debug.LogWarning(
                    "[FoodcartFire] Could not find both grill BoxCollider " +
                    "and FoodcartFireVfx under avika_street_food_cart.",
                    foodcart);
                return;
            }

            FoodcartGrillIgnitionTarget target =
                targetCollider.GetComponent<
                    FoodcartGrillIgnitionTarget>();
            if (target == null)
            {
                target = targetCollider.gameObject.AddComponent<
                    FoodcartGrillIgnitionTarget>();
            }

            target.Configure(targetCollider, targetFireVfx);
            FoodcartFireEmergencyController.InstallForScene(
                scene,
                foodcart,
                target,
                targetFireVfx);
        }

        private static Transform FindSceneTransform(
            Scene scene,
            string objectName)
        {
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

        private void Configure(
            BoxCollider targetCollider,
            GameObject targetFireVfx)
        {
            grillCollider = targetCollider;
            fireVfx = targetFireVfx;
            if (configured)
            {
                return;
            }

            configured = true;
            ignited = false;
            fireVfx.SetActive(false);
        }

        private bool IsTopSurfaceContact(
            Vector3 worldPoint,
            Vector3 worldNormal)
        {
            Transform colliderTransform = grillCollider.transform;
            Matrix4x4 localToWorld = colliderTransform.localToWorldMatrix;
            Vector3 worldAxisX = localToWorld.MultiplyVector(Vector3.right);
            Vector3 worldAxisY = localToWorld.MultiplyVector(Vector3.up);
            Vector3 worldAxisZ = localToWorld.MultiplyVector(Vector3.forward);

            float xAlignment = NormalizedUpAlignment(worldAxisX);
            float yAlignment = NormalizedUpAlignment(worldAxisY);
            float zAlignment = NormalizedUpAlignment(worldAxisZ);

            int topAxis = 0;
            Vector3 topAxisWorld = worldAxisX;
            float strongestAlignment = Mathf.Abs(xAlignment);
            if (Mathf.Abs(yAlignment) > strongestAlignment)
            {
                topAxis = 1;
                topAxisWorld = worldAxisY;
                strongestAlignment = Mathf.Abs(yAlignment);
            }

            if (Mathf.Abs(zAlignment) > strongestAlignment)
            {
                topAxis = 2;
                topAxisWorld = worldAxisZ;
            }

            if (topAxisWorld.sqrMagnitude <= Mathf.Epsilon ||
                worldNormal.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            topAxisWorld.Normalize();
            float topDirectionSign =
                Vector3.Dot(topAxisWorld, Vector3.up) >= 0f ? 1f : -1f;
            Vector3 worldTopNormal = topAxisWorld * topDirectionSign;
            float normalAlignment = Mathf.Abs(Vector3.Dot(
                worldNormal.normalized,
                worldTopNormal));
            if (normalAlignment < minimumTopNormalAlignment)
            {
                return false;
            }

            Vector3 localPoint =
                colliderTransform.InverseTransformPoint(worldPoint);
            Vector3 center = grillCollider.center;
            Vector3 size = grillCollider.size;
            float pointCoordinate = GetAxis(localPoint, topAxis);
            float centerCoordinate = GetAxis(center, topAxis);
            float axisSize = Mathf.Abs(GetAxis(size, topAxis));
            float topCoordinate = centerCoordinate +
                                  topDirectionSign * axisSize * 0.5f;
            float faceTolerance = Mathf.Max(
                0.005f,
                axisSize * topFaceDepthRatio);
            if (Mathf.Abs(pointCoordinate - topCoordinate) > faceTolerance)
            {
                return false;
            }

            for (int axis = 0; axis < 3; axis++)
            {
                if (axis == topAxis)
                {
                    continue;
                }

                float halfExtent =
                    Mathf.Abs(GetAxis(size, axis)) * 0.5f;
                float boundsTolerance = Mathf.Max(
                    0.005f,
                    halfExtent * 0.05f);
                float offset = Mathf.Abs(
                    GetAxis(localPoint, axis) - GetAxis(center, axis));
                if (offset > halfExtent + boundsTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float NormalizedUpAlignment(Vector3 worldAxis)
        {
            if (worldAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Vector3.Dot(worldAxis.normalized, Vector3.up);
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
        }

        private void OnValidate()
        {
            topFaceDepthRatio = Mathf.Clamp(
                topFaceDepthRatio,
                0.01f,
                0.35f);
            minimumTopNormalAlignment = Mathf.Clamp01(
                minimumTopNormalAlignment);
        }
    }
}
