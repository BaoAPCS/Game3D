using System;
using System.IO;
using DormitoryMystery.Chapter1;
using DormitoryMystery.Chapter2;
using NUnit.Framework;
using UnityEngine;

namespace DormitoryMystery.Menu.Tests
{
    public sealed class GameProgressStoreEditModeTests
    {
        private const string TemporaryFolderPrefix = "Game3D.MenuProgressTests.";
        private string temporaryDirectory;
        private string chapter1Path;
        private string chapter2Path;
        private bool completed;
        private int completionClearCount;
        private GameProgressStore store;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(), TemporaryFolderPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            chapter1Path = Path.Combine(temporaryDirectory, "chapter1_save.json");
            chapter2Path = Path.Combine(temporaryDirectory, "chapter2_save.json");
            completed = false;
            completionClearCount = 0;
            // All persistence, including the completion marker, is isolated from the player.
            store = new GameProgressStore(chapter1Path, chapter2Path,
                () => completed,
                () => { completed = false; completionClearCount++; });
        }

        [TearDown]
        public void TearDown()
        {
            if (string.IsNullOrEmpty(temporaryDirectory) || !Directory.Exists(temporaryDirectory))
                return;

            string resolvedDirectory = Path.GetFullPath(temporaryDirectory);
            string resolvedParent = Path.GetFullPath(Path.GetTempPath())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string directoryName = Path.GetFileName(resolvedDirectory);
            // Refuse recursive cleanup unless this is our unique, direct child of temp.
            Assert.That(Path.GetDirectoryName(resolvedDirectory),
                Is.EqualTo(resolvedParent).IgnoreCase);
            Assert.That(directoryName, Does.StartWith(TemporaryFolderPrefix));
            Assert.That(Guid.TryParseExact(directoryName.Substring(TemporaryFolderPrefix.Length),
                "N", out _), Is.True);
            Directory.Delete(resolvedDirectory, true);
        }

        [Test]
        public void MissingSavesDisableContinueAndDoNotCreateFiles()
        {
            Assert.That(store.TryGetContinueScene(out string scene), Is.False);
            Assert.That(scene, Is.Null);
            Assert.That(Directory.GetFiles(temporaryDirectory), Is.Empty);
            Assert.That(completionClearCount, Is.Zero);
        }

        [TestCase("not json")]
        [TestCase("{")]
        [TestCase("{}")]
        [TestCase("{\"CurrentCheckpointId\":1}")]
        [TestCase("null")]
        public void DamagedOrUnversionedChapter1IsRejectedWithoutChangingItsContents(string json)
        {
            File.WriteAllText(chapter1Path, json);

            Assert.That(store.TryGetContinueScene(out string scene), Is.False);
            Assert.That(scene, Is.Null);
            Assert.That(File.ReadAllText(chapter1Path), Is.EqualTo(json));
        }

        [TestCase(0)]
        [TestCase(6)]
        [TestCase(Chapter1SaveData.CurrentSaveVersion + 1)]
        public void IncompatibleChapter1VersionIsRejectedWithoutDeletingSave(int version)
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.SaveVersion = version;
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(chapter1Path, json);

