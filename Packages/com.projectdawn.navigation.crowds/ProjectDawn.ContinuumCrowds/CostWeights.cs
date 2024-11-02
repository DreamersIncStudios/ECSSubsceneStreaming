using System;
using UnityEngine;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// Controls the cost weights of constructing crowd flow fields.
    /// </summary>
    [Serializable]
    public struct CostWeights
    {
        /// <summary>
        /// Distance weight. The higher the value, the more crowd will prioritize shorter paths.
        /// </summary>
        [Tooltip("Distance weight. The higher the value, the more crowd will prioritize shorter paths.")]
        public float Distance;

        /// <summary>
        /// Time weight. The higher the value, the more crowd will prioritize faster paths that are less crowded.
        /// </summary>
        [Tooltip("Time weight. The higher the value, the more crowd will prioritize faster paths that are less crowded.")]
        public float Time;

        /// <summary>
        /// Discomfort weight. The higher the value, the more crowd will prioritize a path that contains the least discomfort.
        /// </summary>
        [Tooltip("Discomfort weight. The higher the value, the more crowd will prioritize a path that contains the least discomfort.")]
        public float Discomfort;

        /// <summary>
        /// If true, weights will be normalized to the sum of 1.
        /// </summary>
        [Tooltip("If true, weights will be normalized to the sum of 1.")]
        public bool Normalize;

        /// <summary>
        /// Returns the default configuration.
        /// </summary>
        public static CostWeights Default => new CostWeights(0.2f, 0.8f, 0.0f);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="distance">Distance weight. The higher the value, the more crowd will prioritize shorter paths.</param>
        /// <param name="time">Time weight. The higher the value, the more crowd will prioritize faster paths that are less crowded.</param>
        /// <param name="discomfort">Discomfort weight. The higher the value, the more crowd will prioritize a path that contains the least discomfort.</param>
        public CostWeights(float distance, float time, float discomfort)
        {
            Distance = distance;
            Time = time;
            Discomfort = discomfort;
            Normalize = true;
        }
    }
}
