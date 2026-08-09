using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    public static class JamesAnimationAutoplay
    {
        private const string JamesObjectName = "James";
        private const string ControllerResourcePath = "James/James_Auto";
        private const string IdleStateName = "Base Layer.James_Idle";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadMode)
        {
            Transform jamesRoot = FindJamesRoot(scene);
            if (jamesRoot == null)
            {
                return;
            }

            Animator animator =
                jamesRoot.GetComponent<Animator>() ??
                jamesRoot.GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                // James.fbx does not create an Animator component on the
                // scene instance, so install it on the model root at runtime.
                animator = jamesRoot.gameObject.AddComponent<Animator>();
            }

            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(
                    ControllerResourcePath);
            if (controller == null)
            {
                Debug.LogWarning(
                    "[James] Khong load duoc Animator Controller James_Auto.",
                    jamesRoot);
                return;
            }

            if (controller.animationClips == null ||
                controller.animationClips.Length == 0)
            {
                Debug.LogWarning(
                    "[James] Animator Controller James_Auto khong co animation clip.",
                    jamesRoot);
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            animator.Rebind();

            int idleStateHash = Animator.StringToHash(IdleStateName);
            if (!animator.HasState(0, idleStateHash))
            {
                Debug.LogWarning(
                    "[James] Animator Controller khong co state James_Idle.",
                    jamesRoot);
                return;
            }

            animator.Play(idleStateHash, 0, 0f);
            animator.Update(0f);
            Debug.Log(
                "[James] Dang phat animation James_Idle bang James_Auto.",
                jamesRoot);
        }

        private static Transform FindJamesRoot(Scene scene)
        {
            Transform[] sceneTransforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);

            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                if (candidate == null ||
                    candidate.gameObject.scene != scene ||
                    !string.Equals(
                        candidate.name,
                        JamesObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
