using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter1
{
    public static class SceneCharacterAnimationAutoplay
    {
        private readonly struct CharacterSetup
        {
            public CharacterSetup(
                string objectName,
                string controllerResourcePath,
                string stateName)
            {
                ObjectName = objectName;
                ControllerResourcePath = controllerResourcePath;
                StateName = stateName;
            }

            public string ObjectName { get; }
            public string ControllerResourcePath { get; }
            public string StateName { get; }
        }

        private static readonly CharacterSetup[] CharacterSetups =
        {
            new CharacterSetup(
                "James",
                "James/James_Auto",
                "Base Layer.James_Idle"),
            new CharacterSetup(
                "David",
                "David/David_Auto",
                "Base Layer.David_Idle"),
            new CharacterSetup(
                "Lewis",
                "Lewis/Lewis_Auto",
                "Base Layer.Lewis_Idle")
        };

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
            for (int i = 0; i < CharacterSetups.Length; i++)
            {
                SetupCharacter(scene, CharacterSetups[i]);
            }
        }

        private static void SetupCharacter(
            Scene scene,
            CharacterSetup setup)
        {
            Transform characterRoot =
                FindCharacterRoot(scene, setup.ObjectName);
            if (characterRoot == null)
            {
                return;
            }

            Animator animator =
                characterRoot.GetComponent<Animator>() ??
                characterRoot.GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                animator =
                    characterRoot.gameObject.AddComponent<Animator>();
            }

            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(
                    setup.ControllerResourcePath);
            if (controller == null)
            {
                Debug.LogWarning(
                    $"[{setup.ObjectName}] Khong load duoc Animator Controller.",
                    characterRoot);
                return;
            }

            if (controller.animationClips == null ||
                controller.animationClips.Length == 0)
            {
                Debug.LogWarning(
                    $"[{setup.ObjectName}] Animator Controller khong co animation clip.",
                    characterRoot);
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            animator.Rebind();

            int stateHash = Animator.StringToHash(setup.StateName);
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogWarning(
                    $"[{setup.ObjectName}] Animator Controller khong co state can phat.",
                    characterRoot);
                return;
            }

            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
            Debug.Log(
                $"[{setup.ObjectName}] Dang phat animation bang Animator Controller.",
                characterRoot);
        }

        private static Transform FindCharacterRoot(
            Scene scene,
            string objectName)
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
                        objectName,
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
