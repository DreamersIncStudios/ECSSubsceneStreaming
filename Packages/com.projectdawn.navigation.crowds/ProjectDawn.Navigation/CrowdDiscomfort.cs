using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    public enum CrowdDiscomfortType
    {
        Quad,
        Circle,
    }

    /// <summary>
    /// Discomfort volume of crowd agents.
    /// </summary>
    public struct CrowdDiscomfort : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// The shape of the discomfort.
        /// </summary>
        public CrowdDiscomfortType Type;
        /// <summary>
        /// The size of the discomfort.
        /// </summary>
        public float3 Size;
        /// <summary>
        /// The gradient of discomfort. The x-component represents the discomfort value at the center of the shape, and the y-component represents it at the edge
        /// </summary>
        public float2 Gradient;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static CrowdDiscomfort Default => new()
        {
            Type = CrowdDiscomfortType.Quad,
            Size = new(1, 1, 1),
            Gradient = new(1, 0),
        };
    }

    public struct CrowdDiscomfortSplat : ICleanupComponentData, IEquatable<CrowdDiscomfortSplat>
    {
        public float3 Position;
        public CrowdDiscomfortType Type;
        public float3 Size;
        public float2 Gradient;

        public bool Equals(CrowdDiscomfortSplat other)
        {
            return math.all(Position == other.Position) &&
                Type == other.Type &&
                math.all(Size == other.Size) &&
                math.all(Gradient == other.Gradient);
        }
        public static bool operator ==(in CrowdDiscomfortSplat lhs, in CrowdDiscomfortSplat rhs) => lhs.Equals(rhs);
        public static bool operator !=(in CrowdDiscomfortSplat lhs, in CrowdDiscomfortSplat rhs) => !lhs.Equals(rhs);
        public override bool Equals(object other) => other is CrowdDiscomfortSplat splat && Equals(splat);
        public override int GetHashCode() => (int) math.hash(Position);
    }
}
