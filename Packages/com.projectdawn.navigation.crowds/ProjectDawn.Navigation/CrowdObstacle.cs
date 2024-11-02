using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    public enum CrowdObstacleType
    {
        Quad,
        Circle,
    }

    /// <summary>
    /// Obstacle volume of crowd agents.
    /// </summary>
    public struct CrowdObstacle : IComponentData
    {
        /// <summary>
        /// The shape of the obstacle.
        /// </summary>
        public CrowdObstacleType Type;
        /// <summary>
        /// The size of the obstacle.
        /// </summary>
        public float3 Size;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static CrowdObstacle Default => new()
        {
            Type = CrowdObstacleType.Quad,
            Size = new float3(1, 1, 1),
        };
    }

    public struct CrowdObstacleSplat : ICleanupComponentData, IEquatable<CrowdObstacleSplat>
    {
        public float3 Position;
        public CrowdObstacleType Type;
        public float3 Size;

        public bool Equals(CrowdObstacleSplat other)
        {
            return math.all(Position == other.Position) &&
                Type == other.Type &&
                math.all(Size == other.Size);
        }
        public static bool operator ==(in CrowdObstacleSplat lhs, in CrowdObstacleSplat rhs) => lhs.Equals(rhs);
        public static bool operator!=(in CrowdObstacleSplat lhs, in CrowdObstacleSplat rhs) => !lhs.Equals(rhs);
        public override bool Equals(object other) => other is CrowdObstacleSplat splat && Equals(splat);
        public override int GetHashCode() => (int)math.hash(Position);
    }
}
