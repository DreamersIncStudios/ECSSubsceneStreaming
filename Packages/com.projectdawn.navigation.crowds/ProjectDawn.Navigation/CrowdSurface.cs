using ProjectDawn.ContinuumCrowds;
using Unity.Entities;
using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Crowd surface settings.
    /// </summary>
    public struct CrowdSurface : IComponentData
    {
        /// <summary>
        /// World space size.
        /// </summary>
        public float2 Size;

        /// <summary>
        /// Cell count horizontally.
        /// </summary>
        public int Width;

        /// <summary>
        /// Cell count vertically.
        /// </summary>
        public int Height;

        /// <summary>
        /// Crowd group density settings.
        /// </summary>
        public Density Density;

        /// <summary>
        /// Crowd group slope settings.
        /// </summary>
        public Slope Slope;

        public NavigationLayers Layers;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static CrowdSurface Default => new()
        {
            Size = new float2(1, 1),
            Width = 16,
            Height = 16,
            Density = Density.Default,
            Slope = Slope.Default,
            Layers = NavigationLayers.Everything,
        };
    }

    /// <summary>
    /// Crowd surface <see cref="CrowdData"/>.
    /// </summary>
    public struct CrowdSurfaceData : ISharedComponentData, System.IEquatable<CrowdSurfaceData>
    {
        public CrowdData Data;

        public bool Equals(CrowdSurfaceData other) => Data == other.Data;

        public override int GetHashCode()
        {
            if (Data == null)
                return 0;
            return Data.GetHashCode();
        }
    }

    /// <summary>
    /// Crowd surface runtime created <see cref="CrowdWorld"/>.
    /// </summary>
    public struct CrowdSurfaceWorld : ICleanupComponentData
    {
        public CrowdWorld World;
    }
}
