using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class AudioStemFader : MonoBehaviour
    {
        [SerializeField] private LanAudioStemId stemId;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Transform handleTransform;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Vector3 localMinPosition = new Vector3(0f, -0.045f, 0f);
        [SerializeField] private Vector3 localMaxPosition = new Vector3(0f, 0.045f, 0f);
        [SerializeField, Range(0f, 1f)] private float normalizedValue = 1f;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private bool isVoiceFader;
        [SerializeField] private AudioSeparatorMixerController mixerController;

        private Color originalHighlightColor = Color.white;
        private bool hasOriginalHighlightColor;
        private Camera dragCamera;
        private Plane dragPlane;

        public LanAudioStemId StemId => stemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? LanAudioRecordingCatalog.GetStemDisplayName(stemId) : displayName;
        public AudioSource AudioSource => audioSource;
        public float NormalizedValue => normalizedValue;
        public bool IsVoiceFader => isVoiceFader;

        private void Awake()
        {
            ResolveReferences();
            ApplyNormalizedValue(normalizedValue, false);
            CacheHighlightColor();
            SetHighlighted(false);
        }

        private void OnValidate()
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            ResolveReferences();
            ApplyNormalizedValue(normalizedValue, false);
        }

        public void Configure(
            AudioSeparatorMixerController controller,
            LanAudioStemId stem,
            string stemDisplayName,
            AudioSource source,
            bool voiceFader)
        {
            mixerController = controller;
            stemId = stem;
            displayName = stemDisplayName;
            audioSource = source;
            isVoiceFader = voiceFader;
            ResolveReferences();
            ApplyNormalizedValue(normalizedValue, false);
        }

        public void SetNormalizedValue(float value)
        {
            ApplyNormalizedValue(value, true);
        }

        public void SetClip(AudioClip clip)
        {
            ResolveReferences();
            if (audioSource != null)
            {
                audioSource.clip = clip;
            }
        }

        private void OnMouseEnter()
        {
            if (mixerController != null && mixerController.IsSessionOpen)
            {
                SetHighlighted(true);
                mixerController.ShowHoverLabel(DisplayName);
            }
        }

        private void OnMouseExit()
        {
            SetHighlighted(false);
            if (mixerController != null)
            {
                mixerController.ClearHoverLabel(DisplayName);
            }
        }

        private void OnMouseDown()
        {
            if (mixerController == null || !mixerController.IsSessionOpen)
            {
                return;
            }

            dragCamera = mixerController.ActiveCamera != null ? mixerController.ActiveCamera : Camera.main;
            if (dragCamera == null)
            {
                return;
            }

            Transform handle = handleTransform != null ? handleTransform : transform;
            dragPlane = new Plane(-dragCamera.transform.forward, handle.position);
        }

        private void OnMouseDrag()
        {
            if (mixerController == null || !mixerController.IsSessionOpen || dragCamera == null)
            {
                return;
            }

            Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float distance))
            {
                return;
            }

            Transform handle = handleTransform != null ? handleTransform : transform;
            Transform parent = handle.parent != null ? handle.parent : transform.parent;
            if (parent == null)
            {
                return;
            }

            Vector3 localPoint = parent.InverseTransformPoint(ray.GetPoint(distance));
            Vector3 axis = localMaxPosition - localMinPosition;
            float lengthSquared = axis.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
            {
                return;
            }

            float normalized = Vector3.Dot(localPoint - localMinPosition, axis) / lengthSquared;
            SetNormalizedValue(normalized);
        }

        private void ApplyNormalizedValue(float value, bool notify)
        {
            normalizedValue = Mathf.Clamp01(value);
            Transform handle = handleTransform != null ? handleTransform : transform;
            handle.localPosition = Vector3.Lerp(localMinPosition, localMaxPosition, normalizedValue);

            if (audioSource != null)
            {
                audioSource.volume = normalizedValue;
            }

            if (notify && mixerController != null)
            {
                mixerController.NotifyFaderChanged(this);
            }
        }

        private void ResolveReferences()
        {
            if (handleTransform == null)
            {
                handleTransform = transform;
            }

            if (interactionCollider == null)
            {
                interactionCollider = GetComponent<Collider>();
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

        private void CacheHighlightColor()
        {
            if (hasOriginalHighlightColor || highlightRenderer == null)
            {
                return;
            }

            originalHighlightColor = highlightRenderer.sharedMaterial != null ? highlightRenderer.sharedMaterial.color : Color.white;
            hasOriginalHighlightColor = true;
        }

        private void SetHighlighted(bool highlighted)
        {
            if (highlightRenderer == null)
            {
                return;
            }

            CacheHighlightColor();
            Color target = highlighted ? Color.Lerp(originalHighlightColor, Color.white, 0.35f) : originalHighlightColor;
            highlightRenderer.material.color = target;
        }
    }
}
