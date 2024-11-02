using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

namespace ProjectDawn.Navigation
{
    public enum NavMeshLinkTraversalMode
    {
        None,
        Seeking,
        Custom,
    }

    /// <summary>
    /// Agent off mesh link data that is currently traversing.
    /// </summary>
    public struct NavMeshLinkTraversal : IComponentData
    {
        public PolygonId StartPolygon;
        public PolygonId EndPolygon;
        public LinkTraversalSeek Seek;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static NavMeshLinkTraversal Default => new()
        {
        };
    }
}
