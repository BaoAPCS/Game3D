using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    public enum Chapter2BrokenDoorInspection
    {
        None,
        Contract,
        Keypad
    }

    [DisallowMultipleComponent]
    public sealed class Chapter2BrokenDoorInteractable :
        Chapter1Interactable
    {
        public const string Prompt = "[F] Kiểm tra";

        private Chapter2BrokenDoorMission mission;
        private Chapter2MissionTriggerZone triggerZone;
        private Chapter2BrokenDoorInspection inspection;
        private Transform interactionPoint;

        public void Configure(
            Chapter2BrokenDoorMission missionController,
            Chapter2MissionTriggerZone zone,
            Chapter2BrokenDoorInspection inspectionType,
            Transform point)
        {
            mission = missionController;
            triggerZone = zone;
            inspection = inspectionType;
            interactionPoint = point;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return Prompt;
        }

        public override Transform GetInteractionTransform()
        {
            return interactionPoint != null
                ? interactionPoint
                : base.GetInteractionTransform();
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   mission != null &&
                   triggerZone != null &&
                   triggerZone.ContainsPlayer &&
                   mission.CanInspect(inspection);
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null &&
                   mission.TryBeginInspection(inspection, context)
                ? InteractionResult.Succeeded()
                : InteractionResult.Ignored();
        }
    }
}
