using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// World in which crowd will navigate.
    /// This container does not contain thread safety checks, make sure it is contained in structure that ensures that.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CrowdWorld : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeCrowdWorld* m_Data;
        AllocatorManager.AllocatorHandle m_Allocator;

        public bool IsCreated => m_Data != null;
        public NativeArray<float> HeightField => m_Data->HeightField;
        public NativeArray<float4> HeightGradientField => m_Data->HeightGradientField;
        public NativeArray<float> DensityField => m_Data->DensityField;
        public NativeArray<float2> AverageVelocityField => m_Data->AverageVelocityField;
        public NativeArray<int> ObstacleField => m_Data->ObstacleField;
        public NativeArray<float> DiscomfortField => m_Data->DiscomfortField;
        public int Width => m_Data->Width;
        public int Height => m_Data->Height;
        public int Length => m_Data->Length;
        public NonUniformTransform Transform => m_Data->Transform;
        public Density Density { get => m_Data->Density; set => m_Data->Density = value; }
        public Slope Slope { get => m_Data->Slope; set => m_Data->Slope = value; }

        public CrowdWorld(int width, int height, NonUniformTransform transform, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = AllocatorManager.Allocate<UnsafeCrowdWorld>(allocator);
            *m_Data = new UnsafeCrowdWorld(width, height, transform, allocator);
            m_Allocator = allocator;
        }

        /// <summary>
        /// Returns unsafe pointer.
        /// </summary>
        public UnsafeCrowdWorld* GetUnsafe() => m_Data;

        /// <summary>
        /// Maps constrained position that is within the navigatable surface.
        /// </summary>
        /// <param name="position">Mapping center.</param>
        /// <param name="radius">Mapping radius.</param>
        /// <param name="result">Constrained position withing the navigatable surface.</param>
        /// <returns>Returns true, if such position exists. It can fail, if position is too far.</returns>
        public bool MapLocation(float3 position, float radius, out float3 result)
        {
            return m_Data->MapLocation(position, radius, out result);
        }

        public void SetHeightField(ReadOnlySpan<float> heights)
        {
            m_Data->SetHeightField(heights);
        }

        /// <summary>
        /// Returns interpolated height at world space position.
        /// </summary>
        public float SampleHeight(float3 point)
        {
            return m_Data->SampleHeight(point);
        }

        /// <summary>
        /// Returns interpolated height at local space point.
        /// </summary>
        public float SampleHeight(float2 point)
        {
            return m_Data->SampleHeight(point);
        }

        public void SetObstacleField(ReadOnlySpan<int> obstacles)
        {
            m_Data->SetObstacleField(obstacles);
        }

        /// <summary>
        /// Updates height gradient field from height field. Must be called after height changes (etc. <see cref="SetHeightField(ReadOnlySpan{float})"/>).
        /// </summary>
        public void RecalculateHeightGradientField()
        {
            m_Data->RecalculateHeightGradientField();
        }

        /// <summary>
        /// Clears accumulated density.
        /// </summary>
        public void ClearDensity()
        {
            m_Data->ClearDensity();
        }

        /// <summary>
        /// Clears accumulated discomfort.
        /// </summary>
        public void ClearDiscomfort()
        {
            m_Data->ClearDiscomfort();
        }

        /// <summary>
        /// Splats density at world space position. Must be called after <see cref="ClearDensity"/>.
        /// </summary>
        public void SplatDensity(float3 position, float3 velocity = default)
        {
            m_Data->SplatDensity(position, velocity);
        }

        /// <summary>
        /// Normalizes average velocity field. Must be called after all density modifications (etc. <see cref="SplatDensity(Unity.Mathematics.float3, Unity.Mathematics.float3)"/>).
        /// </summary>
        public void NormalizeAverageVelocityField()
        {
            m_Data->NormalizeAverageVelocityField();
        }

        /// <summary>
        /// Returns true, if quad area is not covered by obstacles.
        /// </summary>
        public bool IsValidQuad(float3 position, float3 size)
        {
            return m_Data->IsValidQuad(position, size);
        }

        /// <summary>
        /// Returns true, if quad area is not covered by obstacles.
        /// </summary>
        public bool IsValidQuad(float2 start, float2 end)
        {
            return m_Data->IsValidQuad(start, end);
        }

        /// <summary>
        /// Splats quad shape obstacle at world space position.
        /// </summary>
        public void SplatObstacleQuad(float3 position, float3 size, int opacity = 1)
        {
            m_Data->SplatObstacleQuad(position, size, opacity);
        }

        /// <summary>
        /// Splats circle shape obstacle at world space position.
        /// </summary>
        public void SplatObstacleCircle(float3 position, float3 size, int opacity = 1)
        {
            m_Data->SplatObstacleCircle(position, size, opacity);
        }

        /// <summary>
        /// Splats quad shape discomfort at world space position.
        /// </summary>
        public void SplatDiscomfortQuad(float3 position, float3 size, float2 gradient)
        {
            m_Data->SplatDiscomfortQuad(position, size, gradient);
        }

        /// <summary>
        /// Splats circle shape discomfort at world space position.
        /// </summary>
        public void SplatDiscomfortCircle(float3 position, float3 size, float2 gradient)
        {
            m_Data->SplatDiscomfortCircle(position, size, gradient);
        }

        /// <summary>
        /// Raycasts the height field and if succeds returns hit position.
        /// </summary>
        public bool RaycastHeightField(float3 origin, float3 direction, out float3 hit)
        {
            return m_Data->RaycastHeightField(origin, direction, out hit);
        }

        /// <summary>
        /// Converts cell to index.
        /// </summary>
        public int GetCellIndex(int2 cell) => m_Data->GetCellIndex(cell);

        /// <summary>
        /// Converts index to cell.
        /// </summary>
        public int2 GetCell(int index) => m_Data->GetCell(index);

        /// <summary>
        /// Returns true, if cell is within world bounds and has no obstacle.
        /// </summary>
        public bool IsValidCell(int2 cell) => m_Data->IsValidCell(cell);

        /// <summary>
        /// Returns cell, if it is valid.
        /// </summary>
        public bool TryGetCell(float3 position, out int2 cell) => m_Data->TryGetCell(position, out cell);

        /// <summary>
        /// Returns cell enter world space position.
        /// </summary>
        public float3 GetCellPosition(int2 cell) => m_Data->GetCellPosition(cell);

        /// <summary>
        /// Releases all resources related to this container.
        /// </summary>
        public void Dispose()
        {
            m_Data->Dispose();
            AllocatorManager.Free(m_Allocator, m_Data);
            m_Data = null;
        }

        /// <summary>
        /// Returns world parallel writer.
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter { m_Data = m_Data };
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeCrowdWorld* m_Data;

            /// <summary>
            /// Splats quad shape obstacle at world space position.
            /// </summary>
            public void SplatObstacleQuad(float3 position, float3 size, int opacity = 1)
            {
                float3 extent = float3((size - UnsafeCrowdWorld.SizeTreshold) * 0.5f);
                float2 startLS = m_Data->Transform.InverseTransformPoint(position - extent).xy;
                float2 endLS = m_Data->Transform.InverseTransformPoint(position + extent).xy;
                SplatObstacleQuad(startLS, endLS, opacity);
            }

            /// <summary>
            /// Splats quad shape obstacle at local space point.
            /// </summary>
            public void SplatObstacleQuad(float2 start, float2 end, int opacity = 1)
            {
                var obstacleField = m_Data->ObstacleField.AsSpan();

                int2 startCell = (int2) floor(start);
                int2 endCell = (int2) floor(end) + 1;

                int2 cell;
                for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
                {
                    for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                    {
                        if (cell.x < 0 || cell.y < 0 || cell.x >= m_Data->Width || cell.y >= m_Data->Height)
                            continue;
                        System.Threading.Interlocked.Add(ref obstacleField[m_Data->GetCellIndex(cell)], opacity);
                    }
                }
            }

            /// <summary>
            /// Splats circle shape obstacle at world space position.
            /// </summary>
            public void SplatObstacleCircle(float3 position, float3 size, int opacity = 1)
            {

                float3 extent = float3((size - UnsafeCrowdWorld.SizeTreshold) * 0.5f);
                float2 startLS = m_Data->Transform.InverseTransformPoint(position - extent).xy;
                float2 endLS = m_Data->Transform.InverseTransformPoint(position + extent).xy;
                SplatObstacleCircle(startLS, endLS, opacity);
            }

            /// <summary>
            /// Splats quad shape obstacle at local space point.
            /// </summary>
            public void SplatObstacleCircle(float2 start, float2 end, int opacity = 1)
            {
                var obstacleField = m_Data->ObstacleField.AsSpan();

                int2 startCell = (int2) floor(start);
                int2 endCell = (int2) floor(end) + 1;

                float2 center = (start + end) / 2;
                float maxDistance = distancesq(start, center);

                if (maxDistance < EPSILON)
                    return;

                int2 cell;
                for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
                {
                    for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                    {
                        if (cell.x < 0 || cell.y < 0 || cell.x >= m_Data->Width || cell.y >= m_Data->Height)
                            continue;
                        float distance = distancesq((float2) cell + 0.5f, center);
                        if (maxDistance < distance)
                            continue;
                        System.Threading.Interlocked.Add(ref obstacleField[m_Data->GetCellIndex(cell)], opacity);
                    }
                }
            }

            /// <summary>
            /// Splats circle shape discomfort at world space position. Must be called after <see cref="ClearDiscomfort"/>.
            /// </summary>
            public void SplatDiscomfortCircle(float3 position, float3 size, float2 gradient)
            {
                float3 extent = float3((size - UnsafeCrowdWorld.SizeTreshold) * 0.5f);
                float2 startLS = m_Data->Transform.InverseTransformPoint(position - extent).xy;
                float2 endLS = m_Data->Transform.InverseTransformPoint(position + extent).xy;
                SplatDiscomfortCircle(startLS, endLS, gradient);
            }

            /// <summary>
            /// Splats circle shape discomfort at local space point. Must be called after <see cref="ClearDiscomfort"/>.
            /// </summary>
            public void SplatDiscomfortCircle(float2 start, float2 end, float2 gradient)
            {
                var discomfortField = m_Data->DiscomfortField.AsSpan();

                int2 startCell = (int2) floor(start);
                int2 endCell = (int2) floor(end) + 1;

                float2 center = (start + end) / 2;
                float distance = distancesq(start, center);

                if (distance < EPSILON)
                    return;

                int2 cell;
                for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
                {
                    for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                    {
                        if (cell.x < 0 || cell.y < 0 || cell.x >= m_Data->Width || cell.y >= m_Data->Height)
                            continue;
                        int index = m_Data->GetCellIndex(cell);
                        float interpolator = saturate(distancesq((float2) cell + 0.5f, center) / distance);
                        InterlockedAdd(ref discomfortField[index], lerp(gradient.x, gradient.y, interpolator));
                    }
                }
            }

            /// <summary>
            /// Splats quad shape discomfort at world space position. Must be called after <see cref="ClearDiscomfort"/>.
            /// </summary>
            public void SplatDiscomfortQuad(float3 position, float3 size, float2 gradient)
            {
                float3 extent = float3((size - UnsafeCrowdWorld.SizeTreshold) * 0.5f);
                float2 startLS = m_Data->Transform.InverseTransformPoint(position - extent).xy;
                float2 endLS = m_Data->Transform.InverseTransformPoint(position + extent).xy;
                SplatDiscomfortQuad(startLS, endLS, gradient);
            }

            /// <summary>
            /// Splats quad shape discomfort at local space point. Must be called after <see cref="ClearDiscomfort"/>.
            /// </summary>
            public void SplatDiscomfortQuad(float2 start, float2 end, float2 gradient)
            {
                var discomfortField = m_Data->DiscomfortField.AsSpan();

                int2 startCell = (int2) floor(start);
                int2 endCell = (int2) floor(end) + 1;

                float2 center = (start + end) / 2;
                float distance = DistanceManhatten(start, center);

                if (distance < EPSILON)
                    return;

                int2 cell;
                for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
                {
                    for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                    {
                        if (cell.x < 0 || cell.y < 0 || cell.x >= m_Data->Width || cell.y >= m_Data->Height)
                            continue;
                        int index = m_Data->GetCellIndex(cell);
                        float interpolator = saturate(DistanceManhatten((float2) cell + 0.5f, center) / distance);
                        InterlockedAdd(ref discomfortField[index], lerp(gradient.x, gradient.y, interpolator));
                    }
                }
            }

            /// <summary>
            /// Splats density at world space position. Must be called after <see cref="ClearDensity"/>.
            /// </summary>
            public void SplatDensity(float3 position, float3 velocity = default)
            {
                float2 point = m_Data->Transform.InverseTransformPoint(position).xy;
                float2 vel = m_Data->Transform.InverseTransformDirection(velocity).xy;
                SplatDensity(point, vel);
            }

            /// <summary>
            /// Splats density at local space point. Must be called after <see cref="ClearDensity"/>.
            /// </summary>
            public void SplatDensity(float2 point, float2 velocity = default)
            {
                var densityField = m_Data->DensityField.AsSpan();
                var averageVelocityField = m_Data->AverageVelocityField.AsSpan();

                // Find closest cell center that coordinates are both less than of the point
                int2 cellA = (int2) floor(point - 0.5f);
                int2 cellB = cellA + int2(1, 0);
                int2 cellC = cellA + int2(1, 1);
                int2 cellD = cellA + int2(0, 1);

                float2 delta = saturate(point - ((float2) cellA + 0.5f));
                float2 oneMinusDelta = 1.0f - delta;

                // | D | C |
                // | A | B |

                if (m_Data->IsValidCell(cellA))
                {
                    int index = m_Data->GetCellIndex(cellA);
                    float desnity = pow(min(oneMinusDelta.x, oneMinusDelta.y), m_Data->Density.Exponent);
                    InterlockedAdd(ref densityField[index], desnity);
                    InterlockedAdd(ref averageVelocityField[index], velocity * desnity);
                }

                if (m_Data->IsValidCell(cellB))
                {
                    int index = m_Data->GetCellIndex(cellB);
                    float desnity = pow(min(delta.x, oneMinusDelta.y), m_Data->Density.Exponent);
                    InterlockedAdd(ref densityField[index], desnity);
                    InterlockedAdd(ref averageVelocityField[index], velocity * desnity);
                }

                if (m_Data->IsValidCell(cellC))
                {
                    int index = m_Data->GetCellIndex(cellC);
                    float desnity = pow(min(delta.x, delta.y), m_Data->Density.Exponent);
                    InterlockedAdd(ref densityField[index], desnity);
                    InterlockedAdd(ref averageVelocityField[index], velocity * desnity);
                }

                if (m_Data->IsValidCell(cellD))
                {
                    int index = m_Data->GetCellIndex(cellD);
                    float desnity = pow(min(oneMinusDelta.x, delta.y), m_Data->Density.Exponent);
                    InterlockedAdd(ref densityField[index], desnity);
                    InterlockedAdd(ref averageVelocityField[index], velocity * desnity);
                }
            }

            static float DistanceManhatten(float2 a, float2 b)
            {
                float2 d = abs(a - b);
                return max(d.x, d.y);
            }

            /// <summary>
            /// Based on https://stackoverflow.com/questions/1400465/why-is-there-no-overload-of-interlocked-add-that-accepts-doubles-as-parameters.
            /// </summary>
            static void InterlockedAdd(ref float location1, float value)
            {
                float newCurrentValue = location1; // non-volatile read, so may be stale
                while (true)
                {
                    float currentValue = newCurrentValue;
                    float newValue = currentValue + value;
                    newCurrentValue = System.Threading.Interlocked.CompareExchange(ref location1, newValue, currentValue);
                    if (newCurrentValue.Equals(currentValue)) // see "Update" below
                        return;
                }
            }

            static void InterlockedAdd(ref float2 location1, float2 value)
            {
                InterlockedAdd(ref location1.x, value.x);
                InterlockedAdd(ref location1.y, value.y);
            }
        }
    }

    public struct UnsafeCrowdWorld : IDisposable
    {
        public const float SizeTreshold = 0.001f;

        int m_Width;
        int m_Height;
        NonUniformTransform m_Transform;

        NativeArray<float> m_HeightField;
        NativeArray<float4> m_HeightGradientField;
        NativeArray<float> m_DensityField;
        NativeArray<float2> m_AverageVelocityField;
        NativeArray<int> m_ObstacleField;
        NativeArray<float> m_DiscomfortField;

        Density m_Density;
        Slope m_Slope;

        public NativeArray<float> HeightField => m_HeightField;
        public NativeArray<float4> HeightGradientField => m_HeightGradientField;
        public NativeArray<float> DensityField => m_DensityField;
        public NativeArray<float2> AverageVelocityField => m_AverageVelocityField;
        public NativeArray<int> ObstacleField => m_ObstacleField;
        public NativeArray<float> DiscomfortField => m_DiscomfortField;
        public int Width => m_Width;
        public int Height => m_Height;
        public int Length => HeightField.Length;
        public Density Density { get => m_Density; set => m_Density = value; }
        public Slope Slope { get => m_Slope; set => m_Slope = value; }
        public NonUniformTransform Transform => m_Transform;

        public UnsafeCrowdWorld(int width, int height, NonUniformTransform transform, AllocatorManager.AllocatorHandle allocator)
        {
            m_Width = width;
            m_Height = height;
            m_Transform = transform;

            m_Density = Density.Default;
            m_Slope = Slope.Default;

            int length = width * height;
            m_HeightField = new NativeArray<float>(length, allocator.ToAllocator);
            m_HeightGradientField = new NativeArray<float4>(length, allocator.ToAllocator);
            m_DensityField = new NativeArray<float>(length, allocator.ToAllocator);
            m_AverageVelocityField = new NativeArray<float2>(length, allocator.ToAllocator);
            m_ObstacleField = new NativeArray<int>(length, allocator.ToAllocator);
            m_DiscomfortField = new NativeArray<float>(length, allocator.ToAllocator);
        }

        public void SetHeightField(ReadOnlySpan<float> heights)
        {
            if (heights.Length != m_HeightField.Length)
                throw new InvalidOperationException();

            unsafe
            {
                fixed (float* heightPointer = heights)
                {
                    UnsafeUtility.MemCpy(m_HeightField.GetUnsafePtr(), heightPointer, sizeof(float) * heights.Length);
                }
            }
        }

        public void SetObstacleField(ReadOnlySpan<int> obstacles)
        {
            if (obstacles.Length != m_ObstacleField.Length)
                throw new InvalidOperationException();

            unsafe
            {
                fixed (int* ptr = obstacles)
                {
                    UnsafeUtility.MemCpy(m_ObstacleField.GetUnsafePtr(), ptr, sizeof(int) * obstacles.Length);
                }
            }
        }

        public void RecalculateHeightGradientField()
        {
            for (int y = 0; y < m_Height; y++)
            {
                for (int x = 0; x < m_Width; x++)
                {
                    int2 cell = int2(x, y);
                    int2 cellE = cell + int2(1, 0);
                    int2 cellW = cell + int2(-1, 0);
                    int2 cellN = cell + int2(0, 1);
                    int2 cellS = cell + int2(0, -1);

                    float height = m_HeightField[GetCellIndex(cell)];

                    // Calculate anisotropic height differences
                    float4 heightGradient;
                    heightGradient.x = IsValidCell(cellE) ? m_HeightField[GetCellIndex(cellE)] - height : 0f;
                    heightGradient.y = IsValidCell(cellW) ? m_HeightField[GetCellIndex(cellW)] - height : 0f;
                    heightGradient.z = IsValidCell(cellN) ? m_HeightField[GetCellIndex(cellN)] - height : 0f;
                    heightGradient.w = IsValidCell(cellS) ? m_HeightField[GetCellIndex(cellS)] - height : 0f;

                    m_HeightGradientField[GetCellIndex(cell)] = heightGradient;
                }
            }
        }

        public bool MapLocation(float3 position, float radius, out float3 result)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            if (!MapLocation(point, radius, out point))
            {
                result = 0;
                return false;
            }
            float height = -SampleHeight(point);
            result = m_Transform.TransformPoint(float3(point, height));
            return true;
        }

        public bool MapLocation(float2 point, float radius, out float2 result)
        {
            int2 cell = (int2) floor(point);
            int maxIterations = (int) floor(radius);

            if (IsValidCell(cell))
            {
                result = ClosestPoint(cell, point);
                return true;
            }

            for (int iteration = 1; iteration < maxIterations; iteration++)
            {
                float minDistance = float.MaxValue;
                int2 minCell = 0;

                for (int i = cell.x - iteration; i < cell.x + iteration + 1; i++)
                {
                    int2 positiveCell = int2(i, cell.y + iteration);
                    if (IsValidCell(positiveCell))
                    {
                        float cellDistance = distancesq(point, ClosestPoint(positiveCell, point));
                        if (minDistance > cellDistance)
                        {
                            minDistance = cellDistance;
                            minCell = positiveCell;
                        }
                    }

                    int2 negativeCell = int2(i, cell.y - iteration);
                    if (IsValidCell(negativeCell))
                    {
                        float cellDistance = distancesq(point, ClosestPoint(negativeCell, point));
                        if (minDistance > cellDistance)
                        {
                            minDistance = cellDistance;
                            minCell = negativeCell;
                        }
                    }
                }

                for (int i = cell.y - iteration - 1; i < cell.y + iteration; i++)
                {
                    int2 positiveCell = int2(cell.x + iteration, i);
                    if (IsValidCell(positiveCell))
                    {
                        float cellDistance = distancesq(point, ClosestPoint(positiveCell, point));
                        if (minDistance > cellDistance)
                        {
                            minDistance = cellDistance;
                            minCell = positiveCell;
                        }
                    }

                    int2 negativeCell = int2(cell.x - iteration, i);
                    if (IsValidCell(negativeCell))
                    {
                        float cellDistance = distancesq(point, ClosestPoint(negativeCell, point));
                        if (minDistance > cellDistance)
                        {
                            minDistance = cellDistance;
                            minCell = negativeCell;
                        }
                    }
                }

                if (minDistance != float.MaxValue)
                {
                    result =  ClosestPoint(minCell, point);
                    return true;
                }
            }

            result = 0;
            return false;
        }

        /// <summary>
        /// Returns a point on the perimeter of this rectangle that is closest to the specified point.
        /// </summary>
        float2 ClosestPoint(int2 cell, float2 point)
        {
            return clamp(point, cell, cell + 1);
        }

        public float SampleHeight(float3 position)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            return SampleHeight(point);
        }

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

        public float GetHeight(int2 cell)
        {
            return m_HeightField[GetCellIndex(cell)];
        }

        public bool IsValidQuad(float3 position, float3 size)
        {
            float3 extent = float3(size * 0.5f);
            float2 startLS = m_Transform.InverseTransformPoint(position - extent).xy;
            float2 endLS = m_Transform.InverseTransformPoint(position + extent).xy;
            return IsValidQuad(startLS, endLS);
        }

        public bool IsValidQuad(float2 start, float2 end)
        {
            int2 startCell = (int2) floor(start);
            int2 endCell = (int2) floor(end) + 1;

            int2 cell;
            for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
            {
                for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                {
                    if (!IsValidCell(cell))
                        return false;
                }
            }
            return true;
        }

        public void SplatObstacleQuad(float3 position, float3 size, int opacity = 1)
        {
            float3 extent = float3((size - SizeTreshold) * 0.5f);
            float2 startLS = m_Transform.InverseTransformPoint(position - extent).xy;
            float2 endLS = m_Transform.InverseTransformPoint(position + extent).xy;
            SplatObstacleQuad(startLS, endLS, opacity);
        }

        public void SplatObstacleQuad(float2 start, float2 end, int opacity = 1)
        {
            int2 startCell = (int2) floor(start);
            int2 endCell = (int2) floor(end) + 1;

            int2 cell;
            for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
            {
                for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                {
                    if (cell.x < 0 || cell.y < 0 || cell.x >= m_Width || cell.y >= m_Height)
                        continue;
                    m_ObstacleField[GetCellIndex(cell)] += opacity;
                    CheckObstacleCell(cell);
                }
            }
        }

        public void SplatObstacleCircle(float3 position, float3 size, int opacity = 1)
        {
            float3 extent = float3((size - SizeTreshold) * 0.5f);
            float2 startLS = m_Transform.InverseTransformPoint(position - extent).xy;
            float2 endLS = m_Transform.InverseTransformPoint(position + extent).xy;
            SplatObstacleCircle(startLS, endLS, opacity);
        }

        public void SplatObstacleCircle(float2 start, float2 end, int opacity = 1)
        {
            int2 startCell = (int2) floor(start);
            int2 endCell = (int2) floor(end) + 1;

            float2 center = (start + end) / 2;
            float maxDistance = distancesq(start, center);

            if (maxDistance < EPSILON)
                return;

            int2 cell;
            for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
            {
                for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                {
                    if (cell.x < 0 || cell.y < 0 || cell.x >= m_Width || cell.y >= m_Height)
                        continue;
                    int index = GetCellIndex(cell);
                    float distance = distancesq((float2) cell + 0.5f, center);
                    if (maxDistance < distance)
                        continue;
                    m_ObstacleField[GetCellIndex(cell)] += opacity;
                    CheckObstacleCell(cell);
                }
            }
        }

        public void SplatDiscomfortCircle(float3 position, float3 size, float2 gradient)
        {
            float3 extent = float3((size - SizeTreshold) * 0.5f);
            float2 startLS = m_Transform.InverseTransformPoint(position - extent).xy;
            float2 endLS = m_Transform.InverseTransformPoint(position + extent).xy;
            SplatDiscomfortCircle(startLS, endLS, gradient);
        }

        public void SplatDiscomfortCircle(float2 start, float2 end, float2 gradient)
        {
            int2 startCell = (int2) floor(start);
            int2 endCell = (int2) floor(end) + 1;

            float2 center = (start + end) / 2;
            float distance = distancesq(start, center);

            if (distance < EPSILON)
                return;

            int2 cell;
            for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
            {
                for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                {
                    if (!IsValidCell(cell))
                        continue;
                    int index = GetCellIndex(cell);
                    float interpolator = saturate(distancesq((float2) cell + 0.5f, center) / distance);
                    m_DiscomfortField[index] += lerp(gradient.x, gradient.y, interpolator);
                }
            }
        }

        public void SplatDiscomfortQuad(float3 position, float3 size, float2 gradient)
        {
            float3 extent = float3((size - SizeTreshold) * 0.5f);
            float2 startLS = m_Transform.InverseTransformPoint(position - extent).xy;
            float2 endLS = m_Transform.InverseTransformPoint(position + extent).xy;
            SplatDiscomfortQuad(startLS, endLS, gradient);
        }

        public void SplatDiscomfortQuad(float2 start, float2 end, float2 gradient)
        {
            int2 startCell = (int2) floor(start);
            int2 endCell = (int2) floor(end) + 1;

            float2 center = (start + end) / 2;
            float distance = DistanceManhatten(start, center);

            if (distance < EPSILON)
                return;

            int2 cell;
            for (cell.y = startCell.y; cell.y < endCell.y; cell.y++)
            {
                for (cell.x = startCell.x; cell.x < endCell.x; cell.x++)
                {
                    if (!IsValidCell(cell))
                        continue;
                    int index = GetCellIndex(cell);
                    float interpolator = saturate(DistanceManhatten((float2) cell + 0.5f, center) / distance);
                    m_DiscomfortField[index] += lerp(gradient.x, gradient.y, interpolator);
                }
            }
        }

        static float DistanceManhatten(float2 a, float2 b)
        {
            float2 d = abs(a - b);
            return max(d.x, d.y);
        }

        public void SplatDensity(float3 position, float3 velocity = default)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            float2 vel = m_Transform.InverseTransformDirection(velocity).xy;
            SplatDensity(point, vel);
        }

        public void SplatDensity(float2 point, float2 velocity = default)
        {
            // Find closest cell center that coordinates are both less than of the point
            int2 cellA = (int2) floor(point - 0.5f);
            int2 cellB = cellA + int2(1, 0);
            int2 cellC = cellA + int2(1, 1);
            int2 cellD = cellA + int2(0, 1);

            float2 delta = saturate(point - ((float2) cellA + 0.5f));
            float2 oneMinusDelta = 1.0f - delta;

            // | D | C |
            // | A | B |

            if (IsValidCell(cellA))
            {
                int index = GetCellIndex(cellA);
                float desnity = pow(min(oneMinusDelta.x, oneMinusDelta.y), Density.Exponent);
                m_DensityField[index] += desnity;
                m_AverageVelocityField[index] += velocity * desnity;
            }

            if (IsValidCell(cellB))
            {
                int index = GetCellIndex(cellB);
                float desnity = pow(min(delta.x, oneMinusDelta.y), Density.Exponent);
                m_DensityField[index] += desnity;
                m_AverageVelocityField[index] += velocity * desnity;
            }

            if (IsValidCell(cellC))
            {
                int index = GetCellIndex(cellC);
                float desnity = pow(min(delta.x, delta.y), Density.Exponent);
                m_DensityField[index] += desnity;
                m_AverageVelocityField[index] += velocity * desnity;
            }

            if (IsValidCell(cellD))
            {
                int index = GetCellIndex(cellD);
                float desnity = pow(min(oneMinusDelta.x, delta.y), Density.Exponent);
                m_DensityField[index] += desnity;
                m_AverageVelocityField[index] += velocity * desnity;
            }
        }

        public void ClearDensity()
        {
            for (int i = 0; i < Length; i++)
                m_DensityField[i] = 0;
            for (int i = 0; i < Length; i++)
                m_AverageVelocityField[i] = 0;
        }

        public void ClearDiscomfort()
        {
            for (int i = 0; i < Length; i++)
                m_DiscomfortField[i] = 0;
        }

        public void NormalizeAverageVelocityField()
        {
            for (int i = 0; i < Length; i++)
            {
                float density = m_DensityField[i];
                if (density == 0)
                    continue;
                m_AverageVelocityField[i] /= density;
            }
        }

        public void Dispose()
        {
            m_HeightField.Dispose();
            m_HeightGradientField.Dispose();
            m_DensityField.Dispose();
            m_AverageVelocityField.Dispose();
            m_ObstacleField.Dispose();
            m_DiscomfortField.Dispose();
        }

        public bool RaycastHeightField(float3 origin, float3 direction, out float3 hit)
        {
            origin = m_Transform.InverseTransformPoint(origin);
            direction = m_Transform.InverseTransformDirection(direction);

            float2 a = float2(0, 0);
            float2 b = float2(m_Width, 0);
            float2 c = float2(m_Width, m_Height);
            float2 d = float2(0, m_Height);

            float tMin = float.MaxValue;
            float tMax = float.MinValue;

            // Down
            if (Intersection(origin.xy, direction.xy, a, b - a, out float2 t0) && t0.y > 0 && t0.y < 1)
            {
                tMin = min(tMin, t0.x);
                tMax = max(tMax, t0.x);
            }

            // Right
            if (Intersection(origin.xy, direction.xy, b, c - b, out float2 t1) && t1.y > 0 && t1.y < 1)
            {
                tMin = min(tMin, t1.x);
                tMax = max(tMax, t1.x);
            }

            // Top
            if (Intersection(origin.xy, direction.xy, c, d - c, out float2 t2) && t2.y > 0 && t2.y < 1)
            {
                tMin = min(tMin, t2.x);
                tMax = max(tMax, t2.x);
            }

            // Left
            if (Intersection(origin.xy, direction.xy, d, a - d, out float2 t3) && t3.y > 0 && t3.y < 1)
            {
                tMin = min(tMin, t3.x);
                tMax = max(tMax, t3.x);
            }

            if (tMin < 0 && tMax < 0)
            {
                hit = 0;
                return false;
            }

            for (int iteration = 0; iteration < 16; iteration++)
            {
                float tCenter = (tMax + tMin) * 0.5f;
                float3 point = origin + direction * tCenter;
                float height = -SampleHeight(point.xy);
                if (point.z < height)
                {
                    tMin = tCenter;
                }
                else
                {
                    tMax = tCenter;
                }
            }

            {
                float tCenter = (tMax + tMin) * 0.5f;
                hit = m_Transform.TransformPoint(origin + direction * tCenter);
            }
            return true;
        }

        /// <summary>
        /// Finds intersection times of two rays.
        /// Based on https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection.
        /// </summary>
        static bool Intersection(float2 aOrigin, float2 aDirection, float2 bOrigin, float2 bDirection, out float2 t)
        {
            float2 d = bOrigin - aOrigin;

            // Check if lines are not parallel
            float det = Determinant(aDirection, bDirection);
            if (abs(det) < math.EPSILON)
            {
                t = 0;
                return false;
            }

            t = new float2(Determinant(d, bDirection), Determinant(d, aDirection)) / det;
            return true;
        }

        /// <summary>
        /// Returns determinant of two vectors.
        /// Sum of cross product elements.
        /// </summary>
        static float Determinant(float2 a, float2 b) => a.x * b.y - a.y * b.x;

        /// <summary>
        /// Returns true, if cell is within world bounds and has no obstacle.
        /// </summary>
        public bool IsValidCell(int2 cell) => cell.x >= 0 && cell.y >= 0 && cell.x < m_Width && cell.y < m_Height && m_ObstacleField[GetCellIndex(cell)] == 0;

        /// <summary>
        /// Converts cell to index.
        /// </summary>
        public int GetCellIndex(int2 point) => point.y * m_Width + point.x;

        /// <summary>
        /// Converts index to cell.
        /// </summary>
        public int2 GetCell(int index) => new int2(index % m_Width, index / m_Width);

        /// <summary>
        /// Returns cell, if it is valid.
        /// </summary>
        public bool TryGetCell(float3 position, out int2 cell)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            cell = (int2) floor(point - 0.5f);
            return IsValidCell(cell);
        }

        /// <summary>
        /// Returns cell enter world space position.
        /// </summary>
        public float3 GetCellPosition(int2 cell)
        {
            return m_Transform.TransformPoint(new float3((float2)cell + 0.5f, -GetHeight(cell)));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckObstacleCell(int2 cell)
        {
            if (m_ObstacleField[GetCellIndex(cell)] < 0)
                throw new InvalidOperationException("Obstacle field can not have negative value. It usually indicates that obstacle was removed multiple times.");
        }
    }
}
