using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2ClosetInteractable :
        Chapter1Interactable
    {
        private Chapter2ConfiscatedItemsMission mission;
        private Chapter2MissionTriggerZone triggerZone;

        public void Configure(
            Chapter2ConfiscatedItemsMission missionController,
            Chapter2MissionTriggerZone zone)
        {
            mission = missionController;
            triggerZone = zone;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return mission != null && mission.ClosetUnlocked
                ? "[F] Mở tủ"
                : "Tủ bị khóa";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   mission != null &&
                   triggerZone != null &&
                   triggerZone.ContainsPlayer &&
                   mission.CanShowPrompt;
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
