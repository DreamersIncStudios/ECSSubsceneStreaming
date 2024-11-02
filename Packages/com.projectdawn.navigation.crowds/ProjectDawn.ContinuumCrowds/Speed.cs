using System;
using UnityEngine;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// Crowd group speed settings.
    /// </summary>
    [Serializable]
    public struct Speed
    {
        /// <summary>
        /// Minimum speed of crowd.
        /// </summary>
        [Tooltip("Minimum speed of crowd.")]
        public float Min;

        /// <summary>
        /// Maximum speed of crowd.
        /// </summary>
        [Tooltip("Maximum speed of crowd.")]
        public float Max;

        /// <summary>
        /// Difference between max and min.
        /// </summary>
        public float Length => Max - Min;

        /// <summary>
        /// Inverse difference between max and min.
        /// </summary>
        public float InvLength => 1.0f / Length;

        /// <summary>
        /// Returns the default configuration.
        /// </summary>
        public static Speed Default => new(0.1f, 3.5f);

        public static Speed operator -(Speed value) => new Speed { Min = -value.Min, Max = -value.Max };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="min">Minimum speed of crowd.</param>
        /// <param name="max">Maximum speed of crowd.</param>
        /// <exception cref="ArgumentException"></exception>
        public Speed(float min, float max)
        {
            if (min > max)
                throw new ArgumentException("Min cannot be bigger than max.");
            if (max - min <= 0)
                throw new ArgumentException("Max and min difference must be greater than zero.");

            Min = min;
            Max = max;
        }
    }
}
