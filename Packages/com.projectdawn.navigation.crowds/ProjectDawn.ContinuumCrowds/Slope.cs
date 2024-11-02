using System;
using UnityEngine;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// Crowd group slope settings.
    /// </summary>
    [Serializable]
    public struct Slope
    {
        /// <summary>
        /// Slope minimum tangent. At this slope crowd will move maximum speed.
        /// </summary>
        [Tooltip("Slope minimum tangent. At this slope crowd will move maximum speed.")]
        public float Min;

        /// <summary>
        /// Slope maximum tangent. At this slope crowd will move minimum speed.
        /// </summary>
        [Tooltip("Slope maximum tangent. At this slope crowd will move minimum speed.")]
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
        public static Slope Default => new(0.0f, 1.0f);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="min">Slope minimum tangent.</param>
        /// <param name="max">Slope maximum tangent.</param>
        /// <exception cref="ArgumentException"></exception>
        public Slope(float min, float max)
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
