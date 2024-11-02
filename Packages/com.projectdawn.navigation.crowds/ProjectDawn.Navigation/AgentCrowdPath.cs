using System;
using Unity.Entities;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Agent Crowd pathing.
    /// </summary>
    [ChunkSerializable]
    public struct AgentCrowdPath : ISharedComponentData, IEquatable<AgentCrowdPath>
    {
        /// <summary>
        /// Entity that contains <see cref="CrowdGroup"/> and <see cref="CrowdGroupFlow"/>.
        /// </summary>
        public Entity Group;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static AgentCrowdPath Default => new()
        {
            Group = Entity.Null,
        };

        public bool Equals(AgentCrowdPath other) => Group == other.Group;
        public override int GetHashCode() => Group.GetHashCode();
    }
}
