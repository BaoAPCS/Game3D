using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public enum MeleeHitboxLimb
    {
        None = 0,
        LeftHand = 1,
        RightHand = 2,
        LeftFoot = 3,
        RightFoot = 4
    }

    public enum MeleeHitboxRigPreset
    {
        Auto = 0,
        Nam = 1,
        Henry = 2
    }

    public struct MeleeHitboxPose
    {
        public Vector3 Center;
        public Vector3 HalfExtents;
        public Quaternion Rotation;
    }

    /// <summary>
    /// Creates disabled BoxColliders on the attacking bones and exposes their
    /// animated world-space poses to the melee damage query. The colliders are
    /// authoring volumes only: they never participate in Unity trigger physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeHitboxRig : MonoBehaviour
    {
        private const string NamLeftHandBone = "mixamorig:LeftHand";
        private const string NamRightHandBone = "mixamorig:RightHand";
        private const string NamLeftFootBone = "mixamorig:LeftFoot";
        private const string NamRightFootBone = "mixamorig:RightFoot";
        private const string HenryRightHandBone = "CC_Base_R_Hand_086";
        private const string HenryRightFootBone = "CC_Base_R_Foot_024";

        [SerializeField] private MeleeHitboxRigPreset preset =
            MeleeHitboxRigPreset.Auto;
        [SerializeField] private Animator humanoidAnimator;
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly Dictionary<MeleeHitboxLimb, BoxCollider> hitboxes =
            new Dictionary<MeleeHitboxLimb, BoxCollider>();
        private bool configured;
        private bool missingRigLogged;

        public MeleeHitboxRigPreset Preset => preset;
        public bool IsConfigured => configured;

        private void Awake()
        {
            EnsureConfigured();
        }

        public bool ConfigureForNam(Animator animator)
        {
            preset = MeleeHitboxRigPreset.Nam;
            humanoidAnimator = animator;
            configured = false;
            hitboxes.Clear();
            return EnsureConfigured();
        }

        public bool ConfigureForHenry()
        {
            preset = MeleeHitboxRigPreset.Henry;
            humanoidAnimator = null;
            configured = false;
            hitboxes.Clear();
            return EnsureConfigured();
        }

        public bool EnsureConfigured()
        {
            if (configured && hitboxes.Count > 0)
            {
                return true;
            }

            hitboxes.Clear();
            MeleeHitboxRigPreset resolvedPreset = ResolvePreset();
            bool success = resolvedPreset == MeleeHitboxRigPreset.Nam
                ? ConfigureNamHitboxes()
                : resolvedPreset == MeleeHitboxRigPreset.Henry &&
                  ConfigureHenryHitboxes();

            configured = success;
            if (!success && !missingRigLogged)
            {
                missingRigLogged = true;
                Debug.LogError(
                    $"[MeleeHitboxRig] Không tìm thấy bộ xương phù hợp trên '{name}'.",
                    this);
            }

            return success;
        }

        public bool TryGetHitbox(
            MeleeHitboxLimb limb,
            out BoxCollider hitbox)
        {
            if (!EnsureConfigured())
            {
                hitbox = null;
                return false;
            }

            return hitboxes.TryGetValue(limb, out hitbox) && hitbox != null;
        }

        public bool TryGetPose(
            MeleeHitboxLimb limb,
            out MeleeHitboxPose pose)
        {
            if (!TryGetHitbox(limb, out BoxCollider hitbox))
            {
                pose = default;
                return false;
            }

            Transform hitboxTransform = hitbox.transform;
            Vector3 scale = Abs(hitboxTransform.lossyScale);
            pose = new MeleeHitboxPose
            {
                Center = hitboxTransform.TransformPoint(hitbox.center),
                HalfExtents = Vector3.Scale(hitbox.size * 0.5f, scale),
                Rotation = hitboxTransform.rotation
            };
            return true;
        }

        private MeleeHitboxRigPreset ResolvePreset()
        {
            if (preset != MeleeHitboxRigPreset.Auto)
            {
                return preset;
            }

            if (ResolveHumanoidAnimator() != null &&
                humanoidAnimator.isHuman)
            {
                return MeleeHitboxRigPreset.Nam;
            }

            return FindChildRecursive(transform, HenryRightFootBone) != null
                ? MeleeHitboxRigPreset.Henry
                : MeleeHitboxRigPreset.Auto;
        }

        private bool ConfigureNamHitboxes()
        {
            Animator targetAnimator = ResolveHumanoidAnimator();
            if (targetAnimator == null || !targetAnimator.isHuman)
            {
                return false;
            }

            bool leftHand = EnsureHitbox(
                MeleeHitboxLimb.LeftHand,
                ResolveHumanoidBone(
                    targetAnimator,
                    HumanBodyBones.LeftHand,
                    NamLeftHandBone),
                new Vector3(0f, 0.15f, 0f),
                new Vector3(0.20f, 0.32f, 0.16f));
            bool rightHand = EnsureHitbox(
                MeleeHitboxLimb.RightHand,
                ResolveHumanoidBone(
                    targetAnimator,
                    HumanBodyBones.RightHand,
                    NamRightHandBone),
                new Vector3(0f, 0.15f, 0f),
                new Vector3(0.20f, 0.32f, 0.16f));
            bool leftFoot = EnsureHitbox(
                MeleeHitboxLimb.LeftFoot,
                ResolveHumanoidBone(
                    targetAnimator,
                    HumanBodyBones.LeftFoot,
                    NamLeftFootBone),
                new Vector3(0f, 0.24f, 0f),
                new Vector3(0.22f, 0.50f, 0.18f));
            bool rightFoot = EnsureHitbox(
                MeleeHitboxLimb.RightFoot,
                ResolveHumanoidBone(
                    targetAnimator,
                    HumanBodyBones.RightFoot,
                    NamRightFootBone),
                new Vector3(0f, 0.24f, 0f),
                new Vector3(0.22f, 0.50f, 0.18f));

            return leftHand && rightHand && leftFoot && rightFoot;
        }

        private bool ConfigureHenryHitboxes()
        {
            Transform rightHand =
                FindChildRecursive(transform, HenryRightHandBone);
            Transform rightFoot =
                FindChildRecursive(transform, HenryRightFootBone);
            bool handReady = EnsureHitbox(
                MeleeHitboxLimb.RightHand,
                rightHand,
                new Vector3(0f, 7f, 0f),
                new Vector3(12f, 18f, 12f));
            bool footReady = EnsureHitbox(
                MeleeHitboxLimb.RightFoot,
                rightFoot,
                new Vector3(0f, 9f, 1f),
                new Vector3(14f, 28f, 10f));
            return handReady && footReady;
        }

        private Animator ResolveHumanoidAnimator()
        {
            if (humanoidAnimator == null)
            {
                humanoidAnimator = GetComponentInChildren<Animator>(true);
            }

            return humanoidAnimator;
        }

        private Transform ResolveHumanoidBone(
            Animator targetAnimator,
            HumanBodyBones humanBone,
            string fallbackName)
        {
            Transform bone = targetAnimator.GetBoneTransform(humanBone);
            return bone != null
                ? bone
                : FindChildRecursive(transform, fallbackName);
        }

        private bool EnsureHitbox(
            MeleeHitboxLimb limb,
            Transform bone,
            Vector3 defaultCenter,
            Vector3 defaultSize)
        {
            if (bone == null)
            {
                return false;
            }

            string hitboxName = $"Hitbox_{limb}";
            Transform hitboxTransform = FindDirectChild(bone, hitboxName);
            BoxCollider hitbox;
            if (hitboxTransform == null)
            {
                GameObject hitboxObject = new GameObject(hitboxName);
                hitboxObject.layer = gameObject.layer;
                hitboxTransform = hitboxObject.transform;
                hitboxTransform.SetParent(bone, false);
                hitbox = hitboxObject.AddComponent<BoxCollider>();
                hitbox.center = defaultCenter;
                hitbox.size = defaultSize;
            }
            else
            {
                hitbox = hitboxTransform.GetComponent<BoxCollider>();
                if (hitbox == null)
                {
                    hitbox = hitboxTransform.gameObject
                        .AddComponent<BoxCollider>();
                    hitbox.center = defaultCenter;
                    hitbox.size = defaultSize;
                }
            }

            hitbox.isTrigger = true;
            hitbox.enabled = false;
            hitboxes[limb] = hitbox;
            return true;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(
                        child.name,
                        childName,
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform parent,
            string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(
                        child.name,
                        childName,
                        StringComparison.Ordinal))
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            BoxCollider[] colliders =
                GetComponentsInChildren<BoxCollider>(true);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            for (int i = 0; i < colliders.Length; i++)
            {
                BoxCollider collider = colliders[i];
                if (collider == null ||
                    !collider.name.StartsWith(
                        "Hitbox_",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Gizmos.matrix = collider.transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.9f);
                Gizmos.DrawWireCube(collider.center, collider.size);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
