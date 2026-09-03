using UnityEngine;

namespace DormitoryMystery.Chapter2
{
    /// <summary>
    /// Pure signal-strength model used by Mission 05. The model deliberately
    /// ignores height and occlusion: the puzzle is about narrowing down an
    /// area, not guessing how arbitrary level collision attenuates Wi-Fi.
    /// </summary>
    public sealed class Chapter2WifiSignalModel
    {
        public const int MinimumBars = 1;
        public const int MaximumBars = 5;
        public const float TwoBarDistance = 14f;
        public const float ThreeBarDistance = 9f;
        public const float FourBarDistance = 5.5f;
        public const float FiveBarDistance = 2.5f;
        public const float DefaultSmoothingSeconds = 0.35f;
        public const float DefaultHysteresisMetres = 0.4f;

        private readonly float smoothingSeconds;
        private readonly float hysteresisMetres;
        private bool initialized;
        private float smoothedDistance;
        private int currentBars = MinimumBars;

        public Chapter2WifiSignalModel(
            float smoothing = DefaultSmoothingSeconds,
            float hysteresis = DefaultHysteresisMetres)
        {
            smoothingSeconds = Mathf.Max(0f, smoothing);
            hysteresisMetres = Mathf.Max(0f, hysteresis);
        }

        public bool IsInitialized => initialized;
        public float SmoothedDistance => smoothedDistance;
        public int CurrentBars => currentBars;

        public int Reset(float horizontalDistance)
        {
            smoothedDistance = SanitizeDistance(horizontalDistance);
            currentBars = GetRawBars(smoothedDistance);
            initialized = true;
            return currentBars;
        }

        public int Update(float horizontalDistance, float deltaTime)
        {
            float safeDistance = SanitizeDistance(horizontalDistance);
            if (!initialized)
            {
                return Reset(safeDistance);
            }

            if (smoothingSeconds <= 0f || deltaTime <= 0f)
            {
                smoothedDistance = safeDistance;
            }
            else
            {
                float blend = 1f - Mathf.Exp(
                    -Mathf.Max(0f, deltaTime) / smoothingSeconds);
                smoothedDistance = Mathf.Lerp(
                    smoothedDistance,
                    safeDistance,
                    blend);
            }

            int rawBars = GetRawBars(smoothedDistance);
            if (rawBars > currentBars)
            {
                // Moving towards the router must cross inside the stronger
                // band's threshold before the display is promoted.
                currentBars = Mathf.Max(
                    currentBars,
                    GetRawBars(smoothedDistance + hysteresisMetres));
            }
            else if (rawBars < currentBars)
            {
                // Moving away must cross outside the weaker band's threshold
                // before the display is demoted.
                currentBars = Mathf.Min(
                    currentBars,
                    GetRawBars(Mathf.Max(
                        0f,
                        smoothedDistance - hysteresisMetres)));
            }

            currentBars = Mathf.Clamp(
                currentBars,
                MinimumBars,
                MaximumBars);
            return currentBars;
        }

        public static int GetRawBars(float horizontalDistance)
        {
            float distance = SanitizeDistance(horizontalDistance);
            if (distance <= FiveBarDistance)
            {
                return 5;
            }

            if (distance <= FourBarDistance)
            {
                return 4;
            }

            if (distance <= ThreeBarDistance)
            {
                return 3;
            }

            if (distance <= TwoBarDistance)
            {
                return 2;
            }

            return 1;
        }

        public static float HorizontalDistance(
            Vector3 first,
            Vector3 second)
        {
            float deltaX = first.x - second.x;
            float deltaZ = first.z - second.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static float SanitizeDistance(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance))
            {
                return float.MaxValue;
            }

            return Mathf.Max(0f, distance);
        }
    }
}
