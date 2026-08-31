using NUnit.Framework;

namespace DormitoryMystery.Chapter2.Tests
{
    public sealed class Chapter2CircuitPuzzleEditModeTests
    {
        [Test]
        public void InitialBoardIsDeterministicAndUnsolved()
        {
            Chapter2CircuitPuzzle first = new Chapter2CircuitPuzzle();
            Chapter2CircuitPuzzle second = new Chapter2CircuitPuzzle();

            Assert.IsFalse(first.IsSolved);
            Assert.IsFalse(first.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsFalse(first.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsFalse(first.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));

            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    Assert.AreEqual(
                        first.GetConnections(x, y),
                        second.GetConnections(x, y));
                }
            }
        }

        [Test]
        public void CanonicalSolutionPowersAllThreeOutputs()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            RotateEveryTileToCanonicalSolution(puzzle);

            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));
            Assert.IsTrue(puzzle.IsSolved);

            int requiredCount = 0;
            int poweredCount = 0;
            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    if (puzzle.IsRequiredTile(x, y))
                    {
                        requiredCount++;
                        Assert.IsTrue(
                            puzzle.IsTilePowered(x, y),
                            $"Required tile ({x}, {y}) was not powered.");
                    }
                    else
                    {
                        Assert.IsFalse(
                            puzzle.IsTilePowered(x, y),
                            $"Decoy tile ({x}, {y}) joined the canonical circuit.");
                    }

                    if (puzzle.IsTilePowered(x, y))
                    {
                        poweredCount++;
                    }
                }
            }

            Assert.AreEqual(17, requiredCount);
            Assert.AreEqual(17, poweredCount);
        }

        [Test]
        public void ScrambleHasReasonableFixedDifficulty()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            int requiredTiles = 0;
            int misorientedRequiredTiles = 0;
            int requiredClockwisePresses = 0;

            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    bool required = puzzle.IsRequiredTile(x, y);
                    if (required)
                    {
                        requiredTiles++;
                    }

                    if (required &&
                        puzzle.GetConnections(x, y) !=
                        puzzle.GetCanonicalConnections(x, y))
                    {
                        misorientedRequiredTiles++;
                    }

                    int pressesForTile = 0;
                    while (puzzle.GetConnections(x, y) !=
                           puzzle.GetCanonicalConnections(x, y))
                    {
                        puzzle.RotateClockwise(x, y);
                        pressesForTile++;
                        Assert.Less(pressesForTile, 4);
                    }

                    if (required)
                    {
                        requiredClockwisePresses += pressesForTile;
                    }
                }
            }

            Assert.AreEqual(17, requiredTiles);
            Assert.AreEqual(17, misorientedRequiredTiles);
            Assert.AreEqual(18, requiredClockwisePresses);
            Assert.IsTrue(puzzle.IsSolved);
        }

        [Test]
        public void FourQuarterTurnsRestoreAnyTile()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            Chapter2CircuitDirection before = puzzle.GetConnections(4, 2);

            for (int i = 0; i < 4; i++)
            {
                puzzle.RotateClockwise(4, 2);
            }

            Assert.AreEqual(before, puzzle.GetConnections(4, 2));
        }

        [Test]
        public void BreakingMiddleRouteCutsLaterOutputsOnly()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            RotateEveryTileToCanonicalSolution(puzzle);

            // Row 1 lies after the Relay split but before Control and Door Lock
            // on their shared winding trunk. Turning it breaks both later paths.
            puzzle.RotateClockwise(2, 1);

            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsFalse(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsFalse(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));
            Assert.IsFalse(puzzle.IsSolved);
        }

        [Test]
        public void ThreeOutputBranchesFailIndependently()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            RotateEveryTileToCanonicalSolution(puzzle);

            // Relay branch after the first T-junction.
            puzzle.RotateClockwise(3, 0);
            Assert.IsFalse(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));

            RotateEveryTileToCanonicalSolution(puzzle);

            // Control branch above the second T-junction.
            puzzle.RotateClockwise(3, 2);
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsFalse(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));

            RotateEveryTileToCanonicalSolution(puzzle);

            // Door Lock branch below the second T-junction.
            puzzle.RotateClockwise(3, 4);
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.SecurityRelay));
            Assert.IsTrue(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.Control));
            Assert.IsFalse(puzzle.IsOutputPowered(
                Chapter2CircuitOutput.DoorLock));
        }

        [Test]
        public void ResetRestoresOriginalScramble()
        {
            Chapter2CircuitPuzzle puzzle = new Chapter2CircuitPuzzle();
            Chapter2CircuitDirection[] initial =
                new Chapter2CircuitDirection[
                    Chapter2CircuitPuzzle.TileCount];

            int index = 0;
            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    initial[index++] = puzzle.GetConnections(x, y);
                    puzzle.RotateClockwise(x, y);
                }
            }

            puzzle.Reset();

            index = 0;
            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    Assert.AreEqual(
                        initial[index++],
                        puzzle.GetConnections(x, y));
                }
            }
        }

        private static void RotateEveryTileToCanonicalSolution(
            Chapter2CircuitPuzzle puzzle)
        {
            for (int y = 0; y < Chapter2CircuitPuzzle.Height; y++)
            {
                for (int x = 0; x < Chapter2CircuitPuzzle.Width; x++)
                {
                    int safety = 0;
                    while (puzzle.GetConnections(x, y) !=
                           puzzle.GetCanonicalConnections(x, y))
                    {
                        puzzle.RotateClockwise(x, y);
                        safety++;
                        Assert.Less(safety, 4);
                    }
                }
            }
        }
    }
}