            Assert.That(store.TryGetContinueScene(out _), Is.False);
            Assert.That(File.ReadAllText(chapter1Path), Is.EqualTo(json));
        }

        [Test]
        public void InvalidChapter1CheckpointIsRejectedWithoutDeletingSave()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.CurrentCheckpointId = (Chapter1MissionCheckpoint)9999;
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(chapter1Path, json);

            Assert.That(store.TryGetContinueScene(out _), Is.False);
            Assert.That(File.ReadAllText(chapter1Path), Is.EqualTo(json));
        }

        [TestCase(Chapter1MissionCheckpoint.Mission1Start)]
        [TestCase(Chapter1MissionCheckpoint.Mission2Start)]
        [TestCase(Chapter1MissionCheckpoint.Mission3Start)]
        public void ValidChapter1CheckpointResumesChapter1AndPreservesSnapshot(
            Chapter1MissionCheckpoint checkpoint)
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.CurrentCheckpointId = checkpoint;
            new JsonChapter1SaveService(chapter1Path).Save(data);
            string original = File.ReadAllText(chapter1Path);

            AssertScene(GameSessionFlow.Chapter1ScenePath);
            Assert.That(File.ReadAllText(chapter1Path), Is.EqualTo(original));
            Assert.That(new JsonChapter1SaveService(chapter1Path).Load().CurrentCheckpointId,
                Is.EqualTo(checkpoint));
        }

        [TestCase(7)]
        [TestCase(8)]
        public void CompatibleLegacyChapter1CanResumeWithoutRewritingItsSchema(int version)
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.SaveVersion = version;
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(chapter1Path, json);

            AssertScene(GameSessionFlow.Chapter1ScenePath);
            Assert.That(File.ReadAllText(chapter1Path), Is.EqualTo(json));
        }

        [Test]
        public void CompletedChapter1ResumesChapter2WithoutCreatingItsSave()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.ChapterCompleted = true;
            data.Mission03PoliceArrestCompleted = true;
            new JsonChapter1SaveService(chapter1Path).Save(data);

            AssertScene(GameSessionFlow.Chapter2ScenePath);
            Assert.That(File.Exists(chapter2Path), Is.False);
        }

        [Test]
        public void ChapterCompletedFlagAloneDoesNotSkipTheChapter1ArrestCheckpoint()
        {
            Chapter1SaveData data = Chapter1SaveData.CreateDefault();
            data.ChapterCompleted = true;
            File.WriteAllText(chapter1Path, JsonUtility.ToJson(data));

            AssertScene(GameSessionFlow.Chapter1ScenePath);
        }

        [Test]
        public void ValidChapter2TakesPrecedenceOverAnUnfinishedChapter1Save()
        {
            new JsonChapter1SaveService(chapter1Path).Save(Chapter1SaveData.CreateDefault());
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();
            data.Mission04ComputerUnlocked = true;
            new JsonChapter2SaveService(chapter2Path).Save(data);
            string original = File.ReadAllText(chapter2Path);

            AssertScene(GameSessionFlow.Chapter2ScenePath);
            Assert.That(File.ReadAllText(chapter2Path), Is.EqualTo(original));
            Assert.That(new JsonChapter2SaveService(chapter2Path).Load().Mission04ComputerUnlocked,
                Is.True);
        }

        [Test]
        public void Chapter2CanResumeWithoutAChapter1File()
        {
            new JsonChapter2SaveService(chapter2Path).Save(Chapter2SaveData.CreateDefault());

            AssertScene(GameSessionFlow.Chapter2ScenePath);
            Assert.That(File.Exists(chapter1Path), Is.False);
        }

        [Test]
        public void CompletedChapter2ResumesChapter3WithCarryOverSaveUntouched()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();
            data.Chapter2Completed = true;
            data.ChapterEndInventory.Add(new Chapter2InventoryEntry
                { ItemId = "crow_bar", Quantity = 1 });
            new JsonChapter2SaveService(chapter2Path).Save(data);
            string original = File.ReadAllText(chapter2Path);

            AssertScene(GameSessionFlow.Chapter3ScenePath);
            Assert.That(File.ReadAllText(chapter2Path), Is.EqualTo(original));
            Assert.That(new JsonChapter2SaveService(chapter2Path).Load().ChapterEndInventory[0].ItemId,
                Is.EqualTo("crow_bar"));
        }

        [Test]
        public void FinalChapter2ConversationStepNormalizesToChapter3RouteWithoutRewritingSave()
        {
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();
            data.Mission05MinhConversationStep = Chapter2SaveData.EndingConversationFinalStep;
            new JsonChapter2SaveService(chapter2Path).Save(data);
            string original = File.ReadAllText(chapter2Path);

            AssertScene(GameSessionFlow.Chapter3ScenePath);
            Assert.That(File.ReadAllText(chapter2Path), Is.EqualTo(original));
        }

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{\"SaveVersion\":0}")]
        [TestCase("{\"SaveVersion\":2}")]
        public void InvalidChapter2FallsBackToValidChapter1WithoutTouchingInvalidFile(string json)
        {
            new JsonChapter1SaveService(chapter1Path).Save(Chapter1SaveData.CreateDefault());
            File.WriteAllText(chapter2Path, json);

            AssertScene(GameSessionFlow.Chapter1ScenePath);
            Assert.That(File.ReadAllText(chapter2Path), Is.EqualTo(json));
        }

        [Test]
        public void GameCompletionMarkerDisablesContinueWithoutErasingAnyData()
        {
            new JsonChapter1SaveService(chapter1Path).Save(Chapter1SaveData.CreateDefault());
            Chapter2SaveData data = Chapter2SaveData.CreateDefault();
            data.Chapter2Completed = true;
            new JsonChapter2SaveService(chapter2Path).Save(data);
            completed = true;

            Assert.That(store.TryGetContinueScene(out string scene), Is.False);
            Assert.That(scene, Is.Null);
            Assert.That(File.Exists(chapter1Path), Is.True);
            Assert.That(File.Exists(chapter2Path), Is.True);
            Assert.That(completed, Is.True);
            Assert.That(completionClearCount, Is.Zero);
        }

        [Test]
        public void ResetDeletesOnlyGameSavesAndClearsInjectedCompletionMarker()
        {
            new JsonChapter1SaveService(chapter1Path).Save(Chapter1SaveData.CreateDefault());
            new JsonChapter2SaveService(chapter2Path).Save(Chapter2SaveData.CreateDefault());
            string unrelatedPath = Path.Combine(temporaryDirectory, "unrelated-settings.json");
            const string unrelatedContents = "{\"volume\":0.25}";
            File.WriteAllText(unrelatedPath, unrelatedContents);
            completed = true;

            Assert.That(store.TryResetProgress(out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(File.Exists(chapter1Path), Is.False);
            Assert.That(File.Exists(chapter2Path), Is.False);
            Assert.That(File.ReadAllText(unrelatedPath), Is.EqualTo(unrelatedContents));
            Assert.That(completed, Is.False);
            Assert.That(completionClearCount, Is.EqualTo(1));
            Assert.That(store.TryGetContinueScene(out _), Is.False);
        }

        [Test]
        public void ResetCanBeRepeatedWhenSaveFilesAreAlreadyAbsent()
        {
            completed = true;

            Assert.That(store.TryResetProgress(out string firstError), Is.True);
            Assert.That(store.TryResetProgress(out string secondError), Is.True);
            Assert.That(firstError, Is.Null);
            Assert.That(secondError, Is.Null);
            Assert.That(completed, Is.False);
            Assert.That(completionClearCount, Is.EqualTo(2));
            Assert.That(Directory.GetFiles(temporaryDirectory), Is.Empty);
        }

        private void AssertScene(string expected)
        {
            Assert.That(store.TryGetContinueScene(out string actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
