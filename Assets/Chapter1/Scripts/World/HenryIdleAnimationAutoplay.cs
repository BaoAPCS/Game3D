using System;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public static class HenryIdleAnimationAutoplay
    {
        private const string HenryObjectName =
            "henry_animated_cartoon_character";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConfigureHenryIdleAnimation()
        {
            Transform[] sceneTransforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Exclude);

            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                if (candidate == null ||
                    !string.Equals(
                        candidate.name,
                        HenryObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PlayLoopingIdle(candidate);
            }
        }

        private static void PlayLoopingIdle(Transform henryRoot)
        {
            Animation legacyAnimation =
                henryRoot.GetComponentInChildren<Animation>(true);
            if (legacyAnimation == null || legacyAnimation.clip == null)
            {
                Debug.LogWarning(
                    "[HenryIdleAnimationAutoplay] Henry does not have " +
                    "a Legacy Animation clip assigned.",
                    henryRoot);
                return;
            }

            legacyAnimation.playAutomatically = true;
            legacyAnimation.wrapMode = WrapMode.Loop;

            foreach (AnimationState state in legacyAnimation)
            {
                state.wrapMode = WrapMode.Loop;
            }

            legacyAnimation.Play(legacyAnimation.clip.name);
        }
    }
}
