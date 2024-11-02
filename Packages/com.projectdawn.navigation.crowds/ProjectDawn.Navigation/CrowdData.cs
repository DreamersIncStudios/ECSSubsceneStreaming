using ProjectDawn.ContinuumCrowds;
using System;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Crowd data used for initializing topographical surface data.
    /// </summary>
    [CreateAssetMenu(fileName = "New Crowd Data", menuName = "AI/Crowd Data", order = 1000)]
    public class CrowdData : ScriptableObject
    {
        [Tooltip("Maximum slope in degress.")]
        [SerializeField, Range(0, 90)]
        float m_MaxSlope = 45.0f;

        [Tooltip("Maximum height of cell.")]
        [SerializeField]
        float m_MaxHeight = 10.0f;

        [Tooltip("Radius of agent.")]
        [SerializeField]
        float m_Radius = 0.5f;

        [Tooltip("Settings that define objects that will be collected for crowd surface data baking.")]
        [SerializeField]
        ObjectCollection m_Collection = ObjectCollection.Default;

        [Tooltip("Cell color then drawing gizmos.")]
        [SerializeField]
        Color m_GizmosColor = new(0f, 0.75f, 1f);

        [Tooltip("Baked data.")]
        [SerializeField]
        Baked m_Baked;

        /// <summary>
        /// Defines the collected objects geometry.
        /// </summary>
        public enum ObjectCollectionGeometry
        {
            [Tooltip("Unity physics colliders will be collected.")]
            PhysicsColliders,
        }

        /// <summary>
        /// Settings that define objects that will be collected for crowd surface data baking.
        /// </summary>
        [Serializable]
        public struct ObjectCollection
        {
            /// <summary>
            /// Defines the collected objects geometry.
            /// </summary>
            [Tooltip("Defines the collected objects geometry.")]
            public ObjectCollectionGeometry Geometry;

            /// <summary>
            /// Objects with specified layer will be included.
            /// </summary>
            [Tooltip("Objects with specified layer will be included.")]
            public LayerMask Layers;

            public static ObjectCollection Default => new()
            {
                Geometry = ObjectCollectionGeometry.PhysicsColliders,
                Layers = ~0,
            };
        }

        [Serializable]
        public struct Baked
        {
            /// <summary>
            /// Baked data width.
            /// </summary>
            [Tooltip("Baked data width.")]
            [SerializeField]
            public int Width;

            /// <summary>
            /// Baked data height.
            /// </summary>
            [Tooltip("Baked data height.")]
            [SerializeField]
            public int Height;

            /// <summary>
            /// Baked height field.
            /// </summary>
            [Tooltip("Baked height field.")]
            [SerializeField]
            public float[] HeightFieldData;

            /// <summary>
            /// Baked obstacle field: Zero represents a non-occluded cell, a positive number indicates multiple occlusions on a single cell, and a negative number is invalid.
            /// </summary>
            [Tooltip("Baked obstacle field: Zero represents a non-occluded cell, a positive number indicates multiple occlusions on a single cell, and a negative number is invalid.")]
            [SerializeField]
            public int[] ObstacleFieldData;

            /// <summary>
            /// Returns error 
            /// </summary>
            public void Validate()
            {
                CollectionChecks.CheckPositive(Width);
                CollectionChecks.CheckPositive(Height);

                if (HeightFieldData == null)
                    throw new InvalidOperationException("HeightField must be not null.");
                if (ObstacleFieldData == null)
                    throw new InvalidOperationException("HeightField must be not null.");

                int length = Width * Height;
                if (HeightFieldData.Length != length)
                    throw new InvalidOperationException($"HeightField length {HeightFieldData.Length} does not match width and height.");
                if (ObstacleFieldData.Length != length)
                    throw new InvalidOperationException($"HeightField length {HeightFieldData.Length} does not match width and height.");
            }
        }

        /// <summary>
        /// Maximum slope in degress.
        /// </summary>
        public float MaxSlope => m_MaxSlope;

        /// <summary>
        /// Maximum height of cell.
        /// </summary>
        public float MaxHeight => m_MaxHeight;

        /// <summary>
        /// Radius of agent.
        /// </summary>
        public float Radius => m_Radius;

        /// <summary>
        /// Settings that define objects that will be collected for crowd surface data baking.
        /// </summary>
        ObjectCollection Collection => m_Collection;

        /// <summary>
        /// Cell color then drawing gizmos.
        /// </summary>
        public Color GizmosColor => m_GizmosColor;

        /// <summary>
        /// Baked data.
        /// </summary>
        public Baked BakedData => m_Baked;

        /// <summary>
        /// Baked data width.
        /// </summary>
        public int Width => m_Baked.Width;

        /// <summary>
        /// Baked data height.
        /// </summary>
        public int Height => m_Baked.Height;

        /// <summary>
        /// Multiplication of width and height.
        /// </summary>
        public int Length => m_Baked.ObstacleFieldData.Length;

        /// <summary>
        /// Baked height field.
        /// </summary>
        public ReadOnlySpan<float> HeightField => m_Baked.HeightFieldData;

        /// <summary>
        /// Baked obstacle field: Zero represents a non-occluded cell, a positive number indicates multiple occlusions on a single cell, and a negative number is invalid.
        /// </summary>
        public ReadOnlySpan<int> ObstacleField => m_Baked.ObstacleFieldData;

        /// <summary>
        /// Returns interpolated height at world space position.
        /// </summary>
        public float SampleHeight(float2 point)
        {
            // Find closest cell center that coordinates are both less than of the point
            int2 cellA = (int2) floor(point - 0.5f);
            int2 cellB = cellA + int2(1, 0);
            int2 cellC = cellA + int2(0, 1);
            int2 cellD = cellA + int2(1, 1);

            float2 delta = saturate(point - ((float2) cellA + 0.5f));
            float2 oneMinusDelta = 1.0f - delta;

            float2 q11 = IsValidCell(cellA) ? float2(GetHeight(cellA), 1) : 0;
            float2 q12 = IsValidCell(cellB) ? float2(GetHeight(cellB), 1) : 0;
            float2 q21 = IsValidCell(cellC) ? float2(GetHeight(cellC), 1) : 0;
            float2 q22 = IsValidCell(cellD) ? float2(GetHeight(cellD), 1) : 0;

            float4 weights;
            weights.x = oneMinusDelta.x * oneMinusDelta.y;
            weights.y = oneMinusDelta.x * delta.y;
            weights.z = delta.x * oneMinusDelta.y;
            weights.w = delta.x * delta.y;

            float2 interpolated =
                q11 * weights.x +
                q21 * weights.y +
                q12 * weights.z +
                q22 * weights.w;

            if (interpolated.y == 0)
                return 0;

            return interpolated.x / interpolated.y;
        }

        /// <summary>
        /// Sets new baked data. This can be used to bake it outside this class.
        /// </summary>
        public void SetBakedData(in Baked baked)
        {
            baked.Validate();
            m_Baked = baked;
        }

        /// <summary>
        /// Builds height field out of physics <see cref="Collider"/> in the scene.
        /// </summary>
        public void BuildHeightFieldFromColliders(int width, int height, NonUniformTransform transform)
        {
            float maxHeight = m_MaxHeight;

            var normal = transform.Forward();

            float[] heightField = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int2 cell = new int2(x, y);

                    float3 position = new float3(x + 0.5f, y + 0.5f, -maxHeight);
                    position = transform.TransformPoint(position);

                    Ray ray = new Ray(position, normal);

                    if (Physics.Raycast(ray, out RaycastHit hit, maxHeight, m_Collection.Layers))
                    {
                        heightField[GetCellIndex(cell)] = maxHeight - hit.distance;
                    }
                    else
                    {
                        heightField[GetCellIndex(cell)] = 0;
                    }
                }
            }

            m_Baked.Width = width;
            m_Baked.Height = height;
            m_Baked.HeightFieldData = heightField;

            RecalculateObstacleField();
        }

        /// <summary>
        /// Builds height field out of physics <see cref="Collider"/> in the scene with respect of agent radius.
        /// </summary>
        public void BuildHeightFieldFromCollidersWithRadius(int width, int height, NonUniformTransform transform)
        {
            float maxHeight = m_MaxHeight;

            float maxStepHeight = sin(radians(m_MaxSlope));

            float radius = m_Radius;

            m_Baked.Width = width;
            m_Baked.Height = height;

            var normal = transform.Forward();

            m_Baked.HeightFieldData = new float[width * height];
            m_Baked.ObstacleFieldData = new int[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int2 cell = new int2(x, y);

                    float2 point = float2(x + 0.5f, y + 0.5f);

                    float3 position = transform.TransformPoint(new float3(point, -maxHeight));

                    Ray ray = new Ray(position, normal);

                    if (Physics.Raycast(ray, out RaycastHit hit, maxHeight + 0.1f, m_Collection.Layers))
                    {
                        float h = maxHeight - hit.distance;
                        m_Baked.HeightFieldData[GetCellIndex(cell)] = h;

                        if (CheckIfCapsuleCanNotStandThere(transform, point, normal, radius, maxHeight, maxStepHeight))
                        {
                            m_Baked.ObstacleFieldData[GetCellIndex(cell)]++;
                            continue;
                        }
                    }
                    else
                    {
                        m_Baked.ObstacleFieldData[GetCellIndex(cell)]++;
                        m_Baked.HeightFieldData[GetCellIndex(cell)] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Updates obstacle field from <see cref="Baked.HeightFieldData"/>. Make sure height field is updated before calling this method.
        /// </summary>
        public void RecalculateObstacleField()
        {
            float maxStepHeight = atan(radians(m_MaxSlope));

            m_Baked.ObstacleFieldData = new int[m_Baked.Width * m_Baked.Height];

            for (int y = 0; y < m_Baked.Height; y++)
            {
                for (int x = 0; x < m_Baked.Width; x++)
                {
                    int2 cell = new int2(x, y);
                    int2 cellE = new int2(x + 1, y);
                    int2 cellW = new int2(x - 1, y);
                    int2 cellN = new int2(x, y + 1);
                    int2 cellS = new int2(x, y - 1);

                    if (IsValidCell(cellE))
                    {
                        float stepHeight = m_Baked.HeightFieldData[GetCellIndex(cell)] - m_Baked.HeightFieldData[GetCellIndex(cellE)];
                        m_Baked.ObstacleFieldData[GetCellIndex(cell)] += stepHeight > maxStepHeight ? (int) 1 : (int) 0;
                    }

                    if (IsValidCell(cellW))
                    {
                        float stepHeight = m_Baked.HeightFieldData[GetCellIndex(cell)] - m_Baked.HeightFieldData[GetCellIndex(cellW)];
                        m_Baked.ObstacleFieldData[GetCellIndex(cell)] += stepHeight > maxStepHeight ? (int) 1 : (int) 0;
                    }

                    if (IsValidCell(cellN))
                    {
                        float stepHeight = m_Baked.HeightFieldData[GetCellIndex(cell)] - m_Baked.HeightFieldData[GetCellIndex(cellN)];
                        m_Baked.ObstacleFieldData[GetCellIndex(cell)] += stepHeight > maxStepHeight ? (int) 1 : (int) 0;
                    }

                    if (IsValidCell(cellS))
                    {
                        float stepHeight = m_Baked.HeightFieldData[GetCellIndex(cell)] - m_Baked.HeightFieldData[GetCellIndex(cellS)];
                        m_Baked.ObstacleFieldData[GetCellIndex(cell)] += stepHeight > maxStepHeight ? (int) 1 : (int) 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true, if cell is within world bounds and has no obstacle.
        /// </summary>
        public bool IsValidCell(int2 cell) => cell.x >= 0 && cell.y >= 0 && cell.x < m_Baked.Width && cell.y < m_Baked.Height && m_Baked.ObstacleFieldData[GetCellIndex(cell)] == 0;

        /// <summary>
        /// Converts cell to index.
        /// </summary>
        public int GetCellIndex(int2 point) => point.y * m_Baked.Width + point.x;

        /// <summary>
        /// Converts index to cell.
        /// </summary>
        public int2 GetCell(int index) => new int2(index % m_Baked.Width, index / m_Baked.Width);

        float GetHeight(int2 cell) => m_Baked.HeightFieldData[GetCellIndex(cell)];

        bool CheckIfCapsuleCanNotStandThere(NonUniformTransform transform, float2 point, float3 normal, float radius, float maxHeight, float maxStepHeight)
        {
            float3 position = transform.TransformPoint(new float3(point, -maxHeight));
            Ray ray = new Ray(position, normal);

            float offset = radius;

            if (Physics.SphereCast(ray, radius, out RaycastHit hit, maxHeight + 0.1f, m_Collection.Layers))
            {
                hit.distance += radius;
                if (IsFailedCheck(transform, float2(point.x + offset, point.y), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x - offset, point.y), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x, point.y + offset), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x, point.y - offset), normal, radius, hit, maxHeight, maxStepHeight) ||

                    IsFailedCheck(transform, float2(point.x + offset, point.y + offset), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x - offset, point.y + offset), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x + offset, point.y - offset), normal, radius, hit, maxHeight, maxStepHeight) ||
                    IsFailedCheck(transform, float2(point.x - offset, point.y - offset), normal, radius, hit, maxHeight, maxStepHeight))
                {
                    return true;
                }
            }
            return false;
        }

        bool IsFailedCheck(NonUniformTransform transform, float2 point, float3 normal, float radius, RaycastHit hit, float maxHeight, float maxStepHeight)
        {
            float3 position = transform.TransformPoint(new float3(point, -maxHeight));
            float height = maxHeight - hit.distance;
            Ray ray = new Ray(position, normal);
            if (!Physics.Raycast(ray, out RaycastHit hitNew, maxHeight + 0.1f, m_Collection.Layers))
                return true;
            float heightNew = maxHeight - hitNew.distance;
            float step = abs(height - heightNew) / distance(hit.point, hitNew.point); // sinA
            return maxStepHeight < step;
        }
    }
}
