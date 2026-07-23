using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public interface IChapter1Interactable
    {
        bool IsInteractionEnabled { get; }
        string GetInteractionPrompt(InteractionContext context);
        bool CanInteract(InteractionContext context);
        InteractionResult Interact(InteractionContext context);
        Transform GetInteractionTransform();
    }
}
