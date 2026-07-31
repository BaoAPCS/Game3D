using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    public static class HenryIdleAnimationAutoplay
    {
        private const string HenryObjectName =
            "henry_animated_cartoon_character";

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
            ConfigureHenryIdleAnimation(scene);
        }

        private static void ConfigureHenryIdleAnimation(Scene scene)
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
                        HenryObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                HoldInitialPose(candidate);
            }
        }

        private static void HoldInitialPose(Transform henryRoot)
        {
            Animation legacyAnimation =
                henryRoot.GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null || legacyAnimation.clip == null)
            {
                Debug.LogWarning(
                    "[Henry] Không tìm thấy animation gốc để giữ tư thế đứng.",
                    henryRoot);
                return;
            }

            legacyAnimation.playAutomatically = false;
            legacyAnimation.Stop();
            legacyAnimation.clip.SampleAnimation(
                legacyAnimation.gameObject,
                0f);
        }
    }
}
