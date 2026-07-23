using System.Collections;
using TMPro;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class ObjectiveHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private Chapter1Manager chapterManager;
        [SerializeField] private float newObjectiveLabelSeconds = 2f;

        private Coroutine labelRoutine;

        private void Awake()
        {
            ResolveTextReferences();
        }

        private void Start()
        {
            if (chapterManager != null)
            {
                SetObjective(Chapter1Manager.GetObjective(chapterManager.CurrentStep), false);
            }
        }

        private void OnEnable()
        {
            Chapter1EventBus.ObjectiveChanged += HandleObjectiveChanged;
        }

        private void OnDisable()
        {
            Chapter1EventBus.ObjectiveChanged -= HandleObjectiveChanged;
        }

        public void Bind(Chapter1Manager manager)
        {
            chapterManager = manager;
            if (chapterManager != null)
            {
                SetObjective(Chapter1Manager.GetObjective(chapterManager.CurrentStep), false);
            }
        }

        private void ResolveTextReferences()
        {
            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (labelText == null && labels.Length > 0)
            {
                labelText = labels[0];
            }

            if (objectiveText == null && labels.Length > 1)
            {
                objectiveText = labels[1];
            }
        }

        private void HandleObjectiveChanged(string objective)
        {
            SetObjective(objective, true);
        }

        private void SetObjective(string objective, bool showNewLabel)
        {
            ResolveTextReferences();
            if (objectiveText != null)
            {
                objectiveText.text = string.IsNullOrWhiteSpace(objective) ? "Không có mục tiêu." : objective;
            }

            if (labelText == null)
            {
                return;
            }

            labelText.text = showNewLabel ? "Mục tiêu mới" : "Mục tiêu";
            if (labelRoutine != null)
            {
                StopCoroutine(labelRoutine);
                labelRoutine = null;
            }

            if (showNewLabel && isActiveAndEnabled)
            {
                labelRoutine = StartCoroutine(RestoreLabelAfterDelay());
            }
        }

        private IEnumerator RestoreLabelAfterDelay()
        {
            yield return new WaitForSecondsRealtime(newObjectiveLabelSeconds);
            if (labelText != null)
            {
                labelText.text = "Mục tiêu";
            }

            labelRoutine = null;
        }
    }
}
