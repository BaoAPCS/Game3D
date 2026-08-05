using System.Collections;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public enum AudioSeparatorMixerButtonType
    {
        PlayStop,
        Save,
        Reset
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AudioSeparatorMixerButton : MonoBehaviour
    {
        [SerializeField] private AudioSeparatorMixerButtonType buttonType;
        [SerializeField] private AudioSeparatorMixerController mixerController;
        [SerializeField] private Transform buttonTransform;
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.008f, 0f);
        [SerializeField, Min(0.01f)] private float pressSeconds = 0.08f;

        private Vector3 restLocalPosition;
        private Color originalColor = Color.white;
        private bool hasOriginalColor;
        private bool isAnimating;

        public AudioSeparatorMixerButtonType ButtonType => buttonType;

        private void Awake()
        {
            ResolveReferences();
            restLocalPosition = buttonTransform != null ? buttonTransform.localPosition : transform.localPosition;
            CacheColor();
        }

        private void OnValidate()
        {
            pressSeconds = Mathf.Max(0.01f, pressSeconds);
            ResolveReferences();
        }

        public void Configure(AudioSeparatorMixerController controller, AudioSeparatorMixerButtonType type)
        {
            mixerController = controller;
            buttonType = type;
            ResolveReferences();
            restLocalPosition = buttonTransform != null ? buttonTransform.localPosition : transform.localPosition;
        }

        private void OnMouseEnter()
        {
            if (mixerController != null && mixerController.IsSessionOpen)
            {
                SetHighlighted(true);
            }
        }

        private void OnMouseExit()
        {
            SetHighlighted(false);
        }

        private void OnMouseDown()
        {
            if (isAnimating || mixerController == null || !mixerController.IsSessionOpen)
            {
                return;
            }

            StartCoroutine(PressRoutine());
            mixerController.HandleMixerButton(buttonType);
        }

        public void SetAttention(bool attention)
        {
            if (highlightRenderer == null)
            {
                return;
            }

            CacheColor();
            highlightRenderer.material.color = attention ? Color.Lerp(originalColor, Color.red, 0.45f) : originalColor;
        }

        private IEnumerator PressRoutine()
        {
            isAnimating = true;
            Transform target = buttonTransform != null ? buttonTransform : transform;
            target.localPosition = restLocalPosition + pressedLocalOffset;
            yield return new WaitForSecondsRealtime(pressSeconds);
            target.localPosition = restLocalPosition;
            isAnimating = false;
        }

        private void ResolveReferences()
        {
            if (buttonTransform == null)
            {
                buttonTransform = transform;
            }

            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponentInChildren<Renderer>(true);
            }

            if (mixerController == null)
            {
                mixerController = GetComponentInParent<AudioSeparatorMixerController>();
            }
        }

        private void CacheColor()
        {
            if (hasOriginalColor || highlightRenderer == null)
            {
                return;
            }

            originalColor = highlightRenderer.sharedMaterial != null ? highlightRenderer.sharedMaterial.color : Color.white;
            hasOriginalColor = true;
        }

        private void SetHighlighted(bool highlighted)
        {
            if (highlightRenderer == null)
            {
                return;
            }

            CacheColor();
            highlightRenderer.material.color = highlighted ? Color.Lerp(originalColor, Color.white, 0.35f) : originalColor;
        }
    }
}
