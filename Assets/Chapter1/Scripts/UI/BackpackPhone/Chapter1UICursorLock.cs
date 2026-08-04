using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    public static class Chapter1UICursorLock
    {
        public static void ApplyForOpenUi()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void ApplyAfterClose(PlayerInputLock inputLock)
        {
            if (inputLock != null && inputLock.IsLocked)
            {
                ApplyForOpenUi();
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
