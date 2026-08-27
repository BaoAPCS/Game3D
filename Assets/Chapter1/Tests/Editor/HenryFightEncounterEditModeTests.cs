using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class HenryFightEncounterEditModeTests
    {
        private const string PlayerPrefabPath =
            "Assets/Chapter1/Prefabs/Characters/Player.prefab";

        private const string ChapterSceneRelativePath =
            "Chapter1/Scenes/Chapter1_Dormitory.unity";

        private const string FightControllerRelativePath =
            "Chapter1/Scripts/World/HenryFightEncounterController.cs";

        private const string PoliceSirenGuid =
            "488c9851604b9e7478527dff42f59f05";

        [Test]
        public void PlayerFightDamageMatchesApprovedBalance()
        {
            GameObject player =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.NotNull(player);

            PlayerCombatController combat =
                player.GetComponent<PlayerCombatController>();
            Assert.NotNull(combat);
            Assert.AreEqual(4, combat.ComboAttacks.Count);
            for (int i = 0; i < combat.ComboAttacks.Count; i++)
            {
                Assert.AreEqual(
                    10f,
                    combat.ComboAttacks[i].damage,
                    $"Hand attack {i} must use ordinary fight damage.");
            }

            SerializedObject serializedCombat =
                new SerializedObject(combat);
            Assert.AreEqual(
                10f,
                serializedCombat.FindProperty(
                    "neutralKickAttack.damage").floatValue);
            Assert.AreEqual(
                10f,
                serializedCombat.FindProperty(
                    "forwardKickAttack.damage").floatValue);
            Assert.AreEqual(
                30f,
                serializedCombat.FindProperty(
                    "backwardKickAttack.damage").floatValue);
        }

        [Test]
        public void HenryDefeatSaveRepairsMissionThreePrerequisites()
        {
            Chapter1SaveData data = new Chapter1SaveData
            {
                Mission03HenryDefeated = true,
                Mission03HenryConfrontationCompleted = false,
                Mission03PoliceKeyReceived = false,
                Mission03ChallengePassed = false,
                Mission03JamesIntroPlayed = false,
                Mission03GangHostile = true
            };

            data.EnsureValidDefaults();

            Assert.IsTrue(data.Mission03HenryDefeated);
            Assert.IsTrue(data.Mission03HenryConfrontationCompleted);
            Assert.IsTrue(data.Mission03PoliceKeyReceived);
            Assert.IsTrue(data.Mission03ChallengePassed);
            Assert.IsTrue(data.Mission03JamesIntroPlayed);
            Assert.IsFalse(data.Mission03GangHostile);
        }

        [Test]
        public void PoliceArrestSaveCompletesChapterWithoutInventingVictory()
        {
            Chapter1SaveData data = new Chapter1SaveData
            {
                Mission02Started = false,
                Mission02EquipmentDelivered = false,
                Mission02HasPsu = false,
                Mission02HasUps = false,
                Mission02HasHenryBattery = false,
                Mission03JamesIntroPlayed = false,
                Mission03ChallengePassed = false,
                Mission03GangHostile = true,
                Mission03PoliceKeyReceived = false,
                Mission03HenryConfrontationCompleted = false,
                Mission03HenryDefeated = false,
                Mission03PoliceArrestCompleted = true
            };

            data.EnsureValidDefaults();

            Assert.IsTrue(data.Mission03PoliceArrestCompleted);
            Assert.IsFalse(data.Mission03HenryDefeated);
            Assert.IsTrue(data.Mission03HenryConfrontationCompleted);
            Assert.IsTrue(data.Mission03PoliceKeyReceived);
            Assert.IsTrue(data.Mission03ChallengePassed);
            Assert.IsTrue(data.Mission03JamesIntroPlayed);
            Assert.IsFalse(data.Mission03GangHostile);
            Assert.IsTrue(data.Mission02Started);
            Assert.IsTrue(data.Mission02EquipmentDelivered);
            Assert.IsTrue(data.Mission02HasPsu);
            Assert.IsTrue(data.Mission02HasUps);
            Assert.IsTrue(data.Mission02HasHenryBattery);
            Assert.IsTrue(data.ChapterCompleted);
            Assert.AreEqual(
                Chapter1Step.ChapterCompleted,
                data.CurrentStep);
        }

        [Test]
        public void VersionFiveCarArrivalMigratesToNewOfficerPursuit()
        {
            Chapter1SaveData data = new Chapter1SaveData
            {
                SaveVersion = 5,
                Mission03HenryDefeated = false,
                Mission03PoliceArrestCompleted = true,
                ChapterCompleted = false
            };

            data.EnsureValidDefaults();

            Assert.AreEqual(7, data.SaveVersion);
            Assert.IsTrue(data.Mission03HenryDefeated);
            Assert.IsFalse(data.Mission03PoliceArrestCompleted);
            Assert.IsFalse(data.ChapterCompleted);
            Assert.IsTrue(data.Mission03HenryConfrontationCompleted);
        }

        [Test]
        public void ChapterManagerRepairsPartiallyWrittenTerminalStep()
        {
            GameObject managerObject =
                new GameObject("ChapterManagerTerminalRepairTest");
            managerObject.SetActive(false);
            try
            {
                Chapter1Manager manager =
                    managerObject.AddComponent<Chapter1Manager>();
                SerializedObject serializedManager =
                    new SerializedObject(manager);
                serializedManager.FindProperty("autoLoadOnAwake")
                    .boolValue = false;
                serializedManager.FindProperty("autoSaveOnMilestones")
                    .boolValue = false;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                managerObject.SetActive(true);

                manager.CurrentData.CurrentStep =
                    Chapter1Step.ChapterCompleted;
                manager.CurrentData.ChapterCompleted = false;

                Assert.IsTrue(manager.AdvanceTo(
                    Chapter1Step.ChapterCompleted));
                Assert.IsTrue(manager.CurrentData.ChapterCompleted);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void ChapterSceneHasExactInactivePoliceCarWithThreeDimensionalSiren()
        {
            string scenePath = Path.Combine(
                Application.dataPath,
                ChapterSceneRelativePath);
            Assert.IsTrue(File.Exists(scenePath), scenePath);

            string sceneYaml = File.ReadAllText(scenePath);
            MatchCollection policeCarMatches = Regex.Matches(
                sceneYaml,
                @"(?ms)^--- !u!1001 &\d+\r?$\nPrefabInstance:.*?" +
                @"propertyPath: m_Name\s+value: police_car\s+" +
                @"objectReference: \{fileID: 0\}.*?" +
                @"propertyPath: m_IsActive\s+value: 0\s+" +
                @"objectReference: \{fileID: 0\}.*?" +
                @"m_AddedComponents:(?<addedComponents>.*?)" +
                @"m_SourcePrefab:");
            Assert.AreEqual(
                1,
                policeCarMatches.Count,
                "The scene must contain one exact inactive police_car root.");

            Match audioSourceMatch = Regex.Match(
                sceneYaml,
                @"(?ms)^--- !u!82 &(?<audioId>\d+)\r?$\n" +
                @"AudioSource:.*?m_Resource: \{fileID: 8300000, guid: " +
                PoliceSirenGuid +
                @", type: 3\}.*?(?=^--- !u!|\z)");
            Assert.IsTrue(
                audioSourceMatch.Success,
                "police_car must reference the authored police siren clip.");

            string audioId = audioSourceMatch.Groups["audioId"].Value;
            string addedComponents = policeCarMatches[0]
                .Groups["addedComponents"].Value;
            StringAssert.Contains(
                $"addedObject: {{fileID: {audioId}}}",
                addedComponents,
                "The configured AudioSource must belong to police_car.");

            string audioSourceYaml = audioSourceMatch.Value;
            StringAssert.Contains("m_PlayOnAwake: 0", audioSourceYaml);
            StringAssert.Contains("Loop: 1", audioSourceYaml);
            Assert.IsTrue(
                Regex.IsMatch(
                    audioSourceYaml,
                    @"(?ms)panLevelCustomCurve:.*?time: 0\s+value: 1"),
                "The siren AudioSource must use Spatial Blend 3D (1.0)." );
        }

        [Test]
        public void PoliceArrestRuntimeApisAndFightStatesAreAvailable()
        {
            Assert.AreEqual(
                12f,
                PoliceArrestSequenceController.PoliceCarSpeed);
            Assert.AreEqual(
                4.1f,
                PoliceArrestSequenceController.PoliceCarStopOffset);
            Assert.AreEqual(
                15f,
                PoliceArrestSequenceController.PoliceCarTimeout);
            Assert.Greater(
                PoliceOfficerArrestController.PoliceRunSpeed,
                6f);
            Assert.AreEqual(
                1.2f,
                PoliceOfficerArrestController.CaptureDistance);

            Assert.IsTrue(Enum.IsDefined(
                typeof(HenryFightEncounterController.FightState),
                "PoliceArriving"));
            Assert.IsTrue(Enum.IsDefined(
                typeof(HenryFightEncounterController.FightState),
                "PolicePursuing"));
            Assert.IsTrue(Enum.IsDefined(
                typeof(HenryFightEncounterController.FightState),
                "PoliceArrested"));

            MethodInfo inputModeMethod =
                typeof(Chapter1InputReader).GetMethod(
                    "SetPoliceArrestMode",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(bool) },
                    null);
            Assert.NotNull(inputModeMethod);

            MethodInfo beginArrestMethod =
                typeof(PoliceArrestSequenceController).GetMethod(
                    "BeginArrest",
                    BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(beginArrestMethod);
            Assert.NotNull(
                typeof(PoliceOfficerArrestController).GetMethod(
                    "BeginPursuit",
                    BindingFlags.Public | BindingFlags.Instance));
            Assert.NotNull(
                typeof(PoliceOfficerArrestController).GetMethod(
                    "RestoreTerminalState",
                    BindingFlags.Public | BindingFlags.Instance));

            Type missionProgressType = typeof(UPSInteractable).Assembly
                .GetType("DormitoryMystery.Chapter1.Mission3Progress");
            Assert.NotNull(missionProgressType);
            Assert.NotNull(missionProgressType.GetProperty(
                "PoliceArrestCompleted",
                BindingFlags.Public | BindingFlags.Static));
            Assert.NotNull(missionProgressType.GetMethod(
                "TryMarkPoliceArrestCompleted",
                BindingFlags.Public | BindingFlags.Static));
        }

        [Test]
        public void FightOutcomeStartsPoliceWithoutLockingWinnerOrGameOver()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                FightControllerRelativePath);
            Assert.IsTrue(File.Exists(sourcePath), sourcePath);

            string source = File.ReadAllText(sourcePath);
            string playerDied = ExtractMethodBody(
                source,
                "private void HandlePlayerDied()");
            string henryDied = ExtractMethodBody(
                source,
                "private void HandleHenryDied()");
            string scheduleOutcome = ExtractMethodBody(
                source,
                "private void ScheduleOutcomeResolution()");
            string playerDefeat = ExtractMethodBody(
                source,
                "private IEnumerator FinishPlayerDefeat()");
            string henryDefeat = ExtractMethodBody(
                source,
                "private IEnumerator FinishHenryDefeat()");

            StringAssert.Contains(
                "ScheduleOutcomeResolution();",
                playerDied);
            StringAssert.Contains(
                "ScheduleOutcomeResolution();",
                henryDied);
            int outcomePending = scheduleOutcome.IndexOf(
                "outcomePending = true;",
                StringComparison.Ordinal);
            int deleteTestSave = scheduleOutcome.IndexOf(
                "Chapter1Manager.Instance?.DeleteTestSaveForNextSession();",
                StringComparison.Ordinal);
            int beginPolice = scheduleOutcome.IndexOf(
                "BeginPoliceArrest();",
                StringComparison.Ordinal);
            int startResolver = scheduleOutcome.IndexOf(
                "StartCoroutine(ResolveOutcomeNextFrame())",
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(deleteTestSave, 0);
            Assert.Greater(
                outcomePending,
                deleteTestSave,
                "The test save must be deleted before terminal state changes " +
                "can trigger persistence.");
            Assert.GreaterOrEqual(outcomePending, 0);
            Assert.Greater(beginPolice, outcomePending);
            Assert.Greater(
                startResolver,
                beginPolice,
                "Police must start before the simultaneous-KO yield.");
            StringAssert.DoesNotContain(
                "EnterPoliceArrestInputMode(false);",
                scheduleOutcome);

            int playerWait = playerDefeat.IndexOf(
                "while (!policeArrivalResolved)",
                StringComparison.Ordinal);
            int playerArrestComplete = playerDefeat.IndexOf(
                "CompletePoliceArrest();",
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(playerWait, 0);
            Assert.Greater(
                playerArrestComplete,
                playerWait);
            StringAssert.DoesNotContain(
                "Chapter1EventBus.RaiseGameOver(",
                playerDefeat);

            int henryWait = henryDefeat.IndexOf(
                "while (!policeArrivalResolved)",
                StringComparison.Ordinal);
            int arrestComplete = henryDefeat.IndexOf(
                "CompletePoliceArrest();",
                StringComparison.Ordinal);
            int unlockWinner = henryDefeat.IndexOf(
                "RestorePlayerAfterVictory();",
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(unlockWinner, 0);
            Assert.GreaterOrEqual(henryWait, 0);
            Assert.Greater(arrestComplete, henryWait);
            StringAssert.DoesNotContain(
                "Chapter1EventBus.RaiseGameOver(",
                henryDefeat);
            StringAssert.DoesNotContain(
                "Chapter1EventBus.RaiseGameOver(",
                source,
                "A combat result must continue directly into the police " +
                "sequence instead of exposing the R-to-retry presenter.");
            StringAssert.DoesNotContain(
                "Chapter1Manager.Instance?.ResetChapter()",
                source);
        }

        [Test]
        public void FailedPoliceArrivalDoesNotClaimNamWasArrested()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                FightControllerRelativePath);
            Assert.IsTrue(File.Exists(sourcePath), sourcePath);

            string methodBody = ExtractMethodBody(
                File.ReadAllText(sourcePath),
                "private void CompletePoliceArrest()");
            int failedGuard = methodBody.IndexOf(
                "if (!policeArrivalSucceeded)",
                StringComparison.Ordinal);
            int failedReturn = methodBody.IndexOf(
                "return;",
                failedGuard,
                StringComparison.Ordinal);
            int arrestedState = methodBody.IndexOf(
                "state = FightState.PoliceArrested;",
                StringComparison.Ordinal);
            int arrestedNotification = methodBody.IndexOf(
                "PoliceArrestedNotification",
                StringComparison.Ordinal);

            Assert.GreaterOrEqual(failedGuard, 0);
            Assert.Greater(failedReturn, failedGuard);
            Assert.Greater(arrestedState, failedReturn);
            Assert.Greater(arrestedNotification, arrestedState);
        }

        [Test]
        public void ChapterCompletionIsReportedOnlyAfterProgressCommit()
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                FightControllerRelativePath);
            string methodBody = ExtractMethodBody(
                File.ReadAllText(sourcePath),
                "private void CompletePoliceArrest()");

            int progressCommit = methodBody.IndexOf(
                "Mission3Progress.TryMarkPoliceArrestCompleted()",
                StringComparison.Ordinal);
            int failedReturn = methodBody.IndexOf(
                "return;",
                progressCommit,
                StringComparison.Ordinal);
            int arrestedState = methodBody.IndexOf(
                "state = FightState.PoliceArrested;",
                StringComparison.Ordinal);
            int notification = methodBody.IndexOf(
                "PoliceArrestedNotification",
                StringComparison.Ordinal);

            Assert.GreaterOrEqual(progressCommit, 0);
            Assert.Greater(failedReturn, progressCommit);
            Assert.Greater(arrestedState, failedReturn);
            Assert.Greater(notification, arrestedState);
        }

        [Test]
        public void FightHudTracksBothHealthSourcesWithoutDuplication()
        {
            GameObject nam = new GameObject("NamHudTest");
            GameObject henry = new GameObject("HenryHudTest");
            FightCombatHUD hud = null;
            try
            {
                CombatHealth namHealth = nam.AddComponent<CombatHealth>();
                CombatHealth henryHealth =
                    henry.AddComponent<CombatHealth>();

                hud = FightCombatHUD.EnsureRuntimeHUD();
                FightCombatHUD sameHud =
                    FightCombatHUD.EnsureRuntimeHUD();
                Assert.AreSame(hud, sameHud);
                Assert.IsFalse(hud.IsVisible);

                hud.Bind(namHealth, henryHealth);
                hud.Show();
                namHealth.TakeDamage(25f);
                henryHealth.TakeDamage(30f);

                Image namFill = hud.transform.Find(
                    "FightCombatHUD/NamPanel/HealthBarBackground/Fill")
                    .GetComponent<Image>();
                Image henryFill = hud.transform.Find(
                    "FightCombatHUD/HenryPanel/HealthBarBackground/Fill")
                    .GetComponent<Image>();

                Assert.IsTrue(hud.IsVisible);
                Assert.NotNull(namFill.sprite);
                Assert.NotNull(henryFill.sprite);
                Assert.AreEqual(
                    "FightCombatHUD_WhiteSprite",
                    namFill.sprite.name);
                Assert.AreEqual(0.75f, namFill.fillAmount, 0.001f);
                Assert.AreEqual(0.70f, henryFill.fillAmount, 0.001f);

                hud.Hide();
                Assert.IsFalse(hud.IsVisible);
            }
            finally
            {
                if (hud != null)
                {
                    Object.DestroyImmediate(hud.gameObject);
                }

                Object.DestroyImmediate(nam);
                Object.DestroyImmediate(henry);
            }
        }

        [Test]
        public void FightTuningUsesApprovedValues()
        {
            Assert.AreEqual(
                6.5f,
                HenryFightEncounterController.HenryFightSpeed);
            Assert.AreEqual(
                1.35f,
                HenryFightEncounterController.HenryAttackRange);
            Assert.AreEqual(
                0.35f,
                HenryFightEncounterController.HenryAttackRecovery);
        }

        [Test]
        public void HenryUsesPunchThenBothKicksWithSharedDamage()
        {
            MethodInfo getNextAttack =
                typeof(HenryFightEncounterController).GetMethod(
                    "GetNextAttack",
                    BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(getNextAttack);
            Assert.AreEqual(
                HenryCombatAttack.MmaKick,
                getNextAttack.Invoke(
                    null,
                    new object[] { HenryCombatAttack.Punch }));
            Assert.AreEqual(
                HenryCombatAttack.RoundhouseKick,
                getNextAttack.Invoke(
                    null,
                    new object[] { HenryCombatAttack.MmaKick }));
            Assert.AreEqual(
                HenryCombatAttack.Punch,
                getNextAttack.Invoke(
                    null,
                    new object[] { HenryCombatAttack.RoundhouseKick }));

            GameObject owner = new GameObject("HenryCombatDamageTest");
            owner.SetActive(false);
            try
            {
                HenryCombatHitboxController combat =
                    owner.AddComponent<HenryCombatHitboxController>();
                SerializedObject serializedCombat =
                    new SerializedObject(combat);
                Assert.AreEqual(
                    20f,
                    serializedCombat.FindProperty("attackDamage").floatValue);
                Assert.IsNull(serializedCombat.FindProperty("mmaKickDamage"));
                Assert.IsNull(
                    serializedCombat.FindProperty("roundhouseKickDamage"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ChapterSceneDoesNotContainLegacyFightArenaObjects()
        {
            string scenePath = Path.Combine(
                Application.dataPath,
                ChapterSceneRelativePath);
            Assert.IsTrue(File.Exists(scenePath), scenePath);

            string sceneYaml = File.ReadAllText(scenePath);
            StringAssert.DoesNotContain("m_Name: FightArea", sceneYaml);
            StringAssert.DoesNotContain("m_Name: Wall1", sceneYaml);
            StringAssert.DoesNotContain("m_Name: Wall3", sceneYaml);
            StringAssert.DoesNotContain("m_Name: Wall4", sceneYaml);
        }

        [Test]
        public void CombatModeKeepsDoorInputAndRejectsOtherInteractions()
        {
            GameObject player = new GameObject("CombatDoorInputTest");
            GameObject doorObject = new GameObject("CombatDoorTest");
            player.SetActive(false);
            doorObject.SetActive(false);

            InputAction interactAction = new InputAction(
                "Interact",
                InputActionType.Button);
            InputAction talkAction = new InputAction(
                "Talk",
                InputActionType.Button);
            InputActionReference interactReference =
                InputActionReference.Create(interactAction);
            InputActionReference talkReference =
                InputActionReference.Create(talkAction);
            try
            {
                Chapter1InputReader inputReader =
                    player.AddComponent<Chapter1InputReader>();
                Chapter1InteractionController interactionController =
                    player.AddComponent<Chapter1InteractionController>();
                doorObject.AddComponent<Animator>();
                DoorInteractable door =
                    doorObject.AddComponent<DoorInteractable>();

                SetPrivateField(
                    inputReader,
                    "interactActionReference",
                    interactReference);
                SetPrivateField(
                    inputReader,
                    "talkActionReference",
                    talkReference);

                MethodInfo isCombatAllowedAction =
                    typeof(Chapter1InputReader).GetMethod(
                        "IsCombatAllowedAction",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isCombatAllowedAction);
                Assert.IsTrue((bool)isCombatAllowedAction.Invoke(
                    inputReader,
                    new object[] { interactAction }));
                Assert.IsFalse((bool)isCombatAllowedAction.Invoke(
                    inputReader,
                    new object[] { talkAction }));

                int interactPressCount = 0;
                inputReader.InteractPressed += () => interactPressCount++;
                inputReader.SetCombatOnlyMode(true);
                MethodInfo onInteractPerformed =
                    typeof(Chapter1InputReader).GetMethod(
                        "OnInteractPerformed",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(onInteractPerformed);
                onInteractPerformed.Invoke(
                    inputReader,
                    new object[] { default(InputAction.CallbackContext) });
                Assert.AreEqual(1, interactPressCount);

                interactionController.SetCombatDoorOnlyMode(true);
                Assert.IsTrue(interactionController.CombatDoorOnlyMode);

                MethodInfo isAllowedInteraction =
                    typeof(Chapter1InteractionController).GetMethod(
                        "IsAllowedInCurrentInteractionMode",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isAllowedInteraction);
                Assert.IsTrue((bool)isAllowedInteraction.Invoke(
                    interactionController,
                    new object[] { door }));
                Assert.IsFalse((bool)isAllowedInteraction.Invoke(
                    interactionController,
                    new object[] { new FakeInteractable() }));

                interactionController.SetCombatDoorOnlyMode(false);
                Assert.IsTrue((bool)isAllowedInteraction.Invoke(
                    interactionController,
                    new object[] { new FakeInteractable() }));
            }
            finally
            {
                Object.DestroyImmediate(interactReference);
                Object.DestroyImmediate(talkReference);
                interactAction.Dispose();
                talkAction.Dispose();
                Object.DestroyImmediate(doorObject);
                Object.DestroyImmediate(player);
            }
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static string ExtractMethodBody(
            string source,
            string signature)
        {
            int signatureIndex = source.IndexOf(
                signature,
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(
                signatureIndex,
                0,
                $"Method signature not found: {signature}");

            int openingBrace = source.IndexOf('{', signatureIndex);
            Assert.Greater(openingBrace, signatureIndex);

            int depth = 0;
            for (int i = openingBrace; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(
                            openingBrace + 1,
                            i - openingBrace - 1);
                    }
                }
            }

            Assert.Fail($"Method body is not balanced: {signature}");
            return string.Empty;
        }

        private sealed class FakeInteractable : IChapter1Interactable
        {
            public bool IsInteractionEnabled => true;
            public Chapter1InteractionInput InteractionInput =>
                Chapter1InteractionInput.Interact;

            public string GetInteractionPrompt(InteractionContext context)
            {
                return "Fake";
            }

            public bool CanInteract(InteractionContext context)
            {
                return true;
            }

            public InteractionResult Interact(InteractionContext context)
            {
                return InteractionResult.Ignored();
            }

            public Transform GetInteractionTransform()
            {
                return null;
            }
        }
    }
}
