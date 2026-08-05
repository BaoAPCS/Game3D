using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    public sealed class Mission01CalendarInteractable : Chapter1Interactable
    {
        private const string CalendarText = "Hôm nay là ngày 25 tháng 3.";

        public override string GetInteractionPrompt(InteractionContext context)
        {
            return "[F] Xem lịch";
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            Mission01AudioSeparatorManager manager = Mission01AudioSeparatorManager.Instance;
            if (manager != null)
            {
                manager.Data.Mission01CalendarViewed = true;
                manager.SaveMission();
            }

            return InteractionResult.Succeeded(CalendarText);
        }
    }
}
