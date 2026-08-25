using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter1.Tests
{
    public sealed class HenryFightEncounterEditModeTests
    {
        private const string PlayerPrefabPath =
            "Assets/Chapter1/Prefabs/Characters/Player.prefab";

        private const string ChapterSceneRelativePath =
            "Chapter1/Scenes/Chapter1_Dormitory.unity";

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
    }
}
