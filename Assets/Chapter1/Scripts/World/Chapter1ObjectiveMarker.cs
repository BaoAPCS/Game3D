using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class Chapter1ObjectiveMarker : MonoBehaviour
    {
        [SerializeField] private string markerId;
        [SerializeField] private string displayName;
        [SerializeField] private string debugNote;
        [SerializeField] private float plannedTriggerRadius = 1.5f;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.15f, 0.35f);

        public string MarkerId => markerId;
        public string DisplayName => displayName;
        public string DebugNote => debugNote;
        public float PlannedTriggerRadius => plannedTriggerRadius;

        public void Configure(string id, string label, string note, float triggerRadius)
        {
            markerId = id ?? string.Empty;
            displayName = label ?? string.Empty;
            debugNote = note ?? string.Empty;
            plannedTriggerRadius = Mathf.Max(0.1f, triggerRadius);
        }

        private void OnValidate()
        {
            plannedTriggerRadius = Mathf.Max(0.1f, plannedTriggerRadius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, plannedTriggerRadius);
        }
    }
}
