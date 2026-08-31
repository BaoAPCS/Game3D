using System;
using System.Collections.Generic;

namespace DormitoryMystery.Chapter2
{
    [Flags]
    public enum Chapter2CircuitDirection : byte
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3
    }

    public enum Chapter2CircuitOutput
    {
        SecurityRelay = 0,
        Control = 1,
        DoorLock = 2
    }

    /// <summary>
    /// Pure gameplay model for Mission 02's fixed 5x5 rotation puzzle.
    /// A connection carries power only when both neighbouring tiles expose
    /// matching sides. The three right-edge outputs must all be energized.
    /// </summary>
    public sealed class Chapter2CircuitPuzzle
    {
        public const int Width = 5;
        public const int Height = 5;
        public const int TileCount = Width * Height;
        public const int OutputCount = 3;
        public const int SourceRow = 0;

        private static readonly int[] OutputRows = { 0, 2, 4 };

        // The canonical circuit contains two real branches and a winding
        // shared trunk. Branch A at (2, 0) splits Relay from the lower trunk;
        // Branch B at (3, 3) splits Control from Door Lock. Seventeen tiles are
        // required to power all outputs, while eight disconnected tiles are
        // visual decoys.
        private static readonly Chapter2CircuitDirection[]
            CanonicalConnections =
            {
                // Row 0: source -> Branch A -> Relay.
                EW, EW, EWS, EW, EW,
                // Row 1: only x=2 continues the lower trunk.
                NE, SW, NS, ES, NW,
                // Row 2: trunk turns left/down; Control ends on the right.
                ES, ES, NW, ES, EW,
                // Row 3: trunk returns right into Branch B.
                NE, NS, ES, WNS, NS,
                // Row 4: bottom detour and Door Lock branch.
                ES, NE, NW, NE, EW
            };

        private static readonly bool[] RequiredCircuitTiles =
        {
            true,  true,  true, true,  true,
            false, false, true, false, false,
            false, true,  true, true,  true,
            false, true,  true, true,  false,
            false, true,  true, true,  true
        };

        // Fixed instead of random so a save/retry never changes under the
        // player. Every required circuit tile starts misoriented; restoring
        // those seventeen tiles takes 18 clockwise quarter-turn presses. Decoy
        // rotations never contribute to the completion requirement.
        private static readonly byte[] InitialRotations =
        {
            1, 3, 3, 1, 3,
            3, 3, 1, 3, 3,
            3, 2, 3, 3, 3,
            2, 3, 3, 3, 3,
            2, 3, 3, 3, 1
        };

        private readonly byte[] rotations = new byte[TileCount];
        private readonly bool[] powered = new bool[TileCount];
        private readonly Queue<int> traversalQueue = new Queue<int>(TileCount);
        private bool powerStateDirty = true;

        public Chapter2CircuitPuzzle()
        {
            Reset();
        }

        public int Revision { get; private set; }

        public bool IsSolved
        {
            get
            {
                EnsurePowerState();
                for (int i = 0; i < OutputCount; i++)
                {
                    if (!IsOutputPoweredWithoutRefresh(i))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Reset()
        {
            Array.Copy(InitialRotations, rotations, TileCount);
            MarkChanged();
        }

        public void RotateClockwise(int x, int y)
        {
            int index = GetIndex(x, y);
            rotations[index] = (byte)((rotations[index] + 1) & 3);
            MarkChanged();
        }

        public Chapter2CircuitDirection GetConnections(int x, int y)
        {
            int index = GetIndex(x, y);
            return RotateDirectionsClockwise(
                CanonicalConnections[index],
                rotations[index]);
        }

        public Chapter2CircuitDirection GetCanonicalConnections(
            int x,
            int y)
        {
            return CanonicalConnections[GetIndex(x, y)];
        }

        public int GetRotation(int x, int y)
        {
            return rotations[GetIndex(x, y)];
        }

        public int GetInitialRotation(int x, int y)
        {
            return InitialRotations[GetIndex(x, y)];
        }

        public bool IsRequiredTile(int x, int y)
        {
            return RequiredCircuitTiles[GetIndex(x, y)];
        }

        public bool IsTilePowered(int x, int y)
        {
            EnsurePowerState();
            return powered[GetIndex(x, y)];
        }

        public bool IsOutputPowered(Chapter2CircuitOutput output)
        {
            int outputIndex = (int)output;
            ValidateOutputIndex(outputIndex);
            EnsurePowerState();
            return IsOutputPoweredWithoutRefresh(outputIndex);
        }

        public int GetOutputRow(Chapter2CircuitOutput output)
        {
            int outputIndex = (int)output;
            ValidateOutputIndex(outputIndex);
            return OutputRows[outputIndex];
        }

        public static Chapter2CircuitDirection RotateDirectionsClockwise(
            Chapter2CircuitDirection directions,
            int quarterTurns = 1)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            int bits = (int)directions & 0x0F;
            for (int i = 0; i < turns; i++)
            {
                bits = ((bits << 1) & 0x0F) | ((bits >> 3) & 0x01);
            }

            return (Chapter2CircuitDirection)bits;
        }

        private void EnsurePowerState()
        {
            if (!powerStateDirty)
            {
                return;
            }

            Array.Clear(powered, 0, powered.Length);
            traversalQueue.Clear();

            int sourceIndex = GetIndex(0, SourceRow);
            if ((GetConnections(0, SourceRow) &
                 Chapter2CircuitDirection.West) == 0)
            {
                powerStateDirty = false;
                return;
            }

            powered[sourceIndex] = true;
            traversalQueue.Enqueue(sourceIndex);

            while (traversalQueue.Count > 0)
            {
                int index = traversalQueue.Dequeue();
                int x = index % Width;
                int y = index / Width;
                Chapter2CircuitDirection current = GetConnections(x, y);

                TryEnergizeNeighbour(
                    x, y, x, y - 1,
                    current,
                    Chapter2CircuitDirection.North,
                    Chapter2CircuitDirection.South);
                TryEnergizeNeighbour(
                    x, y, x + 1, y,
                    current,
                    Chapter2CircuitDirection.East,
                    Chapter2CircuitDirection.West);
                TryEnergizeNeighbour(
                    x, y, x, y + 1,
                    current,
                    Chapter2CircuitDirection.South,
                    Chapter2CircuitDirection.North);
                TryEnergizeNeighbour(
                    x, y, x - 1, y,
                    current,
                    Chapter2CircuitDirection.West,
                    Chapter2CircuitDirection.East);
            }

            powerStateDirty = false;
        }

        private void TryEnergizeNeighbour(
            int currentX,
            int currentY,
            int neighbourX,
            int neighbourY,
            Chapter2CircuitDirection currentConnections,
            Chapter2CircuitDirection outwardDirection,
            Chapter2CircuitDirection requiredNeighbourDirection)
        {
            if ((currentConnections & outwardDirection) == 0 ||
                neighbourX < 0 || neighbourX >= Width ||
                neighbourY < 0 || neighbourY >= Height)
            {
                return;
            }

            int neighbourIndex = GetIndex(neighbourX, neighbourY);
            if (powered[neighbourIndex] ||
                (GetConnections(neighbourX, neighbourY) &
                 requiredNeighbourDirection) == 0)
            {
                return;
            }

            powered[neighbourIndex] = true;
            traversalQueue.Enqueue(neighbourIndex);
        }

        private bool IsOutputPoweredWithoutRefresh(int outputIndex)
        {
            int row = OutputRows[outputIndex];
            int tileIndex = GetIndex(Width - 1, row);
            return powered[tileIndex] &&
                   (GetConnections(Width - 1, row) &
                    Chapter2CircuitDirection.East) != 0;
        }

        private void MarkChanged()
        {
            powerStateDirty = true;
            Revision++;
        }

        private static int GetIndex(int x, int y)
        {
            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            return y * Width + x;
        }

        private static void ValidateOutputIndex(int outputIndex)
        {
            if (outputIndex < 0 || outputIndex >= OutputCount)
            {
                throw new ArgumentOutOfRangeException(nameof(outputIndex));
            }
        }

        private const Chapter2CircuitDirection EW =
            Chapter2CircuitDirection.East |
            Chapter2CircuitDirection.West;
        private const Chapter2CircuitDirection ES =
            Chapter2CircuitDirection.East |
            Chapter2CircuitDirection.South;
        private const Chapter2CircuitDirection NE =
            Chapter2CircuitDirection.North |
            Chapter2CircuitDirection.East;
        private const Chapter2CircuitDirection NW =
            Chapter2CircuitDirection.North |
            Chapter2CircuitDirection.West;
        private const Chapter2CircuitDirection NS =
            Chapter2CircuitDirection.North |
            Chapter2CircuitDirection.South;
        private const Chapter2CircuitDirection SW =
            Chapter2CircuitDirection.South |
            Chapter2CircuitDirection.West;
        private const Chapter2CircuitDirection EWS =
            Chapter2CircuitDirection.East |
            Chapter2CircuitDirection.West |
            Chapter2CircuitDirection.South;
        private const Chapter2CircuitDirection WNS =
            Chapter2CircuitDirection.West |
            Chapter2CircuitDirection.North |
            Chapter2CircuitDirection.South;
    }
}
