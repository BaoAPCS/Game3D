using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2CircuitBoxInteractable :
        Chapter1Interactable
    {
        private Chapter2CircuitPuzzleMission mission;
        private Transform interactionPoint;

        public void Configure(
            Chapter2CircuitPuzzleMission missionController,
            Transform configuredInteractionPoint)
        {
            mission = missionController;
            interactionPoint = configuredInteractionPoint;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[F] Kích hoạt";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   mission != null &&
                   mission.CanActivate;
        }

        public override Transform GetInteractionTransform()
        {
            return interactionPoint != null
                ? interactionPoint
                : base.GetInteractionTransform();
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null && mission.TryOpen(context)
                ? InteractionResult.Succeeded()
                : InteractionResult.Ignored();
        }
    }
}
