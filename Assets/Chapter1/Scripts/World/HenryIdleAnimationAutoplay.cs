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

                PlayOriginalIdle(candidate);
            }
        }

        private static void PlayOriginalIdle(Transform henryRoot)
        {
            HenryRunAnimationPlayer animationPlayer =
                henryRoot.GetComponent<HenryRunAnimationPlayer>();
            if (animationPlayer == null)
            {
                animationPlayer = henryRoot.gameObject.AddComponent<
                    HenryRunAnimationPlayer>();
            }

            if (!animationPlayer.PlayIdle())
            {
                Debug.LogWarning(
                    "[Henry] Không thể phát animation đứng yên gốc.",
                    henryRoot);
            }
        }
    }
}
