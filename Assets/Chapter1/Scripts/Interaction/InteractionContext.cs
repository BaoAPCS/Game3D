using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public readonly struct InteractionContext
    {
        public InteractionContext(
            GameObject playerObject,
            Transform playerTransform,
            PlayerInventory inventory,
            Chapter1Manager chapterManager,
            Chapter1InteractionController interactionController)
        {
            PlayerObject = playerObject;
            PlayerTransform = playerTransform;
            Inventory = inventory;
            ChapterManager = chapterManager;
            InteractionController = interactionController;
        }

        public GameObject PlayerObject { get; }
        public Transform PlayerTransform { get; }
        public PlayerInventory Inventory { get; }
        public Chapter1Manager ChapterManager { get; }
        public Chapter1InteractionController InteractionController { get; }
    }
}
