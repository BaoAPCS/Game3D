using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2RouterInteractable :
        Chapter1Interactable
    {
        public const string Prompt =
            "[F] Kiểm tra thiết bị Wi-Fi";

        private Chapter2WifiSignalScannerMission mission;
        private Chapter2MissionTriggerZone triggerZone;
        private Transform interactionPoint;

        public void Configure(
            Chapter2WifiSignalScannerMission missionController,
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
                   mission.CanInspectRouter;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null &&
                   mission.TryBeginRouterInspection(context)
                ? InteractionResult.Succeeded()
                : InteractionResult.Ignored();
        }
    }
}
