using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2DeskComputerInteractable :
        Chapter1Interactable
    {
        private Chapter2PoliceComputerMission mission;
        private Chapter2MissionTriggerZone triggerZone;
        private Transform interactionPoint;

        public void Configure(
            Chapter2PoliceComputerMission missionController,
            Chapter2MissionTriggerZone zone,
            Transform point)
        {
            mission = missionController;
            triggerZone = zone;
            interactionPoint = point;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[F] Kiểm tra";
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
                   mission.CanInspect;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null &&
                   mission.TryBeginObservation(context)
                ? InteractionResult.Succeeded()
                : InteractionResult.Ignored();
        }
    }
}
