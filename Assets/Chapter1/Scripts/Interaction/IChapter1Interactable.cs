using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public enum Chapter1InteractionInput
    {
        Interact = 0,
        Talk = 1
    }

    public interface IChapter1Interactable
    {
        bool IsInteractionEnabled { get; }
        Chapter1InteractionInput InteractionInput { get; }
        string GetInteractionPrompt(InteractionContext context);
        bool CanInteract(InteractionContext context);
        InteractionResult Interact(InteractionContext context);
        Transform GetInteractionTransform();
    }
}
