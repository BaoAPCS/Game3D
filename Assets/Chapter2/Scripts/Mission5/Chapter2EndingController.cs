using System;
using System.Collections;
using DormitoryMystery.Chapter1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2EndingController :
        MonoBehaviour,
        IInventoryItemUseHandler
    {
        public const string RuntimeObjectName =
            "Chapter2_EndingController";
        public const string NewMessageNotification =
            "Có tin nhắn mới từ Minh, hãy kiểm tra điện thoại";
        public const string CompletionNotification =
            "CHAPTER 2 COMPLETE";
        public const float NewMessageDelay = 1.5f;

        private const string PoliceStationSceneName = "Police_Station";
        private const string DocumentResourcePath =
            "Inventory/ClassifiedDocumentItem";

        private Chapter2SaveManager saveManager;
        private InventoryController inventory;
        private InventoryUIController inventoryUI;
        private PhoneUIController phoneUI;
        private PlayerInputLock inputLock;
        private ItemDefinition documentDefinition;
        private Chapter2SecretDocumentViewer viewer;
        private Coroutine notificationRoutine;
        private bool transitionRequested;
        private bool viewerSubscribed;
        private int lastPhoneHash = int.MinValue;

        public bool IsDocumentViewerOpen =>
            viewer != null && viewer.IsVisible;
        public bool ConversationAvailable =>
            saveManager != null &&
            saveManager.CurrentData
                .Mission05MinhConversationAvailable;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialSceneLoad()
        {
            InstallForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            InstallForScene(scene);
        }

        private static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != PoliceStationSceneName)
            {
                return;
            }

            Chapter2EndingController[] controllers =
                FindObjectsByType<Chapter2EndingController>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null &&
                    controllers[i].gameObject.scene == scene)
                {
                    return;
                }
            }

            GameObject owner = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<Chapter2EndingController>();
        }

        private void Start()
        {
            ResolveReferences();
            SynchronizeState(true);
        }

        private void Update()
        {
            ResolveReferences();
            RegisterInventoryUseHandler();
            SynchronizeState(false);
        }

        private void OnDisable()
        {
            inventoryUI?.ClearItemUseHandler(this);
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
                notificationRoutine = null;
            }
        }

        private void OnDestroy()
        {
            inventoryUI?.ClearItemUseHandler(this);
            if (viewerSubscribed && viewer != null)
            {
                viewer.Closed -= HandleViewerClosed;
            }
        }

        public bool CanUseInventoryItem(InventoryItem item)
        {
            return item?.Definition != null &&
                   string.Equals(
                       item.Definition.ItemId,
                       Chapter2WifiSignalScannerMission
                           .ClassifiedDocumentItemId,
                       StringComparison.OrdinalIgnoreCase) &&
                   saveManager != null &&
                   saveManager.CurrentData
                       .Mission05SecretDocumentCollected &&
                   !transitionRequested &&
                   viewer != null &&
                   !viewer.IsVisible;
        }

        public bool TryUseInventoryItem(InventoryItem item)
        {
            if (!CanUseInventoryItem(item) ||
                documentDefinition == null ||
                documentDefinition.PreviewImage == null)
            {
                return false;
            }

            viewer.Show(documentDefinition.PreviewImage);
            Chapter2SaveData data = saveManager.CurrentData;
            if (!data.Mission05SecretDocumentViewed)
            {
                saveManager.SaveMission05DocumentViewed();
            }

            return true;
        }

        private void ResolveReferences()
        {
            Scene scene = gameObject.scene;
            saveManager ??= Chapter2SaveManager.EnsureForScene(scene);
            inventory ??= FindSceneComponent<InventoryController>(scene);
            inputLock ??= inventory != null
                ? inventory.GetComponent<PlayerInputLock>()
                : FindSceneComponent<PlayerInputLock>(scene);

            BackpackPhoneInputController backpack = inventory != null
                ? inventory.GetComponent<BackpackPhoneInputController>()
                : FindSceneComponent<BackpackPhoneInputController>(scene);
            inventoryUI ??= backpack != null
                ? backpack.InventoryUIController
                : FindSceneComponent<InventoryUIController>(scene);
            phoneUI ??= backpack != null
                ? backpack.PhoneUIController
                : FindSceneComponent<PhoneUIController>(scene);
            documentDefinition ??= Resources.Load<ItemDefinition>(
                DocumentResourcePath);

            if (viewer == null)
            {
                viewer = Chapter2SecretDocumentViewer.Create(
                    transform,
                    inputLock);
            }

            if (!viewerSubscribed && viewer != null)
            {
                viewer.Closed += HandleViewerClosed;
                viewerSubscribed = true;
            }
        }

        private void RegisterInventoryUseHandler()
        {
            if (inventoryUI == null || saveManager == null)
            {
                return;
            }

            if (saveManager.CurrentData
                    .Mission05SecretDocumentCollected &&
                inventory != null &&
                inventory.HasItem(
                    Chapter2WifiSignalScannerMission
                        .ClassifiedDocumentItemId) &&
                !transitionRequested)
            {
                inventoryUI.SetItemUseHandler(this);
            }
            else
            {
                inventoryUI.ClearItemUseHandler(this);
            }
        }

        private void SynchronizeState(bool force)
        {
            if (saveManager == null)
            {
                return;
            }

            Chapter2SaveData data = saveManager.CurrentData;
            data.EnsureValidDefaults();
            if (data.Mission05SecretDocumentViewed &&
                !data.Mission05MinhConversationAvailable &&
                (viewer == null || !viewer.IsVisible))
            {
                ScheduleNewMessage();
            }

            int hash = CalculatePhoneHash(data);
            if (force || hash != lastPhoneHash)
            {
                ConfigurePhone(data);
                lastPhoneHash = hash;
            }

            if (data.Chapter2Completed && !transitionRequested)
            {
                CompleteAndTransition(false);
            }
        }

        private void ScheduleNewMessage()
        {
            if (notificationRoutine != null ||
                saveManager == null ||
                saveManager.CurrentData
                    .Mission05MinhConversationAvailable)
            {
                return;
            }

            notificationRoutine = StartCoroutine(
                RevealNewMessageAfterDelay());
        }

        private void HandleViewerClosed()
        {
            ScheduleNewMessage();
        }

        private IEnumerator RevealNewMessageAfterDelay()
        {
            yield return new WaitForSecondsRealtime(NewMessageDelay);
            notificationRoutine = null;
            if (saveManager == null ||
                saveManager.CurrentData
                    .Mission05MinhConversationAvailable)
            {
                yield break;
            }

            saveManager.SaveMission05MinhConversationAvailable();
            ConfigurePhone(saveManager.CurrentData);
            Chapter1EventBus.RaiseNotification(NewMessageNotification);
        }

        private void ConfigurePhone(Chapter2SaveData data)
        {
            if (phoneUI == null || data == null)
            {
                return;
            }

            phoneUI.ConfigureChapter2EndingConversation(
                data.Mission05MinhConversationAvailable,
                data.Mission05MinhConversationOpened,
                data.Mission05MinhConversationStep,
                HandleConversationOpened,
                HandleConversationStepChanged,
                HandleConversationCompleted);
        }

        private void HandleConversationOpened()
        {
            if (saveManager == null ||
                saveManager.CurrentData
                    .Mission05MinhConversationOpened)
            {
                return;
            }

            saveManager.SaveMission05MinhConversationOpened();
            lastPhoneHash = int.MinValue;
        }

        private void HandleConversationStepChanged(int step)
        {
            if (saveManager == null)
            {
                return;
            }

            saveManager.SaveMission05MinhConversationStep(step);
            lastPhoneHash = int.MinValue;
        }

        private void HandleConversationCompleted()
        {
            CompleteAndTransition(true);
        }

        private void CompleteAndTransition(bool showNotification)
        {
            if (transitionRequested || saveManager == null)
            {
                return;
            }

            transitionRequested = true;
            Chapter1SaveData phoneSnapshot = phoneUI != null
                ? phoneUI.CurrentPhoneData
                : saveManager.CurrentData.PhoneData
                    .ToChapter1SaveData();
            saveManager.SaveChapter2Completed(
                inventory,
                phoneSnapshot);
            inventoryUI?.ClearItemUseHandler(this);
            if (showNotification)
            {
                Chapter1EventBus.RaiseUrgentNotification(
                    CompletionNotification);
            }

            Chapter2SceneTransitionController
                .BeginChapter3Transition(showNotification ? 1.2f : 0f);
        }

        private static int CalculatePhoneHash(Chapter2SaveData data)
        {
            int hash = 17;
            hash = hash * 31 +
                   (data.Mission05MinhConversationAvailable ? 1 : 0);
            hash = hash * 31 +
                   (data.Mission05MinhConversationOpened ? 1 : 0);
            hash = hash * 31 +
                   data.Mission05MinhConversationStep;
            return hash;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] candidates = FindObjectsByType<T>(
                FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                T candidate = candidates[i];
                if (candidate != null &&
                    candidate.gameObject.scene == scene)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
