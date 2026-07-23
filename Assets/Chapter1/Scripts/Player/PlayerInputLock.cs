using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public sealed class PlayerInputLock : MonoBehaviour
    {
        public const string DialogueReason = "Dialogue";
        public const string PuzzleReason = "Puzzle";
        public const string CCTVReason = "CCTV";
        public const string HidingReason = "Hiding";
        public const string EndingReason = "Ending";
        public const string PauseReason = "Pause";
        public const string RespawnReason = "Respawn";

        private readonly HashSet<string> activeLocks = new HashSet<string>(StringComparer.Ordinal);

        public bool IsLocked => activeLocks.Count > 0;
        public IReadOnlyCollection<string> ActiveLocks => activeLocks;

        public event Action<bool> LockStateChanged;

        public void Lock(string reason)
        {
            if (!IsValidReason(reason, "khóa"))
            {
                return;
            }

            bool wasLocked = IsLocked;
            activeLocks.Add(reason);
            RaiseIfStateChanged(wasLocked);
        }

        public void Unlock(string reason)
        {
            if (!IsValidReason(reason, "mở khóa"))
            {
                return;
            }

            bool wasLocked = IsLocked;
            activeLocks.Remove(reason);
            RaiseIfStateChanged(wasLocked);
        }

        public void ClearAllLocks()
        {
            bool wasLocked = IsLocked;
            activeLocks.Clear();
            RaiseIfStateChanged(wasLocked);
        }

        private bool IsValidReason(string reason, string actionName)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return true;
            }

            Debug.LogWarning($"[PlayerInputLock] GameObject '{gameObject.name}' nhận yêu cầu {actionName} input với reason rỗng.", this);
            return false;
        }

        private void RaiseIfStateChanged(bool wasLocked)
        {
            if (wasLocked != IsLocked)
            {
                LockStateChanged?.Invoke(IsLocked);
            }
        }
    }
}
