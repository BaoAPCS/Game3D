using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldPickupPersistence))]
    public sealed class ItemPickup : Chapter1Interactable
    {
        [SerializeField] private Chapter1ItemId itemId;
        [SerializeField] private int amount = 1;
        [SerializeField] private string pickupMessage;
        [SerializeField] private bool hideAfterPickup = true;
        [SerializeField] private bool destroyAfterPickup;
        [SerializeField] private float destroyDelay;
        [SerializeField] private AudioSource optionalAudioSource;
        [SerializeField] private AudioClip optionalPickupClip;

        private WorldPickupPersistence persistence;
        private Collider[] cachedColliders;
        private Renderer[] cachedRenderers;

        public Chapter1ItemId ItemId => itemId;
        public int Amount => amount;
        public WorldPickupPersistence Persistence => persistence;

        protected override void Awake()
        {
            base.Awake();
            persistence = GetComponent<WorldPickupPersistence>();
            cachedColliders = GetComponentsInChildren<Collider>(true);
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }

        protected override InteractionResult PerformInteraction(InteractionContext context)
        {
            if (context.Inventory == null)
            {
                return InteractionResult.Failed("Không thể nhặt vật phẩm lúc này.");
            }

            if (itemId == Chapter1ItemId.None)
            {
                return InteractionResult.Failed("Vật phẩm không hợp lệ.");
            }

            if (IsUniqueItem(itemId) && context.Inventory.HasItem(itemId))
            {
                return InteractionResult.Failed("Bạn đã có vật phẩm này.");
            }

            if (!context.Inventory.AddItem(itemId, amount))
            {
                return InteractionResult.Failed("Không thể nhặt vật phẩm lúc này.");
            }

            if (optionalAudioSource != null && optionalPickupClip != null)
            {
                optionalAudioSource.PlayOneShot(optionalPickupClip);
            }

            if (persistence != null)
            {
                persistence.RecordCollected(context.ChapterManager);
            }

            ApplyCollectedState();

            string message = string.IsNullOrWhiteSpace(pickupMessage) ? GetDefaultPickupMessage(itemId) : pickupMessage;
            return InteractionResult.Succeeded(message);
        }

        public void ApplyCollectedState()
        {
            DisableInteraction();
            SetCollidersEnabled(false);

            if (hideAfterPickup)
            {
                SetRenderersEnabled(false);
            }

            if (destroyAfterPickup)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cachedColliders == null)
            {
                cachedColliders = GetComponentsInChildren<Collider>(true);
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = enabled;
                }
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (cachedRenderers == null)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].enabled = enabled;
                }
            }
        }

        private static bool IsUniqueItem(Chapter1ItemId item)
        {
            return item == Chapter1ItemId.LanRecording
                || item == Chapter1ItemId.Flashlight
                || item == Chapter1ItemId.Fuse
                || item == Chapter1ItemId.HardDrive
                || item == Chapter1ItemId.Archive17;
        }

        private static string GetDefaultPickupMessage(Chapter1ItemId item)
        {
            switch (item)
            {
                case Chapter1ItemId.Flashlight:
                    return "Đã nhặt đèn pin.";
                case Chapter1ItemId.Fuse:
                    return "Đã nhặt cầu chì.";
                case Chapter1ItemId.ThrowableCan:
                    return "Đã nhặt lon nước.";
                case Chapter1ItemId.HardDrive:
                    return "Đã nhặt ổ cứng.";
                case Chapter1ItemId.LanRecording:
                    return "Đã nhặt đoạn ghi âm.";
                case Chapter1ItemId.Archive17:
                    return "Đã nhặt Hồ sơ số 17.";
                default:
                    return "Đã nhặt vật phẩm.";
            }
        }
    }
}
