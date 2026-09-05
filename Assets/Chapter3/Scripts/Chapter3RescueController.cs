using System;
using System.Collections;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using DormitoryMystery.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter3
{
    public enum Chapter3RescueState
    {
        Locked, Unlocked, OpeningDoor, DoorOpen, Dialogue, Fading, Completed
    }

    [DisallowMultipleComponent]
    public sealed class Chapter3RescueController : MonoBehaviour, IInventoryItemUseHandler
    {
        public const string RuntimeObjectName = "Chapter3_RescueLan";
        public const string DoorPath = "Hospital/Basement/SeclusionRoomB/Door_V3";
        public const string KeyItemId = Mission3PoliceKeyInventorySync.PoliceKeyItemId;
        public const string CompletionPreferenceKey = "DormitoryMystery.Chapter3.Completed";
        private const string EndingLockReason = "Chapter3.RescueLan.Ending";
        private const string DoorKnockAudioResourcePath = "heavyknock";

        private static readonly string[] Speakers = { "Lan", "Nam", "Lan" };
        private static readonly string[] Lines =
        {
            "Nam... sao em lại ở đây?",
            "Không còn thời gian đâu chị. Mình phải rời khỏi đây ngay.",
            "Được... đi thôi."
        };
        private static readonly float[] LineDurations = { 4f, 5.5f, 3f };

        [SerializeField, Min(0.1f)] private float fadeDuration = 1.5f;
        private Chapter1InputReader player;
        private PlayerInputLock inputLock;
        private InventoryController inventory;
        private BackpackPhoneInputController backpack;
        private InventoryUIController inventoryUI;
        private PhoneUIController phoneUI;
        private PlayerVisualController visual;
        private Chapter2MissionTriggerZone doorZone;
        private Chapter2MissionTriggerZone lanZone;
        private Chapter3HingedDoor door;
        private AudioSource doorKnockAudio;
        private Camera lanCamera;
        private Chapter3RescueHUD hud;
        private readonly Dictionary<Behaviour, bool> capturedBehaviours =
            new Dictionary<Behaviour, bool>();
        private readonly Dictionary<Canvas, bool> capturedCanvases =
            new Dictionary<Canvas, bool>();
        private bool configured;
        private bool handlerRegistered;
        private bool endingCaptured;
        private bool visualWasVisible;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private float lineElapsed;

        public Chapter3RescueState State { get; private set; }
        public int DialogueIndex { get; private set; } = -1;
        public bool IsCompleted => State == Chapter3RescueState.Completed;
        public bool IsInsideDoorZone => doorZone != null && doorZone.ContainsPlayer;
        public bool IsInsideLanZone => lanZone != null && lanZone.ContainsPlayer;

        internal static void InstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != Chapter3CarryOverBootstrap.Chapter3SceneName ||
                FindSceneComponent<Chapter3RescueController>(scene) != null)
                return;

            GameObject owner = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<Chapter3RescueController>();
        }

        private void Start()
        {
            Transform doorRoot = FindSceneTransform(gameObject.scene, DoorPath);
            Transform lan = FindSceneTransform(gameObject.scene, "Lan");
            Transform cameraTransform = lan != null ? lan.Find("Lan_cam") : null;
            player = FindSceneComponent<Chapter1InputReader>(gameObject.scene);
            if (!Configure(doorRoot, lan,
                    cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null,
                    player))
            {
                Debug.LogError(
                    "[Chapter3Rescue] Thiếu Door_V3, SphereCollider, Lan, Lan_cam hoặc Player.", this);
                enabled = false;
            }
        }

        public bool Configure(Transform doorRoot, Transform lan,
            Camera camera, Chapter1InputReader playerInput)
        {
            if (configured)
                return true;
            lanCamera = camera;
            // Keep the inspection camera off even if another required reference is missing.
            if (lanCamera != null)
            {
                lanCamera.enabled = false;
                AudioListener listener = lanCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            SphereCollider doorCollider = doorRoot != null
                ? doorRoot.GetComponent<SphereCollider>() : null;
            SphereCollider lanCollider = lan != null
                ? lan.GetComponent<SphereCollider>() : null;
            if (doorCollider == null || lanCollider == null ||
                lanCamera == null || playerInput == null)
                return false;

            player = playerInput;
            inputLock = player.GetComponent<PlayerInputLock>();
            inventory = player.GetComponent<InventoryController>();
            backpack = player.GetComponent<BackpackPhoneInputController>();
            visual = player.GetComponent<PlayerVisualController>();
            if (inventory == null || inputLock == null || backpack == null)
                return false;

            doorZone = GetOrAdd<Chapter2MissionTriggerZone>(doorRoot.gameObject);
            doorZone.Configure(doorCollider, player);
            lanZone = GetOrAdd<Chapter2MissionTriggerZone>(lan.gameObject);
            lanZone.Configure(lanCollider, player);
            door = GetOrAdd<Chapter3HingedDoor>(doorRoot.gameObject);
            if (!door.Configure(lan))
                return false;
            ConfigureDoorKnockAudio(doorRoot);

            hud = Chapter3RescueHUD.Create(transform);
            ResolveUI();
            configured = true;
            return true;
        }

        private void Update()
        {
            if (!configured)
                return;

            ResolveUI();
            UpdateDoorKnockAudio();
            if (State == Chapter3RescueState.OpeningDoor && door.IsOpen)
                State = Chapter3RescueState.DoorOpen;

            if (State == Chapter3RescueState.Dialogue)
            {
                if (Time.timeScale <= 0f) return;
                lineElapsed += Time.unscaledDeltaTime;
                Keyboard keyboard = Keyboard.current;
                bool pressed = keyboard != null &&
                    (keyboard.enterKey.wasPressedThisFrame ||
                     keyboard.numpadEnterKey.wasPressedThisFrame);
                if (lineElapsed >= LineDurations[DialogueIndex] ||
                    (pressed && lineElapsed >= 0.35f))
                    AdvanceDialogue();
                return;
            }
            if (State >= Chapter3RescueState.Fading)
                return;

            bool nearDoor = IsInsideDoorZone;
            bool obstructedByModal = HasExternalLock(allowInventory: true);
            UpdateInventoryRegistration(nearDoor &&
                State == Chapter3RescueState.Locked && !obstructedByModal);
            bool gameplayVisible = !HasExternalLock(allowInventory: false);
            hud.SetDoorVoice(nearDoor && State <= Chapter3RescueState.Unlocked &&
                gameplayVisible);
            hud.SetDoorPrompt(nearDoor && State == Chapter3RescueState.Unlocked &&
                gameplayVisible);

            if (State == Chapter3RescueState.Unlocked &&
                Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                TryOpenDoor();
            if (State == Chapter3RescueState.DoorOpen)
                TryBeginRescue();
        }

        public bool CanUseInventoryItem(InventoryItem item)
        {
            return configured && isActiveAndEnabled && State == Chapter3RescueState.Locked &&
                   IsInsideDoorZone && !HasExternalLock(allowInventory: true) &&
                   item?.Definition != null &&
                   string.Equals(item.ItemId, KeyItemId, StringComparison.OrdinalIgnoreCase) &&
                   inventory != null && ReferenceEquals(inventory.GetItem(KeyItemId), item);
        }

        public bool TryUseInventoryItem(InventoryItem item)
        {
            if (!CanUseInventoryItem(item)) return false;
            State = Chapter3RescueState.Unlocked;
            UpdateInventoryRegistration(false);
            // InventoryUIController closes the bag when this returns true.
            return true;
        }

        public bool TryOpenDoor()
        {
            if (!configured || State != Chapter3RescueState.Unlocked ||
                !IsInsideDoorZone || HasExternalLock(allowInventory: false) ||
                !door.TryOpen())
                return false;
            State = Chapter3RescueState.OpeningDoor;
            StopDoorKnockAudio();
            hud.SetDoorVoice(false);
            hud.SetDoorPrompt(false);
            return true;
        }

        public bool TryBeginRescue()
        {
            if (!configured || State != Chapter3RescueState.DoorOpen ||
                !IsInsideLanZone || HasExternalLock(allowInventory: true))
                return false;
            inventoryUI?.CloseInventory();
            phoneUI?.ClosePhone();
            UpdateInventoryRegistration(false);
            CaptureEndingState();
            hud.SetDoorVoice(false);
            hud.SetDoorPrompt(false);
            State = Chapter3RescueState.Dialogue;
            DialogueIndex = 0;
            ShowCurrentLine();
            return true;
        }

        public void AdvanceDialogue()
        {
            if (State != Chapter3RescueState.Dialogue) return;
            DialogueIndex++;
            if (DialogueIndex < Lines.Length)
                ShowCurrentLine();
            else
            {
                State = Chapter3RescueState.Fading;
                hud.HideDialogue();
                StartCoroutine(FinishGame());
            }
        }

        private void ShowCurrentLine()
        {
            lineElapsed = 0f;
            hud.ShowDialogue(Speakers[DialogueIndex], Lines[DialogueIndex]);
        }

        private IEnumerator FinishGame()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                hud.SetFade(elapsed / fadeDuration);
                yield return null;
            }
            hud.ShowEnding();
            State = Chapter3RescueState.Completed;
            PlayerPrefs.SetInt(CompletionPreferenceKey, 1);
            PlayerPrefs.Save();
            // Leave the completed HUD visible for a full unscaled second before reset.
            yield return new WaitForSecondsRealtime(GameSessionFlow.EndingMenuDelay);
            if (!GameSessionFlow.TryReturnToMenuAfterCompletion(out string error))
                Debug.LogError("[Chapter3Rescue] " + error, this);
        }

        private void ResolveUI()
        {
            if (backpack == null) return;
            if (inventoryUI == null) inventoryUI = backpack.InventoryUIController;
            if (phoneUI == null) phoneUI = backpack.PhoneUIController;
        }

        private void ConfigureDoorKnockAudio(Transform doorRoot)
        {
            AudioClip clip = Resources.Load<AudioClip>(
                DoorKnockAudioResourcePath);
            if (clip == null)
            {
                Debug.LogError(
                    "[Chapter3Rescue] Không tìm thấy Resources/heavyknock.mp3.",
                    this);
                return;
            }

            doorKnockAudio = doorRoot.GetComponent<AudioSource>();
            if (doorKnockAudio == null)
            {
                doorKnockAudio = doorRoot.gameObject.AddComponent<AudioSource>();
            }

            if (doorKnockAudio.isPlaying)
            {
                doorKnockAudio.Stop();
            }

            doorKnockAudio.clip = clip;
            doorKnockAudio.playOnAwake = false;
            doorKnockAudio.loop = true;
            doorKnockAudio.spatialBlend = 1f;
            doorKnockAudio.minDistance = 1f;
            doorKnockAudio.maxDistance = 8f;
            doorKnockAudio.dopplerLevel = 0f;
        }

        private void UpdateDoorKnockAudio()
        {
            bool shouldPlay = doorKnockAudio != null &&
                doorKnockAudio.clip != null &&
                IsInsideDoorZone &&
                State <= Chapter3RescueState.Unlocked;
            if (!shouldPlay)
            {
                StopDoorKnockAudio();
                return;
            }

            if (!doorKnockAudio.isPlaying)
            {
                doorKnockAudio.Play();
            }
        }

        private void StopDoorKnockAudio()
        {
            if (doorKnockAudio != null && doorKnockAudio.isPlaying)
            {
                doorKnockAudio.Stop();
            }
        }

        private void UpdateInventoryRegistration(bool register)
        {
            if (inventoryUI == null || handlerRegistered == register) return;
            if (register) inventoryUI.RegisterContextItemUseHandler(this);
            else inventoryUI.UnregisterContextItemUseHandler(this);
            handlerRegistered = register;
        }

        private bool HasExternalLock(bool allowInventory)
        {
            if (Time.timeScale <= 0f) return true;
            if (inputLock == null) return false;
            foreach (string reason in inputLock.ActiveLocks)
            {
                if (allowInventory && reason == PlayerInputLock.InventoryReason) continue;
                return true;
            }
            return false;
        }

        private void CaptureEndingState()
        {
            endingCaptured = true;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            visualWasVisible = visual == null || visual.IsVisible;
            inputLock.AcquireInputLock(EndingLockReason);
            CaptureAndSet(backpack, false);
            CaptureAndSet(player.GetComponent<Chapter1InteractionController>(), false);
            CaptureAndSet(player.GetComponent<PlayerCombatController>(), false);
            CaptureAndSet(player, false);
            visual?.SetVisible(false);

            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera.gameObject.scene == gameObject.scene)
                    CaptureAndSet(camera, camera == lanCamera);
            }
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
            {
                if (listener.gameObject.scene == gameObject.scene)
                    CaptureAndSet(listener, listener.gameObject == lanCamera.gameObject);
            }
            foreach (ThirdPersonCameraRig rig in FindObjectsByType<ThirdPersonCameraRig>())
            {
                if (rig.gameObject.scene == gameObject.scene) CaptureAndSet(rig, false);
            }
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.gameObject.scene != gameObject.scene ||
                    canvas.transform.IsChildOf(transform)) continue;
                capturedCanvases[canvas] = canvas.enabled;
                canvas.enabled = false;
            }
            lanCamera.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CaptureAndSet(Behaviour behaviour, bool value)
        {
            if (behaviour == null) return;
            capturedBehaviours[behaviour] = behaviour.enabled;
            behaviour.enabled = value;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            StopDoorKnockAudio();
            UpdateInventoryRegistration(false);
            hud?.HideAll();
            if (!endingCaptured) return;
            foreach (KeyValuePair<Behaviour, bool> entry in capturedBehaviours)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            foreach (KeyValuePair<Canvas, bool> entry in capturedCanvases)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            visual?.SetVisible(visualWasVisible);
            inputLock?.ReleaseInputLock(EndingLockReason);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            capturedBehaviours.Clear();
            capturedCanvases.Clear();
            endingCaptured = false;
            if (State >= Chapter3RescueState.Dialogue)
                State = Chapter3RescueState.DoorOpen;
        }

        private static Transform FindSceneTransform(Scene scene, string path)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == path) return root.transform;
                int slash = path.IndexOf('/');
                if (slash > 0 && root.name == path.Substring(0, slash))
                    return root.transform.Find(path.Substring(slash + 1));
                if (slash < 0)
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        if (child.name == path) return child;
            }
            return null;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (T candidate in FindObjectsByType<T>(FindObjectsInactive.Include))
                if (candidate.gameObject.scene == scene) return candidate;
            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }
    }
}
