using System.Collections;
using UnityEngine;

namespace DormitoryMystery.Chapter3
{
    [DisallowMultipleComponent]
    public sealed class Chapter3HingedDoor : MonoBehaviour
    {
        public const string HingeName = "Door_V3_Hinge";

        [SerializeField, Min(0.05f)] private float openingDuration = 0.6f;
        [SerializeField, Range(45f, 120f)] private float openingAngle = 90f;

        private Transform hinge;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Collider[] leafColliders;
        private bool[] colliderStates;

        public bool IsOpening { get; private set; }
        public bool IsOpen { get; private set; }
        public Transform Hinge => hinge;

        public bool Configure(Transform roomInside)
        {
            if (hinge != null)
                return true;

            hinge = transform.Find(HingeName);
            Transform leaf = hinge != null
                ? hinge.Find("Door")
                : transform.Find("Door");
            if (leaf == null) leaf = transform.Find("Door");
            if (leaf == null || roomInside == null)
                return false;

            if (hinge == null)
            {
                // Match the left-edge hinge baked into the hospital scene.
                // Only the leaf moves; Door_Frame and the root trigger stay put.
                hinge = new GameObject(HingeName).transform;
                hinge.SetParent(transform, false);
                hinge.localPosition = new Vector3(
                    0.5315362f, 0.01273104f, -0.05360961f);
            }

            // Prefab children cannot be reparented in Edit mode without
            // unpacking the hospital. Keep that override runtime-only.
            if (leaf.parent != hinge) leaf.SetParent(hinge, true);

            closedRotation = hinge.localRotation;
            Renderer leafRenderer = leaf.GetComponentInChildren<Renderer>();
            Vector3 center = leafRenderer != null
                ? leafRenderer.bounds.center
                : leaf.position;
            Vector3 offset = center - hinge.position;
            Vector3 inward = Vector3.ProjectOnPlane(
                roomInside.position - center, hinge.up);
            Vector3 positive = Quaternion.AngleAxis(openingAngle, hinge.up) * offset;
            Vector3 negative = Quaternion.AngleAxis(-openingAngle, hinge.up) * offset;
            float signedAngle = Vector3.Dot(positive, inward) >
                                Vector3.Dot(negative, inward)
                ? openingAngle : -openingAngle;
            openRotation = closedRotation * Quaternion.Euler(0f, signedAngle, 0f);
            leafColliders = leaf.GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[leafColliders.Length];
            return true;
        }

        public bool TryOpen()
        {
            if (!isActiveAndEnabled || hinge == null || IsOpening || IsOpen)
                return false;

            IsOpening = true;
            for (int i = 0; i < leafColliders.Length; i++)
            {
                colliderStates[i] = leafColliders[i] != null && leafColliders[i].enabled;
                if (leafColliders[i] != null)
                    leafColliders[i].enabled = false;
            }
            StartCoroutine(OpenLeaf());
            return true;
        }

        private IEnumerator OpenLeaf()
        {
            float elapsed = 0f;
            while (elapsed < openingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openingDuration);
                hinge.localRotation = Quaternion.Slerp(
                    closedRotation, openRotation, t * t * (3f - 2f * t));
                yield return null;
            }
            FinishOpening();
        }

        private void FinishOpening()
        {
            if (hinge != null)
                hinge.localRotation = openRotation;
            IsOpening = false;
            IsOpen = true;
            for (int i = 0; i < leafColliders.Length; i++)
            {
                if (leafColliders[i] != null)
                    leafColliders[i].enabled = colliderStates[i];
            }
            Physics.SyncTransforms();
        }

        private void OnDisable()
        {
            if (!IsOpening)
                return;
            StopAllCoroutines();
            FinishOpening();
        }
    }
}
