using System;
using System.Collections.Generic;
using DormitoryMystery.Chapter1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Editor
{
    public static class Chapter1InteractionHudDiagnostic
    {
        private const string ScenePath = "Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity";
        private const string InputActionsPath = "Assets/Chapter1/Settings/Chapter1Controls.inputactions";
        private const string GameplayMapName = "Gameplay";

        private static readonly InputReferenceSpec[] InputReferenceSpecs =
        {
            new InputReferenceSpec("Move", "moveActionReference", null),
            new InputReferenceSpec("Look", "lookActionReference", null),
            new InputReferenceSpec("Sprint", "sprintActionReference", null),
            new InputReferenceSpec("Crouch", "crouchActionReference", null),
            new InputReferenceSpec("Interact", "interactActionReference", "<Keyboard>/f"),
            new InputReferenceSpec("ToggleFlashlight", "toggleFlashlightActionReference", "<Keyboard>/t"),
            new InputReferenceSpec("ThrowCan", "throwCanActionReference", "<Keyboard>/g"),
            new InputReferenceSpec("Pause", "pauseActionReference", "<Keyboard>/escape")
        };

        private static readonly TestObjectSpec[] TestObjects =
        {
            new TestObjectSpec("Pickup_Flashlight_Test", Chapter1ItemId.Flashlight, "pickup.flashlight.test", true),
            new TestObjectSpec("Pickup_Fuse_Test", Chapter1ItemId.Fuse, "pickup.fuse.test", true),
            new TestObjectSpec("Pickup_Can_Test_01", Chapter1ItemId.ThrowableCan, "pickup.can.test.01", true),
            new TestObjectSpec("Pickup_Can_Test_02", Chapter1ItemId.ThrowableCan, "pickup.can.test.02", true),
            new TestObjectSpec("Pickup_Can_Test_03", Chapter1ItemId.ThrowableCan, "pickup.can.test.03", true),
            new TestObjectSpec("TestInspectableTable", Chapter1ItemId.None, "inspectable.table.test", false)
        };

        public readonly struct DiagnosticResult
        {
            public DiagnosticResult(int passCount, int warningCount, int errorCount)
            {
                PassCount = passCount;
                WarningCount = warningCount;
                ErrorCount = errorCount;
            }

            public int PassCount { get; }
            public int WarningCount { get; }
            public int ErrorCount { get; }
        }

        private readonly struct InputReferenceSpec
        {
            public InputReferenceSpec(string actionName, string fieldName, string requiredKeyboardBinding)
            {
                ActionName = actionName;
                FieldName = fieldName;
                RequiredKeyboardBinding = requiredKeyboardBinding;
            }

            public string ActionName { get; }
            public string FieldName { get; }
            public string RequiredKeyboardBinding { get; }
            public string ActionPath => $"{GameplayMapName}/{ActionName}";
        }

        private readonly struct TestObjectSpec
        {
            public TestObjectSpec(string objectName, Chapter1ItemId itemId, string persistentId, bool isPickup)
            {
                ObjectName = objectName;
                ItemId = itemId;
                PersistentId = persistentId;
                IsPickup = isPickup;
            }

            public string ObjectName { get; }
            public Chapter1ItemId ItemId { get; }
            public string PersistentId { get; }
            public bool IsPickup { get; }
        }

        private sealed class Reporter
        {
            private readonly bool logToConsole;

            public Reporter(bool logToConsole)
            {
                this.logToConsole = logToConsole;
            }

            public int PassCount { get; private set; }
            public int WarningCount { get; private set; }
            public int ErrorCount { get; private set; }

            public DiagnosticResult Result => new DiagnosticResult(PassCount, WarningCount, ErrorCount);

            public void Pass(string message)
            {
                PassCount++;
                if (logToConsole)
                {
                    Debug.Log($"[Chapter1 Diagnostic] PASS: {message}");
                }
            }

            public void Warning(string message)
            {
                WarningCount++;
                if (logToConsole)
                {
                    Debug.LogWarning($"[Chapter1 Diagnostic] WARNING: {message}");
                }
            }

            public void Error(string message)
            {
                ErrorCount++;
                if (logToConsole)
                {
                    Debug.LogError($"[Chapter1 Diagnostic] ERROR: {message}");
                }
            }
        }

        [MenuItem("Tools/Chapter 1/Diagnose Interaction and HUD")]
        public static void DiagnoseInteractionAndHud()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Chapter1 Diagnostic] Canceled because current scene changes were not saved.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DiagnosticResult result = RunDiagnosticForScene(scene, true);
            EditorUtility.DisplayDialog(
                "Diagnose Interaction and HUD",
                $"PASS = {result.PassCount}\nWARNING = {result.WarningCount}\nERROR = {result.ErrorCount}",
                "OK");
        }

        public static DiagnosticResult RunDiagnosticForScene(Scene scene, bool logToConsole)
        {
            Reporter reporter = new Reporter(logToConsole);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                reporter.Error($"Scene không hợp lệ hoặc chưa load: {ScenePath}.");
                return reporter.Result;
            }

            Physics.SyncTransforms();

            GameObject player = FindScenePlayer(scene);
            GameObject cameraRig = FindSceneObject(scene, "CameraRig");
            Camera mainCamera = FindMainCamera(scene, reporter);
            GameObject testGroup = FindSceneObject(scene, "InteractionInventoryTest");
            GameObject uiRoot = FindSceneObject(scene, "UI");
            EventSystem eventSystem = GetSceneComponents<EventSystem>(scene).Count == 1 ? GetSceneComponents<EventSystem>(scene)[0] : null;

            ValidateNamedObject(player, "Player", reporter);
            ValidateNamedObject(cameraRig, "CameraRig", reporter);
            ValidateNamedObject(mainCamera != null ? mainCamera.gameObject : null, "Main Camera", reporter);
            ValidateNamedObject(testGroup, "InteractionInventoryTest", reporter);
            ValidateNamedObject(uiRoot, "UI", reporter);
            ValidateEventSystem(scene, eventSystem, reporter);

            ValidatePlayer(player, reporter);
            ValidateInputReferences(player != null ? player.GetComponent<Chapter1InputReader>() : null, reporter);
            ValidateInteractionController(player, mainCamera, reporter);
            ValidateCamera(scene, cameraRig, mainCamera, player, reporter);
            ValidateTestObjects(scene, testGroup, player, mainCamera, reporter);
            ValidateFlashlight(player, reporter);
            ValidateHud(scene, uiRoot, reporter);
            ValidateRuntimeSelfTest(testGroup, reporter);

            return reporter.Result;
        }

        private static void ValidateNamedObject(GameObject gameObject, string label, Reporter reporter)
        {
            if (gameObject != null)
            {
                reporter.Pass($"Tìm thấy {label}.");
            }
            else
            {
                reporter.Error($"Không tìm thấy {label}.");
            }
        }

        private static void ValidatePlayer(GameObject player, Reporter reporter)
        {
            if (player == null)
            {
                return;
            }

            ValidateSingleComponent<CharacterController>(player, "Player", reporter);
            ValidateSingleComponent<Chapter1InputReader>(player, "Player", reporter);
            ValidateSingleComponent<PlayerInputLock>(player, "Player", reporter);
            ValidateSingleComponent<PlayerStamina>(player, "Player", reporter);
            ValidateSingleComponent<Chapter1PlayerMotor>(player, "Player", reporter);
            ValidateSingleComponent<PlayerInventory>(player, "Player", reporter);
            ValidateSingleComponent<Chapter1InteractionController>(player, "Player", reporter);
            ValidateSingleComponent<FlashlightController>(player, "Player", reporter);

            if (string.Equals(player.tag, "Player", StringComparison.Ordinal))
            {
                reporter.Pass("Player có tag Player.");
            }
            else
            {
                reporter.Error($"Player tag là '{player.tag}', yêu cầu Player.");
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0 && player.layer == playerLayer)
            {
                reporter.Pass("Player ở layer Player.");
            }
            else
            {
                reporter.Error("Player chưa ở layer Player.");
            }

            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null && characterController.enabled)
            {
                reporter.Pass("CharacterController enabled.");
            }
            else
            {
                reporter.Error("CharacterController thiếu hoặc disabled.");
            }

            if (player.activeInHierarchy)
            {
                reporter.Pass("Player active trong scene.");
            }
            else
            {
                reporter.Error("Player không active trong scene.");
            }

            if (player.GetComponent<Rigidbody>() == null)
            {
                reporter.Pass("Player root không có Rigidbody.");
            }
            else
            {
                reporter.Error("Player root không được có Rigidbody.");
            }

            if (player.GetComponent<NavMeshAgent>() == null)
            {
                reporter.Pass("Player root không có NavMeshAgent.");
            }
            else
            {
                reporter.Error("Player root không được có NavMeshAgent.");
            }
        }

        private static void ValidateInputReferences(Chapter1InputReader inputReader, Reporter reporter)
        {
            if (inputReader == null)
            {
                reporter.Error("Không thể kiểm tra input references vì Player thiếu Chapter1InputReader.");
                return;
            }

            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
            {
                reporter.Error($"Thiếu InputActionAsset: {InputActionsPath}.");
                return;
            }

            SerializedObject serializedReader = new SerializedObject(inputReader);
            foreach (InputReferenceSpec spec in InputReferenceSpecs)
            {
                SerializedProperty property = serializedReader.FindProperty(spec.FieldName);
                if (property == null)
                {
                    reporter.Error($"Chapter1InputReader thiếu serialized field {spec.FieldName}.");
                    continue;
                }

                InputAction expectedAction = inputAsset.FindAction(spec.ActionPath, false);
                InputActionReference actionReference = property.objectReferenceValue as InputActionReference;
                if (actionReference == null)
                {
                    reporter.Error($"{spec.FieldName} đang null.");
                    continue;
                }

                if (!EditorUtility.IsPersistent(actionReference))
                {
                    reporter.Error($"{spec.FieldName} không trỏ tới InputActionReference persistent asset.");
                }
                else
                {
                    reporter.Pass($"{spec.FieldName} là persistent asset.");
                }

                if (actionReference.action == null)
                {
                    reporter.Error($"{spec.FieldName}.action đang null.");
                    continue;
                }

                if (expectedAction == null)
                {
                    reporter.Error($"Không tìm thấy action {spec.ActionPath} trong {InputActionsPath}.");
                    continue;
                }

                ValidateActionIdentity(actionReference, expectedAction, spec, reporter);
            }
        }

        private static void ValidateActionIdentity(InputActionReference actionReference, InputAction expectedAction, InputReferenceSpec spec, Reporter reporter)
        {
            InputAction actualAction = actionReference.action;
            string actualAssetPath = actionReference.asset != null ? AssetDatabase.GetAssetPath(actionReference.asset) : string.Empty;
            string expectedAssetPath = expectedAction.actionMap != null && expectedAction.actionMap.asset != null
                ? AssetDatabase.GetAssetPath(expectedAction.actionMap.asset)
                : string.Empty;

            bool validIdentity = actualAction.id == expectedAction.id
                && string.Equals(actualAction.name, spec.ActionName, StringComparison.Ordinal)
                && actualAction.actionMap != null
                && string.Equals(actualAction.actionMap.name, GameplayMapName, StringComparison.Ordinal)
                && string.Equals(actualAssetPath, expectedAssetPath, StringComparison.Ordinal);

            if (validIdentity)
            {
                reporter.Pass($"{spec.FieldName} trỏ đúng {spec.ActionPath}.");
            }
            else
            {
                reporter.Error($"{spec.FieldName} trỏ tới {DescribeAction(actualAction)} thay vì {spec.ActionPath}.");
            }

            if (!string.IsNullOrEmpty(spec.RequiredKeyboardBinding))
            {
                if (HasBinding(expectedAction, spec.RequiredKeyboardBinding))
                {
                    reporter.Pass($"{spec.ActionPath} có binding {spec.RequiredKeyboardBinding}.");
                }
                else
                {
                    reporter.Error($"{spec.ActionPath} thiếu binding {spec.RequiredKeyboardBinding}.");
                }
            }
        }

        private static void ValidateInteractionController(GameObject player, Camera mainCamera, Reporter reporter)
        {
            Chapter1InteractionController controller = player != null ? player.GetComponent<Chapter1InteractionController>() : null;
            if (controller == null)
            {
                return;
            }

            ValidateSerializedObjectReference(controller, "inputReader", "InteractionController inputReader", reporter);
            ValidateSerializedObjectReference(controller, "inputLock", "InteractionController inputLock", reporter);
            ValidateSerializedObjectReference(controller, "inventory", "InteractionController inventory", reporter);
            Object gameplayCamera = ValidateSerializedObjectReference(controller, "gameplayCamera", "InteractionController gameplayCamera", reporter);
            if (mainCamera != null && gameplayCamera == mainCamera)
            {
                reporter.Pass("InteractionController gameplayCamera trỏ đúng Main Camera.");
            }

            float distance = GetSerializedFloat(controller, "interactionDistance");
            if (distance >= 2.5f && distance <= 3.5f)
            {
                reporter.Pass($"Interaction distance hợp lệ: {distance:0.00}.");
            }
            else
            {
                reporter.Error($"Interaction distance {distance:0.00} nằm ngoài khoảng 2.5 đến 3.5.");
            }

            float radius = GetSerializedFloat(controller, "sphereRadius");
            if (radius >= 0.1f && radius <= 0.35f)
            {
                reporter.Pass($"Sphere radius hợp lệ: {radius:0.00}.");
            }
            else
            {
                reporter.Error($"Sphere radius {radius:0.00} nằm ngoài khoảng 0.1 đến 0.35.");
            }

            ValidateLayerMaskIncludes(controller, "interactionMask", "Interactable", "InteractionController interactionMask", reporter);
            int triggerInteraction = GetSerializedInt(controller, "triggerInteraction");
            if (triggerInteraction != (int)QueryTriggerInteraction.Ignore)
            {
                reporter.Pass("QueryTriggerInteraction cho phép phát hiện collider cần thiết.");
            }
            else
            {
                reporter.Error("QueryTriggerInteraction đang Ignore.");
            }

            if (controller.enabled && controller.gameObject.activeInHierarchy)
            {
                reporter.Pass("InteractionController enabled và active.");
            }
            else
            {
                reporter.Error("InteractionController disabled hoặc GameObject không active.");
            }
        }

        private static void ValidateCamera(Scene scene, GameObject cameraRig, Camera mainCamera, GameObject player, Reporter reporter)
        {
            List<AudioListener> listeners = GetSceneComponents<AudioListener>(scene);
            if (listeners.Count == 1)
            {
                reporter.Pass("Scene có đúng một AudioListener.");
            }
            else
            {
                reporter.Error($"Scene có {listeners.Count} AudioListener, yêu cầu đúng 1.");
            }

            if (mainCamera == null)
            {
                return;
            }

            if (mainCamera.gameObject.activeInHierarchy && mainCamera.enabled)
            {
                reporter.Pass("Main Camera active và enabled.");
            }
            else
            {
                reporter.Error("Main Camera không active hoặc disabled.");
            }

            if (cameraRig != null && mainCamera.transform.IsChildOf(cameraRig.transform))
            {
                reporter.Pass("Main Camera nằm trong CameraRig.");
            }
            else
            {
                reporter.Error("Main Camera không nằm trong CameraRig.");
            }

            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            if (cameras.Count == 1)
            {
                reporter.Pass("Scene có đúng một Camera.");
            }
            else
            {
                reporter.Error($"Scene có {cameras.Count} Camera, yêu cầu đúng 1.");
            }
        }

        private static void ValidateTestObjects(Scene scene, GameObject testGroup, GameObject player, Camera mainCamera, Reporter reporter)
        {
            if (testGroup == null)
            {
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < TestObjects.Length; i++)
            {
                TestObjectSpec spec = TestObjects[i];
                GameObject testObject = FindSceneObject(scene, spec.ObjectName);
                ValidateTestObject(testObject, spec, ids, reporter);
                ValidateGeometry(testObject, player, mainCamera, reporter);
            }
        }

        private static void ValidateTestObject(GameObject testObject, TestObjectSpec spec, HashSet<string> ids, Reporter reporter)
        {
            if (testObject == null)
            {
                reporter.Error($"Scene thiếu {spec.ObjectName}.");
                return;
            }

            reporter.Pass($"Scene có {spec.ObjectName}.");
            if (testObject.activeInHierarchy)
            {
                reporter.Pass($"{spec.ObjectName} active.");
            }
            else
            {
                reporter.Error($"{spec.ObjectName} không active.");
            }

            Collider[] colliders = testObject.GetComponentsInChildren<Collider>(true);
            bool hasEnabledCollider = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                hasEnabledCollider |= colliders[i] != null && colliders[i].enabled;
            }

            if (hasEnabledCollider)
            {
                reporter.Pass($"{spec.ObjectName} có Collider enabled.");
            }
            else
            {
                reporter.Error($"{spec.ObjectName} thiếu Collider enabled.");
            }

            ValidateInteractableLayer(testObject, colliders, reporter);
            IChapter1Interactable interactable = GetInteractable(testObject);
            if (interactable != null)
            {
                reporter.Pass($"{spec.ObjectName} có IChapter1Interactable.");
            }
            else
            {
                reporter.Error($"{spec.ObjectName} thiếu IChapter1Interactable trên root hoặc parent.");
            }

            if (spec.IsPickup)
            {
                ValidatePickup(testObject, spec, ids, reporter);
            }
            else
            {
                ValidateInspectableTable(testObject, reporter);
            }
        }

        private static void ValidatePickup(GameObject testObject, TestObjectSpec spec, HashSet<string> ids, Reporter reporter)
        {
            ItemPickup pickup = testObject.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                reporter.Error($"{testObject.name} thiếu ItemPickup.");
            }
            else
            {
                if (pickup.ItemId == spec.ItemId)
                {
                    reporter.Pass($"{testObject.name} có ItemId đúng: {spec.ItemId}.");
                }
                else
                {
                    reporter.Error($"{testObject.name} có ItemId {pickup.ItemId}, yêu cầu {spec.ItemId}.");
                }

                if (pickup.Amount == 1)
                {
                    reporter.Pass($"{testObject.name} amount = 1.");
                }
                else
                {
                    reporter.Error($"{testObject.name} amount = {pickup.Amount}, yêu cầu 1.");
                }
            }

            WorldPickupPersistence persistence = testObject.GetComponent<WorldPickupPersistence>();
            if (persistence != null && !string.IsNullOrWhiteSpace(persistence.PersistentId))
            {
                reporter.Pass($"{testObject.name} có persistent ID: {persistence.PersistentId}.");
                if (!ids.Add(persistence.PersistentId))
                {
                    reporter.Error($"Persistent ID bị trùng: {persistence.PersistentId}.");
                }
            }
            else
            {
                reporter.Error($"{testObject.name} thiếu persistent ID.");
            }
        }

        private static void ValidateInspectableTable(GameObject testObject, Reporter reporter)
        {
            TestInspectableInteractable table = testObject.GetComponent<TestInspectableInteractable>();
            if (table != null)
            {
                reporter.Pass("TestInspectableTable có TestInspectableInteractable.");
                if (!GetSerializedBool(table, "oneShot"))
                {
                    reporter.Pass("TestInspectableTable không one-shot.");
                }
                else
                {
                    reporter.Error("TestInspectableTable đang one-shot.");
                }
            }
            else
            {
                reporter.Error("TestInspectableTable thiếu TestInspectableInteractable.");
            }
        }

        private static void ValidateGeometry(GameObject testObject, GameObject player, Camera mainCamera, Reporter reporter)
        {
            if (testObject == null || player == null)
            {
                return;
            }

            Chapter1InteractionController controller = player.GetComponent<Chapter1InteractionController>();
            float interactionDistance = controller != null ? controller.InteractionDistance : 3f;
            Vector3 targetPosition = GetInteractionTargetPosition(testObject);
            float playerDistance = Vector3.Distance(player.transform.position, targetPosition);
            if (playerDistance <= interactionDistance + 0.25f)
            {
                reporter.Pass($"{testObject.name} nằm trong vùng test tương tác ({playerDistance:0.00}m).");
            }
            else
            {
                reporter.Warning($"{testObject.name} cách Player {playerDistance:0.00}m, xa hơn interaction distance {interactionDistance:0.00}m.");
            }

            int interactableMask = LayerMask.GetMask("Interactable");
            if (interactableMask == 0)
            {
                reporter.Error("LayerMask Interactable bằng 0, không thể kiểm tra hình học tương tác.");
                return;
            }

            bool overlapDetected = false;
            Collider[] detectedColliders = Physics.OverlapSphere(player.transform.position, interactionDistance, interactableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < detectedColliders.Length; i++)
            {
                if (detectedColliders[i] != null && detectedColliders[i].transform.IsChildOf(testObject.transform))
                {
                    overlapDetected = true;
                    break;
                }
            }

            if (overlapDetected)
            {
                reporter.Pass($"{testObject.name} có thể phát hiện bằng proximity overlap.");
            }
            else
            {
                reporter.Error($"{testObject.name} không được proximity overlap với mask Interactable phát hiện.");
            }

            if (IsObstructed(player, mainCamera, targetPosition, controller))
            {
                reporter.Error($"{testObject.name} đang bị Environment obstruction chắn.");
            }
            else
            {
                reporter.Pass($"{testObject.name} không bị Environment obstruction chắn.");
            }
        }

        private static bool IsObstructed(GameObject player, Camera mainCamera, Vector3 targetPosition, Chapter1InteractionController controller)
        {
            int obstructionMask = controller != null ? GetSerializedInt(controller, "obstructionMask") : 0;
            if (obstructionMask == 0)
            {
                obstructionMask = LayerMask.GetMask("Environment");
            }

            if (obstructionMask == 0)
            {
                return false;
            }

            Vector3 origin = mainCamera != null ? mainCamera.transform.position : player.transform.position + Vector3.up * 1.4f;
            Vector3 direction = targetPosition - origin;
            float distance = direction.magnitude;
            return distance > 0.01f && Physics.Raycast(origin, direction.normalized, distance - 0.01f, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        private static void ValidateFlashlight(GameObject player, Reporter reporter)
        {
            if (player == null)
            {
                return;
            }

            Transform pivot = player.transform.Find("FlashlightPivot");
            Transform lightTransform = pivot != null ? pivot.Find("FlashlightLight") : null;
            Light light = lightTransform != null ? lightTransform.GetComponent<Light>() : null;
            FlashlightController controller = player.GetComponent<FlashlightController>();

            if (pivot != null)
            {
                reporter.Pass("Player có FlashlightPivot.");
            }
            else
            {
                reporter.Error("Player thiếu FlashlightPivot.");
            }

            if (light != null)
            {
                reporter.Pass("FlashlightLight có component Light.");
                if (light.type == LightType.Spot)
                {
                    reporter.Pass("Flashlight Light type = Spot.");
                }
                else
                {
                    reporter.Error("Flashlight Light phải là Spot.");
                }

                if (light.range >= 10f && light.range <= 14f)
                {
                    reporter.Pass($"Flashlight range hợp lệ: {light.range:0.0}.");
                }
                else
                {
                    reporter.Error($"Flashlight range {light.range:0.0}, yêu cầu khoảng 12.");
                }

                if (light.spotAngle >= 45f && light.spotAngle <= 60f)
                {
                    reporter.Pass($"Flashlight spot angle hợp lệ: {light.spotAngle:0.0}.");
                }
                else
                {
                    reporter.Error($"Flashlight spot angle {light.spotAngle:0.0}, yêu cầu khoảng 50.");
                }

                if (!light.enabled)
                {
                    reporter.Pass("Flashlight Light mặc định disabled.");
                }
                else
                {
                    reporter.Error("Flashlight Light phải disabled mặc định trước khi có đèn pin.");
                }
            }
            else
            {
                reporter.Error("Player thiếu FlashlightPivot/FlashlightLight/Light.");
            }

            if (controller != null)
            {
                ValidateSerializedObjectReference(controller, "inputReader", "FlashlightController inputReader", reporter);
                ValidateSerializedObjectReference(controller, "inputLock", "FlashlightController inputLock", reporter);
                ValidateSerializedObjectReference(controller, "inventory", "FlashlightController inventory", reporter);
                ValidateSerializedObjectReference(controller, "flashlightPivot", "FlashlightController flashlightPivot", reporter);
                ValidateSerializedObjectReference(controller, "flashlightLight", "FlashlightController flashlightLight", reporter);
            }
        }

        private static void ValidateHud(Scene scene, GameObject uiRoot, Reporter reporter)
        {
            ValidateSceneComponentCount<Chapter1HUD>(scene, "Chapter1HUD", 1, reporter);
            ValidateSceneComponentCount<InteractionPromptUI>(scene, "InteractionPromptUI", 1, reporter);
            ValidateSceneComponentCount<InventoryHUD>(scene, "InventoryHUD", 1, reporter);
            ValidateSceneComponentCount<NotificationUI>(scene, "NotificationUI", 1, reporter);
            ValidateSceneComponentCount<ObjectiveHUD>(scene, "ObjectiveHUD", 1, reporter);
            ValidateSceneComponentCount<StaminaHUD>(scene, "StaminaHUD", 1, reporter);

            Chapter1HUD hud = GetFirstSceneComponent<Chapter1HUD>(scene);
            Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.enabled)
            {
                reporter.Pass("Chapter1HUD Canvas là Screen Space Overlay và enabled.");
            }
            else
            {
                reporter.Error("Chapter1HUD Canvas thiếu, disabled hoặc không phải Screen Space Overlay.");
            }

            CanvasScaler scaler = hud != null ? hud.GetComponent<CanvasScaler>() : null;
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                reporter.Pass("Chapter1HUD CanvasScaler dùng Scale With Screen Size.");
            }
            else
            {
                reporter.Error("Chapter1HUD thiếu CanvasScaler Scale With Screen Size.");
            }

            ValidatePromptUI(GetFirstSceneComponent<InteractionPromptUI>(scene), reporter);
            ValidateInventoryUI(GetFirstSceneComponent<InventoryHUD>(scene), reporter);
            ValidateNotificationUI(GetFirstSceneComponent<NotificationUI>(scene), reporter);
            ValidateCrosshair(uiRoot, reporter);
        }

        private static void ValidatePromptUI(InteractionPromptUI promptUI, Reporter reporter)
        {
            if (promptUI == null)
            {
                return;
            }

            CanvasGroup canvasGroup = ValidateSerializedObjectReference(promptUI, "canvasGroup", "InteractionPromptUI CanvasGroup", reporter) as CanvasGroup;
            TextMeshProUGUI text = ValidateSerializedObjectReference(promptUI, "promptText", "InteractionPromptUI promptText", reporter) as TextMeshProUGUI;
            if (canvasGroup != null)
            {
                reporter.Pass("InteractionPromptUI có CanvasGroup.");
            }

            ValidateTextStyle(text, "InteractionPromptUI promptText", 26f, 36f, reporter);
            ValidateRectAnchor(promptUI.GetComponent<RectTransform>(), "InteractionPromptUI", new Vector2(0.5f, 0f), reporter);
        }

        private static void ValidateInventoryUI(InventoryHUD inventoryHUD, Reporter reporter)
        {
            if (inventoryHUD == null)
            {
                return;
            }

            ValidateSerializedObjectReference(inventoryHUD, "flashlightText", "InventoryHUD FlashlightText", reporter);
            ValidateSerializedObjectReference(inventoryHUD, "fuseText", "InventoryHUD FuseText", reporter);
            ValidateSerializedObjectReference(inventoryHUD, "canText", "InventoryHUD CanText", reporter);
            ValidateSerializedObjectReference(inventoryHUD, "hardDriveText", "InventoryHUD HardDriveText", reporter);
            ValidateTextStyle(GetSerializedObjectReference(inventoryHUD, "flashlightText") as TextMeshProUGUI, "InventoryHUD FlashlightText", 20f, 24f, reporter);
            ValidateRectAnchor(inventoryHUD.GetComponent<RectTransform>(), "InventoryHUD", new Vector2(1f, 0f), reporter);
        }

        private static void ValidateNotificationUI(NotificationUI notificationUI, Reporter reporter)
        {
            if (notificationUI == null)
            {
                return;
            }

            ValidateSerializedObjectReference(notificationUI, "canvasGroup", "NotificationUI CanvasGroup", reporter);
            TextMeshProUGUI text = ValidateSerializedObjectReference(notificationUI, "notificationText", "NotificationUI notificationText", reporter) as TextMeshProUGUI;
            ValidateTextStyle(text, "NotificationUI notificationText", 20f, 32f, reporter);
            ValidateRectAnchor(notificationUI.GetComponent<RectTransform>(), "NotificationUI", new Vector2(0.5f, 0f), reporter);
        }

        private static void ValidateCrosshair(GameObject uiRoot, Reporter reporter)
        {
            GameObject crosshair = uiRoot != null ? FindChildRecursive(uiRoot.transform, "Crosshair")?.gameObject : null;
            if (crosshair == null)
            {
                reporter.Error("HUD thiếu Crosshair.");
                return;
            }

            Image image = crosshair.GetComponent<Image>();
            RectTransform rect = crosshair.GetComponent<RectTransform>();
            if (image != null && rect != null && rect.sizeDelta.x >= 6f && rect.sizeDelta.y >= 6f && !image.raycastTarget)
            {
                reporter.Pass("Crosshair tồn tại, có Image nhỏ giữa màn hình và không chặn raycast.");
            }
            else
            {
                reporter.Error("Crosshair thiếu Image/RectTransform hợp lệ hoặc đang Raycast Target.");
            }
        }

        private static void ValidateEventSystem(Scene scene, EventSystem eventSystem, Reporter reporter)
        {
            List<EventSystem> eventSystems = GetSceneComponents<EventSystem>(scene);
            if (eventSystems.Count == 1)
            {
                reporter.Pass("Scene có đúng một EventSystem.");
            }
            else
            {
                reporter.Error($"Scene có {eventSystems.Count} EventSystem, yêu cầu đúng 1.");
            }

            if (eventSystem == null)
            {
                return;
            }

            if (eventSystem.gameObject.activeInHierarchy)
            {
                reporter.Pass("EventSystem active.");
            }
            else
            {
                reporter.Error("EventSystem không active.");
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() != null)
            {
                reporter.Pass("EventSystem dùng InputSystemUIInputModule.");
            }
            else
            {
                reporter.Error("EventSystem thiếu InputSystemUIInputModule.");
            }

            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                reporter.Pass("EventSystem không dùng StandaloneInputModule cũ.");
            }
            else
            {
                reporter.Error("EventSystem còn StandaloneInputModule cũ.");
            }
        }

        private static void ValidateRuntimeSelfTest(GameObject testGroup, Reporter reporter)
        {
            Chapter1InteractionRuntimeSelfTest selfTest = testGroup != null ? testGroup.GetComponent<Chapter1InteractionRuntimeSelfTest>() : null;
            if (selfTest != null)
            {
                reporter.Pass("InteractionInventoryTest có Chapter1InteractionRuntimeSelfTest.");
            }
            else
            {
                reporter.Error("InteractionInventoryTest thiếu Chapter1InteractionRuntimeSelfTest.");
            }
        }

        private static T ValidateSingleComponent<T>(GameObject gameObject, string context, Reporter reporter) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            if (components.Length == 1)
            {
                reporter.Pass($"{context} có đúng 1 component {typeof(T).Name}.");
                return components[0];
            }

            reporter.Error($"{context} có {components.Length} component {typeof(T).Name}, yêu cầu đúng 1.");
            return components.Length > 0 ? components[0] : null;
        }

        private static void ValidateSceneComponentCount<T>(Scene scene, string label, int expectedCount, Reporter reporter) where T : Component
        {
            int count = GetSceneComponents<T>(scene).Count;
            if (count == expectedCount)
            {
                reporter.Pass($"Scene có đúng {expectedCount} {label}.");
            }
            else
            {
                reporter.Error($"Scene có {count} {label}, yêu cầu đúng {expectedCount}.");
            }
        }

        private static Object ValidateSerializedObjectReference(Object target, string fieldName, string label, Reporter reporter)
        {
            Object value = GetSerializedObjectReference(target, fieldName);
            if (value != null)
            {
                reporter.Pass($"{label} đã được gán.");
                return value;
            }

            reporter.Error($"{label} chưa được gán.");
            return null;
        }

        private static void ValidateLayerMaskIncludes(Object target, string fieldName, string layerName, string label, Reporter reporter)
        {
            int layer = LayerMask.NameToLayer(layerName);
            int value = GetSerializedInt(target, fieldName);
            if (layer >= 0 && (value & (1 << layer)) != 0)
            {
                reporter.Pass($"{label} gồm layer {layerName}.");
            }
            else
            {
                reporter.Error($"{label} thiếu layer {layerName}.");
            }
        }

        private static void ValidateInteractableLayer(GameObject testObject, Collider[] colliders, Reporter reporter)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0)
            {
                reporter.Error("Layer Interactable chưa tồn tại.");
                return;
            }

            if (testObject.layer == interactableLayer)
            {
                reporter.Pass($"{testObject.name} root ở layer Interactable.");
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].gameObject.layer == interactableLayer)
                {
                    reporter.Pass($"{testObject.name} có collider child ở layer Interactable.");
                    return;
                }
            }

            reporter.Error($"{testObject.name} root/collider child chưa ở layer Interactable.");
        }

        private static void ValidateTextStyle(TextMeshProUGUI text, string label, float minFontSize, float maxFontSize, Reporter reporter)
        {
            if (text == null)
            {
                return;
            }

            if (text.color.a > 0f)
            {
                reporter.Pass($"{label} alpha chữ > 0.");
            }
            else
            {
                reporter.Error($"{label} alpha chữ đang 0.");
            }

            if (text.fontSize >= minFontSize && text.fontSize <= maxFontSize)
            {
                reporter.Pass($"{label} font size hợp lệ: {text.fontSize:0}.");
            }
            else
            {
                reporter.Error($"{label} font size {text.fontSize:0}, yêu cầu {minFontSize:0}-{maxFontSize:0}.");
            }
        }

        private static void ValidateRectAnchor(RectTransform rect, string label, Vector2 expectedAnchor, Reporter reporter)
        {
            if (rect == null)
            {
                reporter.Error($"{label} thiếu RectTransform.");
                return;
            }

            if (Vector2.Distance(rect.anchorMin, expectedAnchor) < 0.01f && Vector2.Distance(rect.anchorMax, expectedAnchor) < 0.01f)
            {
                reporter.Pass($"{label} anchor đúng.");
            }
            else
            {
                reporter.Error($"{label} anchorMin/anchorMax chưa đúng.");
            }
        }

        private static Object GetSerializedObjectReference(Object target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static float GetSerializedFloat(Object target, string fieldName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return property != null ? property.floatValue : 0f;
        }

        private static int GetSerializedInt(Object target, string fieldName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return property != null ? property.intValue : 0;
        }

        private static bool GetSerializedBool(Object target, string fieldName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return property != null && property.boolValue;
        }

        private static bool HasBinding(InputAction action, string bindingPath)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                string effectivePath = action.bindings[i].effectivePath;
                string path = action.bindings[i].path;
                if (string.Equals(effectivePath, bindingPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(path, bindingPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribeAction(InputAction action)
        {
            if (action == null)
            {
                return "<null>";
            }

            string mapName = action.actionMap != null ? action.actionMap.name : "<no map>";
            return $"{mapName}/{action.name}";
        }

        private static IChapter1Interactable GetInteractable(GameObject gameObject)
        {
            MonoBehaviour[] behaviours = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IChapter1Interactable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        private static Vector3 GetInteractionTargetPosition(GameObject gameObject)
        {
            Transform interactionPoint = FindChildRecursive(gameObject.transform, "InteractionPoint");
            if (interactionPoint != null)
            {
                return interactionPoint.position;
            }

            Collider collider = gameObject.GetComponentInChildren<Collider>(true);
            return collider != null ? collider.bounds.center : gameObject.transform.position;
        }

        private static Camera FindMainCamera(Scene scene, Reporter reporter)
        {
            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            Camera taggedCamera = null;
            int taggedCount = 0;
            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i] != null && cameras[i].CompareTag("MainCamera"))
                {
                    taggedCamera = cameras[i];
                    taggedCount++;
                }
            }

            if (taggedCount == 1)
            {
                reporter.Pass("Scene có đúng một Camera tag MainCamera.");
            }
            else
            {
                reporter.Error($"Scene có {taggedCount} Camera tag MainCamera, yêu cầu đúng 1.");
            }

            return taggedCamera;
        }

        private static GameObject FindScenePlayer(Scene scene)
        {
            GameObject namedPlayer = FindSceneObject(scene, "Player");
            if (namedPlayer != null)
            {
                return namedPlayer;
            }

            Chapter1InputReader inputReader = GetFirstSceneComponent<Chapter1InputReader>(scene);
            return inputReader != null ? inputReader.gameObject : null;
        }

        private static T GetFirstSceneComponent<T>(Scene scene) where T : Component
        {
            List<T> components = GetSceneComponents<T>(scene);
            return components.Count > 0 ? components[0] : null;
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            List<GameObject> roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            for (int i = 0; i < roots.Count; i++)
            {
                Transform match = FindChildRecursive(roots[i].transform, objectName);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform match = FindChildRecursive(child, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
