using System;
using UnityEngine;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// Crowd group density settings.
    /// </summary>
    [Serializable]
    public struct Density
    {
        /// <summary>
        /// Minimum density of single crowd cell that will effect crowd pathing. The density value at which crowd will not share average speed in the cell.
        /// </summary>
        [Tooltip("Minimum density of single crowd cell that will effect crowd pathing. The density value at which crowd will not share average speed in the cell.")]
        public float Min;
        /// <summary>
        /// Maximum density of single crowd cell that will effect crowd pathing. The density value at which crowd will fully share average speed in the cell.
        /// </summary>
        [Tooltip("Maximum density of single crowd cell that will effect crowd pathing. The density value at which crowd will fully share average speed in the cell.")]
        public float Max;
        /// <summary>
        /// Controls interpolation falloff of density then splattering in world. The one would represent linear falloff and lower values exponential curve.
        /// </summary>
        [Tooltip("Controls interpolation falloff of density then splattering in world. The one would represent linear falloff and lower values exponential curve.")]
        [Range(0, 1)]
        public float Exponent;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static Density Default => new(0.32f, 1.6f, 0.3f);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="min">Minimum density of single crowd cell that will effect crowd pathing. The density value at which crowd will not share average speed in the cell.</param>
        /// <param name="max">Maximum density of single crowd cell that will effect crowd pathing. The density value at which crowd will fully share average speed in the cell.</param>
        /// <param name="exponent">Controls interpolation falloff of density then splattering in world. The one would represent linear falloff and lower values exponential curve.</param>
        /// <exception cref="ArgumentException"></exception>
        public Density(float min, float max, float exponent)
        {
            if (min > max)
                throw new ArgumentException("Min cannot be bigger than max.");
            if (max - min <= 0)
                throw new ArgumentException("Max and min difference must be greater than zero.");

            Min = min;
            Max = max;
            Exponent = exponent;
        }
    }
}
