using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2MissionTriggerZone : MonoBehaviour
    {
        [SerializeField] private Collider zoneCollider;

        private readonly HashSet<Collider> playerContacts =
            new HashSet<Collider>();
        private Chapter1InputReader playerInput;
        private CharacterController playerController;

        public Collider ZoneCollider => zoneCollider;

        public bool ContainsPlayer
        {
            get
            {
                if (playerContacts.Count > 0)
                {
                    return true;
                }

                return zoneCollider != null &&
                       playerController != null &&
                       zoneCollider.enabled &&
                       playerController.enabled &&
                       zoneCollider.gameObject.activeInHierarchy &&
                       playerController.gameObject.activeInHierarchy &&
                       zoneCollider.bounds.Intersects(
                           playerController.bounds) &&
                       Physics.ComputePenetration(
                           zoneCollider,
                           zoneCollider.transform.position,
                           zoneCollider.transform.rotation,
                           playerController,
                           playerController.transform.position,
                           playerController.transform.rotation,
                           out _,
                           out _);
            }
        }

        public void Configure(
            Collider triggerCollider,
            Chapter1InputReader inputReader)
        {
            zoneCollider = triggerCollider;
            playerInput = inputReader;
            playerController = playerInput != null
                ? playerInput.GetComponent<CharacterController>()
                : null;

            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }

            playerContacts.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterPlayerContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            RegisterPlayerContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != null)
            {
                playerContacts.Remove(other);
            }
        }

        private void OnDisable()
        {
            playerContacts.Clear();
        }

        private void RegisterPlayerContact(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                playerContacts.Add(other);
            }
        }

        private bool IsPlayerCollider(Collider other)
        {
            return other != null &&
                   playerInput != null &&
                   other.GetComponentInParent<Chapter1InputReader>() ==
                   playerInput;
        }
    }
}
