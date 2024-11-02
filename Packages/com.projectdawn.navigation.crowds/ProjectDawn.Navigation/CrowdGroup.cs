using ProjectDawn.ContinuumCrowds;
using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Source from which crowd will get goal.
    /// </summary>
    public enum CrowdGoalSource
    {
        /// <summary>
        /// Manually set via scripting.
        /// </summary>
        [Tooltip("Manually set via scripting.")]
        Script,
        /// <summary>
        /// Automatically gathered from crowd agents <see cref="AgentBody.Destination"/>.
        /// </summary>
        [Tooltip("Automatically gathered from crowd AgentBody.Destination.")]
        AgentDestination,
    }

    /// <summary>
    /// Crowd group settings.
    /// </summary>
    public struct CrowdGroup : IComponentData
    {
        /// <summary>
        /// Entity that contains <see cref="CrowdSurface"/> and <see cref="CrowdSurfaceWorld"/>.
        /// </summary>
        public Entity Surface;
        /// <summary>
        /// Crowd group speed settings.
        /// </summary>
        public Speed Speed;
        /// <summary>
        /// Controls the cost weights of constructing crowd flow fields.
        /// </summary>
        public CostWeights CostWeights;
        /// <summary>
        /// Source from which crowd will get goal.
        /// </summary>
        public CrowdGoalSource GoalSource;
        /// <summary>
        /// Maximum distance that will be used when attempting to map the agent's position or destination onto surface. The higher the value, the bigger the performance cost.
        /// </summary>
        public bool Grounded;
        /// <summary>
        /// Maximum distance that will be used when attempting to map the agent's position or destination onto surface. The higher the value, the bigger the performance cost.
        /// </summary>
        public float MappingRadius;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static CrowdGroup Default => new()
        {
            Surface = Entity.Null,
            Speed = Speed.Default,
            CostWeights = CostWeights.Default,
            GoalSource = CrowdGoalSource.AgentDestination,
            Grounded = true,
            MappingRadius = 5.0f,
        };
    }

    /// <summary>
    /// Crowd group runtime created <see cref="CrowdFlow"/>.
    /// </summary>
    public struct CrowdGroupFlow : ICleanupComponentData
    {
        public CrowdFlow Flow;
    }
}
