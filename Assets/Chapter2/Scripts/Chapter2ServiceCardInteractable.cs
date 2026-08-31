using DormitoryMystery.Chapter1;

namespace DormitoryMystery.Chapter2
{
    public sealed class Chapter2ServiceCardInteractable :
        Chapter1Interactable
    {
        private Chapter2ServiceCardMission mission;

        public override Chapter1InteractionInput InteractionInput =>
            Chapter1InteractionInput.Talk;

        public void Configure(
            Chapter2ServiceCardMission missionController)
        {
            mission = missionController;
        }

        public override string GetInteractionPrompt(
            InteractionContext context)
        {
            return "[E] Nhặt thẻ kích hoạt";
        }

        public override bool CanInteract(InteractionContext context)
        {
            return base.CanInteract(context) &&
                   mission != null &&
                   mission.CanCollectServiceCard;
        }

        protected override InteractionResult PerformInteraction(
            InteractionContext context)
        {
            return mission != null && mission.TryCollectServiceCard()
                ? InteractionResult.Succeeded(
                    "Đã nhặt thẻ kích hoạt.")
                : InteractionResult.Ignored();
        }
    }
}
