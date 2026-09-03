using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2DesktopClickTarget : MonoBehaviour
    {
        [SerializeField] private Collider hitCollider;

        public Collider HitCollider => hitCollider;

        public void Configure(Collider collider)
        {
            hitCollider = collider;
        }

        public bool Matches(Collider candidate)
        {
            return candidate != null && candidate == hitCollider;
        }
    }
}
