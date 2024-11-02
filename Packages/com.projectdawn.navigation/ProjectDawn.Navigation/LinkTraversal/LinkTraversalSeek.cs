using Unity.Entities;
using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    public struct Portal
    {
        public float3 Left;
        public float3 Right;

        public Portal(float3 point)
        {
            Left = point;
            Right = point;
        }

        public Portal(float3 left, float3 right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Returns closest point from position to portal line.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float3 GetClosestPortalPoint(float3 position) => GeometryUtils.ClosestPointOnSegment(Left, Right, position);
    }

    public struct LinkTraversalSeek : IComponentData
    {
        public Portal Start;
        public Portal End;
    }
}
