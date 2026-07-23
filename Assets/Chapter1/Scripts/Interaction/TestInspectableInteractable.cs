namespace DormitoryMystery.Chapter1
{
    public sealed class TestInspectableInteractable : Chapter1Interactable
    {
        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            return InteractionResult.Succeeded("Chiếc bàn cũ phủ đầy bụi.");
        }
    }
}
