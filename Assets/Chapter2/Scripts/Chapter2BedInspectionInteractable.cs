using DormitoryMystery.Chapter1;

namespace DormitoryMystery.Chapter2
{
    public sealed class Chapter2BedInspectionInteractable :
        Chapter1Interactable
    {
        private Chapter2ServiceCardMission mission;
        private Chapter2MissionTriggerZone triggerZone;

        public void Configure(
            Chapter2ServiceCardMission missionController,
            Chapter2MissionTriggerZone zone)
        {
            mission = missionController;
            triggerZone = zone;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[F] Quan sát";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   mission != null &&
                   triggerZone != null &&
                   triggerZone.ContainsPlayer &&
                   mission.CanInspectBed;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null &&
                   mission.TryBeginBedObservation(context)
                ? InteractionResult.Succeeded()
                : InteractionResult.Ignored();
        }
    }
}
