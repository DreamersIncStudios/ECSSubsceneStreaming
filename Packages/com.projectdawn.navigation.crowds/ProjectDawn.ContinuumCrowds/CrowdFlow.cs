using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProjectDawn.ContinuumCrowds
{
    /// <summary>
    /// A single crowd flow in the world. There can be multiple flows in the world.
    /// This container does not contain thread safety checks, make sure it is contained in structure that ensures that.
    /// </summary>
    public unsafe struct CrowdFlow : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeCrowdFlow* m_Data;
        AllocatorManager.AllocatorHandle m_Allocator;

        public bool IsCreated => m_Data != null;
        public NativeArray<float4> SpeedField => m_Data->SpeedField;
        public NativeArray<float> PotentialField => m_Data->PotentialField;
        public NativeList<int2> GoalCells => m_Data->GoalCells;
        public int Width => m_Data->Width;
        public int Height => m_Data->Height;
        public NonUniformTransform Transform => m_Data->Transform;
        public CostWeights CostWeights { get => m_Data->CostWeights; set => m_Data->CostWeights = value; }
        public Speed Speed { get => m_Data->Speed; set => m_Data->Speed = value; }

        public CrowdFlow(int width, int height, NonUniformTransform transform, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = AllocatorManager.Allocate<UnsafeCrowdFlow>(allocator);
            *m_Data = new UnsafeCrowdFlow(width, height, transform, allocator);
            m_Allocator = allocator;
        }

        /// <summary>
        /// Returns unsafe pointer.
        /// </summary>
        public UnsafeCrowdFlow* GetUnsafe() => m_Data;

        /// <summary>
        /// Clears all accumulated goals. Must be called before <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public void ClearGoals()
        {
            m_Data->ClearGoals();
        }

        /// <summary>
        /// Adds a new world space goal that the crowd will attempt to reach. Must be called before <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public void AddGoal(float3 position)
        {
            m_Data->AddGoal(position);
        }

        /// <summary>
        /// Adds a new world space goal that the crowd will attempt to reach. Must be called before <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public void AddGoal(float3 min, float3 max)
        {
            m_Data->AddGoal(min, max);
        }

        /// <summary>
        /// Updates the crowd speed field and unit cost field for the world.
        /// </summary>
        public void RecalculateSpeedAndCostField(CrowdWorld world)
        {
            CheckWorld(world);
            m_Data->RecalculateSpeedAndCostField(world.GetUnsafe());
        }

        /// <summary>
        /// Updates the crowd potential field for the world. Must be called after <see cref="RecalculateSpeedAndCostField(CrowdWorld)"/>.
        /// </summary>
        public void RecalculatePotentialField(CrowdWorld world, int iterations = int.MaxValue)
        {
            CheckWorld(world);
            m_Data->RecalculatePotentialField(world.GetUnsafe(), iterations);
        }

        /// <summary>
        /// Samples interpolated velocity at the world space position. Must be called after <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public float3 SampleVelocity(float3 position)
        {
            return m_Data->SampleVelocity(position);
        }

        /// <summary>
        /// Returns velocity at the cell center. Must be called after <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public float2 GetVelocity(int2 cell)
        {
            return m_Data->GetVelocity(cell);
        }

        /// <summary>
        /// Returns true if the world space position is considered a goal. Must be called after <see cref="RecalculatePotentialField(CrowdWorld)"/>.
        /// </summary>
        public bool IsGoalReached(float3 position)
        {
            return m_Data->IsGoalReached(position);
        }

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
        /// Converts cell to index.
        /// </summary>
        public int GetCellIndex(int2 cell) => m_Data->GetCellIndex(cell);

        /// <summary>
        /// Converts index to cell.
        /// </summary>
        public int2 GetCell(int index) => m_Data->GetCell(index);

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
            internal UnsafeCrowdFlow* m_Data;

            /// <summary>
            /// Adds a new world space goal that the crowd will attempt to reach. Must be called before <see cref="RecalculatePotentialField(CrowdWorld)"/>.
            /// </summary>
            public void AddGoal(float3 position)
            {
                m_Data->AddGoal(position);
            }
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWorld(CrowdWorld world)
        {
            if (world.Width != Width)
                throw new InvalidOperationException("Crowd world and surface width must match!");
            if (world.Height != Height)
                throw new InvalidOperationException("Crowd world and surface height must match!");
            if (any(world.Transform.Position != Transform.Position) || any(world.Transform.Rotation.value != Transform.Rotation.value) || any(world.Transform.Scale != Transform.Scale))
                throw new InvalidOperationException("Crowd world and surface Transform must match!");
        }
    }

    public unsafe struct UnsafeCrowdFlow : IDisposable
    {
        static class ProfilerMarkers
        {
            public static readonly ProfilerMarker SpeedAndCostField = new("SpeedAndCostField");
            public static readonly ProfilerMarker PotentialField = new("PotentialField");
        }

        const float Infinity = 10000f;

        int m_Width;
        int m_Height;
        NonUniformTransform m_Transform;

        NativeList<int2> m_GoalCells;
        NativeArray<float4> m_SpeedField;
        NativeArray<float4> m_UnitCostField;
        NativeArray<float> m_PotentialField;
        NativeArray<float> m_PartialPotentialField;

        CostWeights m_CostWeights;
        Speed m_Speed;
        float m_Radius;

        UnsafeHeap<float, int2>* m_Candidates;
        NativeArray<bool> m_KnownField;

        public int Length => m_Width * m_Height;
        public NativeArray<float4> SpeedField => m_SpeedField;
        public NativeArray<float> PotentialField => m_PotentialField;
        public NativeList<int2> GoalCells => m_GoalCells;
        public int Width => m_Width;
        public int Height => m_Height;
        public NonUniformTransform Transform => m_Transform;
        public CostWeights CostWeights { get => m_CostWeights; set => m_CostWeights = value; }
        public Speed Speed { get => m_Speed; set => m_Speed = value; }

        bool IsPotentialFieldComplete => m_Candidates->IsEmpty;

        public UnsafeCrowdFlow(int width, int height, NonUniformTransform transform, AllocatorManager.AllocatorHandle allocator)
        {
            m_Width = width;
            m_Height = height;
            m_Transform = transform;

            m_CostWeights = CostWeights.Default;
            m_Speed = Speed.Default;
            m_Radius = 1.0f;

            int length = m_Width * m_Height;
            m_GoalCells = new NativeList<int2>(length, allocator.ToAllocator);
            m_SpeedField = new NativeArray<float4>(length, allocator.ToAllocator);
            m_UnitCostField = new NativeArray<float4>(length, allocator.ToAllocator);
            m_PotentialField = new NativeArray<float>(length, allocator.ToAllocator);
            m_PartialPotentialField = new NativeArray<float>(length, allocator.ToAllocator);

            m_KnownField = new NativeArray<bool>(length, allocator.ToAllocator);
            m_Candidates = UnsafeHeap<float, int2>.Create(256, allocator);
        }

        public void ClearGoals()
        {
            GoalCells.Clear();
        }

        public void AddGoal(float2 point)
        {
            int2 cell = (int2)floor(point - 0.5f);
            GoalCells.Add(cell);
        }

        public void AddGoal(float3 position)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            int2 cell = (int2) floor(point - 0.5f);
            GoalCells.Add(cell);
        }

        public void AddGoal(float3 min, float3 max)
        {
            float2 minLS = m_Transform.InverseTransformPoint(min).xy;
            float2 maxLS = m_Transform.InverseTransformPoint(max).xy;
            int2 minCell = (int2) floor(minLS - 0.5f);
            int2 maxCell = (int2) floor(maxLS - 0.5f) + 1;
            for (int j = minCell.y; j < maxCell.y; j++)
            {
                for (int i = minCell.x; i < maxCell.x; i++)
                {
                    GoalCells.Add(new int2(i, j));
                }
            }
        }

        public void RecalculateSpeedAndCostField(UnsafeCrowdWorld* world)
        {
            if (!IsPotentialFieldComplete)
                return;

            ProfilerMarkers.SpeedAndCostField.Begin();
            for (int i = 0; i < m_Width; i++)
            {
                for (int j = 0; j < m_Height; j++)
                {
                    int2 cell = int2(i, j);
                    int2 cellE = cell + int2(1, 0);
                    int2 cellW = cell + int2(-1, 0);
                    int2 cellN = cell + int2(0, 1);
                    int2 cellS = cell + int2(0, -1);

                    int index = GetCellIndex(cell);
                    int indexE = GetCellIndex(cellE);
                    int indexW = GetCellIndex(cellW);
                    int indexN = GetCellIndex(cellN);
                    int indexS = GetCellIndex(cellS);

                    // Calculate flow speed
                    float4 flowSpeed;
                    flowSpeed.x = world->IsValidCell(cellE) ? dot(world->AverageVelocityField[indexE], float2(m_Radius, 0)) : 0;
                    flowSpeed.y = world->IsValidCell(cellW) ? dot(world->AverageVelocityField[indexW], float2(-m_Radius, 0)) : 0;
                    flowSpeed.z = world->IsValidCell(cellN) ? dot(world->AverageVelocityField[indexN], float2(0, m_Radius)) : 0;
                    flowSpeed.w = world->IsValidCell(cellS) ? dot(world->AverageVelocityField[indexS], float2(0, -m_Radius)) : 0;
                    flowSpeed = max(flowSpeed, 0.01f); // Avoid stop or negative speed

                    // Calculate topographical speed
                    float4 heightGradient = world->HeightGradientField[index];
                    float4 topographicalInterpolator = saturate(heightGradient - world->Slope.Min) / (world->Slope.Max - world->Slope.Min);
                    float4 topographicalSpeed = m_Speed.Max + topographicalInterpolator * (m_Speed.Min - m_Speed.Max);
                    //topographicalSpeed = m_MaxSpeed;

                    // Calculate final speed
                    float4 density;
                    density.x = world->IsValidCell(cellE) ? world->DensityField[indexE] : 0;
                    density.y = world->IsValidCell(cellW) ? world->DensityField[indexW] : 0;
                    density.z = world->IsValidCell(cellN) ? world->DensityField[indexN] : 0;
                    density.w = world->IsValidCell(cellS) ? world->DensityField[indexS] : 0;
                    float4 l = saturate(density - world->Density.Min) / (world->Density.Max - world->Density.Min);
                    float4 speed = topographicalSpeed + l * (flowSpeed - topographicalSpeed);

                    m_SpeedField[index] = speed;

                    // Calculate unit cost
                    float4 discomfort = m_CostWeights.Discomfort == 0 ? 0 : world->DiscomfortField[index];
                    float4 unitCost = (m_CostWeights.Distance * speed + m_CostWeights.Time + m_CostWeights.Discomfort * discomfort) / speed;

                    m_UnitCostField[index] = unitCost;
                }
            }
            ProfilerMarkers.SpeedAndCostField.End();
        }

        public void RecalculatePotentialField2(UnsafeCrowdWorld* world)
        {
            ProfilerMarkers.PotentialField.Begin();

            // Set as unknown
            for (int i = 0; i < Length; i++)
            {
                m_PartialPotentialField[i] = Infinity;
            }

            m_Candidates->Clear();

            for (int i = 0; i < m_GoalCells.Length; i++)
            {
                int2 cell = m_GoalCells[i];
                m_Candidates->Push(0, cell);
            }

            while (m_Candidates->TryPop(out var candidate))
            {
                int2 cell = candidate.Item2;

                //Debug.Log($"Pop {cell} {candidate.Item1}");

                // Skip known cells
                if (m_PartialPotentialField[GetCellIndex(cell)] != Infinity)
                    continue;

                // Update potential
                // Set as known
                m_PartialPotentialField[GetCellIndex(cell)] = candidate.Item1;

                int2 cellE = cell + int2(1, 0);
                int2 cellW = cell + int2(-1, 0);
                int2 cellN = cell + int2(0, 1);
                int2 cellS = cell + int2(0, -1);

                if (world->IsValidCell(cellE) && m_PartialPotentialField[GetCellIndex(cellE)] == Infinity)
                {
                    //Debug.Log($"New candidate {cellE}");
                    float potentialE = ApproximatePotential(world, cellE);
                    m_Candidates->Push(potentialE, cellE);
                }
                if (world->IsValidCell(cellW) && m_PartialPotentialField[GetCellIndex(cellW)] == Infinity)
                {
                    //Debug.Log($"New candidate {cellW}");
                    float potentialW = ApproximatePotential(world, cellW);
                    m_Candidates->Push(potentialW, cellW);
                }
                if (world->IsValidCell(cellN) && m_PartialPotentialField[GetCellIndex(cellN)] == Infinity)
                {
                    //Debug.Log($"New candidate {cellN}");
                    float potentialN = ApproximatePotential(world, cellN);
                    m_Candidates->Push(potentialN, cellN);
                }
                if (world->IsValidCell(cellS) && m_PartialPotentialField[GetCellIndex(cellS)] == Infinity)
                {
                    //Debug.Log($"New candidate {cellS}");
                    float potentialS = ApproximatePotential(world, cellS);
                    m_Candidates->Push(potentialS, cellS);
                }
            }
            ProfilerMarkers.PotentialField.End();
        }

        public void RecalculatePotentialField(UnsafeCrowdWorld* world, int iterations = 0)
        {
            ProfilerMarkers.PotentialField.Begin();

            if (iterations == 0)
                iterations = -1;

            bool* known = (bool*) m_KnownField.GetUnsafePtr();

            if (IsPotentialFieldComplete)
            {
                for (int i = 0; i < Length; i++)
                    m_PartialPotentialField[i] = Infinity;

                for (int i = 0; i < m_GoalCells.Length; i++)
                {
                    int2 cell = m_GoalCells[i];
                    if (!world->IsValidCell(cell))
                        continue;
                    m_Candidates->Push(0, cell);
                }

                for (int i = 0; i < Length; i++)
                    known[i] = false;
            }

            while (m_Candidates->TryPop(out var candidate) && iterations != 0)
            {
                int2 cell = candidate.Item2;

                int index = GetCellIndex(cell);

                //Debug.Log($"Pop {cell} {candidate.Item1}");

                // Skip known cells 
                if (known[index])
                    continue;

                // Update potential
                // Set as known
                m_PartialPotentialField[index] = candidate.Item1;

                known[index] = true;

                int2 cellE = cell + int2(1, 0);
                int2 cellW = cell + int2(-1, 0);
                int2 cellN = cell + int2(0, 1);
                int2 cellS = cell + int2(0, -1);

                int indexE = GetCellIndex(cellE);
                int indexW = GetCellIndex(cellW);
                int indexN = GetCellIndex(cellN);
                int indexS = GetCellIndex(cellS);

                if (!known[indexE] && world->IsValidCell(cellE))
                {
                    //Debug.Log($"New candidate {cellE}");
                    float potentialE = ApproximatePotential(world, cellE);
                    if (m_PartialPotentialField[indexE] > potentialE)
                    {
                        m_PartialPotentialField[indexE] = potentialE;
                        m_Candidates->Push(potentialE, cellE);
                    }
                }
                if (!known[indexW] && world->IsValidCell(cellW))
                {
                    //Debug.Log($"New candidate {cellW}");
                    float potentialW = ApproximatePotential(world, cellW);
                    if (m_PartialPotentialField[indexW] > potentialW)
                    {
                        m_PartialPotentialField[indexW] = potentialW;
                        m_Candidates->Push(potentialW, cellW);
                    }
                }
                if (!known[indexN] && world->IsValidCell(cellN))
                {
                    //Debug.Log($"New candidate {cellN}");
                    float potentialN = ApproximatePotential(world, cellN);
                    if (m_PartialPotentialField[indexN] > potentialN)
                    {
                        m_PartialPotentialField[indexN] = potentialN;
                        m_Candidates->Push(potentialN, cellN);
                    }
                }
                if (!known[indexS] && world->IsValidCell(cellS))
                {
                    //Debug.Log($"New candidate {cellS}");
                    float potentialS = ApproximatePotential(world, cellS);
                    if (m_PartialPotentialField[indexS] > potentialS)
                    {
                        m_PartialPotentialField[indexS] = potentialS;
                        m_Candidates->Push(potentialS, cellS);
                    }
                }

                iterations--;
            }

            // When partial potential field is complete swap it with previous potential field
            // This essentially double buffer potential field, where partial potential field acts as back buffer
            if (IsPotentialFieldComplete)
            {
                var temp = m_PotentialField;
                m_PotentialField = m_PartialPotentialField;
                m_PartialPotentialField = temp;
            }

            ProfilerMarkers.PotentialField.End();
        }

        public float3 SampleVelocity(float3 position)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            return m_Transform.TransformDirection(new float3(SampleVelocity(point), 0));
        }

        public float2 SampleVelocity(float2 point)
        {
            // Find closest cell center that coordinates are both less than of the point
            int2 cellA = (int2) floor(point - 0.5f);
            int2 cellB = cellA + int2(1, 0);
            int2 cellC = cellA + int2(0, 1);
            int2 cellD = cellA + int2(1, 1);

            float2 delta = saturate(point - ((float2) cellA + 0.5f));
            float2 oneMinusDelta = 1.0f - delta;

            float3 q11 = IsValidCell(cellA) ? float3(GetVelocity(cellA), 1) : 0;
            float3 q12 = IsValidCell(cellB) ? float3(GetVelocity(cellB), 1) : 0;
            float3 q21 = IsValidCell(cellC) ? float3(GetVelocity(cellC), 1) : 0;
            float3 q22 = IsValidCell(cellD) ? float3(GetVelocity(cellD), 1) : 0;

            float4 weights;
            weights.x = oneMinusDelta.x * oneMinusDelta.y;
            weights.y = oneMinusDelta.x * delta.y;
            weights.z = delta.x * oneMinusDelta.y;
            weights.w = delta.x * delta.y;

            float3 interpolated =
                q11 * weights.x +
                q21 * weights.y +
                q12 * weights.z +
                q22 * weights.w;

            if (interpolated.z == 0)
                return 0;

            return interpolated.xy / interpolated.z;
        }

        public float2 GetVelocity(int2 cell)
        {
            int index = GetCellIndex(cell);

            if (m_PotentialField[index] == 0)
                return 0;

            int2 cellE = cell + int2(1, 0);
            int2 cellW = cell + int2(-1, 0);
            int2 cellN = cell + int2(0, 1);
            int2 cellS = cell + int2(0, -1);

            float4 speed = m_SpeedField[index];

            // Calculate potential gradient
            float4 potential;
            potential.x = IsValidCell(cellE) ? m_PotentialField[GetCellIndex(cellE)] : Infinity;
            potential.y = IsValidCell(cellW) ? m_PotentialField[GetCellIndex(cellW)] : Infinity;
            potential.z = IsValidCell(cellN) ? m_PotentialField[GetCellIndex(cellN)] : Infinity;
            potential.w = IsValidCell(cellS) ? m_PotentialField[GetCellIndex(cellS)] : Infinity;
            float4 potentialGradientAnisotropic = select(0, potential - m_PotentialField[GetCellIndex(cell)], potential < Infinity);

            // Normalize potential gradient
            float2 potentialGradient = abs(new float2(potentialGradientAnisotropic.x - potentialGradientAnisotropic.y, potentialGradientAnisotropic.z - potentialGradientAnisotropic.w));
            float2 normalizedPotentialGradient = normalizesafe(potentialGradient);
            float2 divider = select(1.0f, normalizedPotentialGradient / potentialGradient, potentialGradient > 0.01f);
            //if (abs(divider.x - divider.y) > 0.001f && divider.x != 1f && divider.y != 1f)
            //    Debug.LogError($"Different dividers {divider}");
            potentialGradientAnisotropic.x *= divider.x;
            potentialGradientAnisotropic.y *= divider.x;
            potentialGradientAnisotropic.z *= divider.y;
            potentialGradientAnisotropic.w *= divider.y;

            float4 velocityAnisotropic = -speed * potentialGradientAnisotropic;

            float2 velocity = new float2(velocityAnisotropic.x - velocityAnisotropic.y, velocityAnisotropic.z - velocityAnisotropic.w);
            return velocity;
        }

        public bool IsGoalReached(float3 position)
        {
            float2 point = m_Transform.InverseTransformPoint(position).xy;
            return IsGoalReached(point);
        }

        public bool IsGoalReached(float2 point)
        {
            int2 cell = (int2) floor(point - 0.5f);
            if (!IsValidCell(cell))
                return false;
            // Goal cells always has zero potential
            return m_PotentialField[GetCellIndex(cell)] == 0;
        }

        static float Solve(float a, float b)
        {
            // Solves for x: ((x - a) / b)^2 = 1
            // https://www.wolframalpha.com/input?i=%28%28x+-+a%29+%2F+b%29%5E2+%2B+%28%28x+-+c%29+%2F+%28d%29%29%5E2+%3D+1

            float x0 = a - b;
            float x1 = a + b;

            return max(x0, x1);
        }

        static float Solve(float a, float b, float c, float d)
        {
            // Solves for x: ((x - a) / b)^2 + ((x - c) / d)^2 = 1
            // https://www.wolframalpha.com/input?i=%28%28x+-+a%29+%2F+b%29%5E2+%2B+%28%28x+-+c%29+%2F+%28d%29%29%5E2+%3D+1

            float a2 = a * a;
            float b2 = b * b;
            float c2 = c * c;
            float d2 = d * d;

            float j = -a2 + 2 * a * c + b2 - c2 + d2;

            if (j < 0)
            {
                if (a < c)
                {
                    //Debug.LogError("Droping dimenion as cant sqrt negative");
                    return Solve(a, b);
                }
                else
                {
                    //Debug.LogError("Droping dimenion as cant sqrt negative");
                    return Solve(c, d);
                }
            }

            float p0 = sqrt(j) / (b * d);
            float p1 = a / b2 + c / d2;
            float p2 = 1.0f / b2 + 1.0f / d2;

            float x0 = (-p0 + p1) / p2;
            float x1 = (p0 + p1) / p2;

            float r = max(x0, x1);

            // The solution can not be smaller than two provided by dimensions
            if (r >= a && r >= c)
            {
                return r;
            }
            else
            {
                if (a < c)
                {
                    //Debug.LogError("Droping dimenion as solution is bad");
                    return Solve(a, b);
                }
                else
                {
                    //Debug.LogError("Droping dimenion as solution is bad");
                    return Solve(c, d);
                }
            }
        }

        float ApproximatePotential(UnsafeCrowdWorld* world, int2 cell)
        {
            int2 cellE = cell + int2(1, 0);
            int2 cellW = cell + int2(-1, 0);
            int2 cellN = cell + int2(0, 1);
            int2 cellS = cell + int2(0, -1);

            float potentialE = world->IsValidCell(cellE) ? m_PartialPotentialField[GetCellIndex(cellE)] : Infinity;
            float potentialW = world->IsValidCell(cellW) ? m_PartialPotentialField[GetCellIndex(cellW)] : Infinity;
            float potentialN = world->IsValidCell(cellN) ? m_PartialPotentialField[GetCellIndex(cellN)] : Infinity;
            float potentialS = world->IsValidCell(cellS) ? m_PartialPotentialField[GetCellIndex(cellS)] : Infinity;

            float costE = m_UnitCostField[GetCellIndex(cell)].x;
            float costW = m_UnitCostField[GetCellIndex(cell)].y;
            float costN = m_UnitCostField[GetCellIndex(cell)].z;
            float costS = m_UnitCostField[GetCellIndex(cell)].w;

            float potentialX;
            float costX;
            if (potentialE + costE < potentialW + costW)
            {
                potentialX = potentialE;
                costX = costE;
            }
            else
            {
                potentialX = potentialW;
                costX = costW;
            }

            float potentialY;
            float costY;
            if (potentialN + costN < potentialS + costS)
            {
                potentialY = potentialN;
                costY = costN;
            }
            else
            {
                potentialY = potentialS;
                costY = costS;
            }

            if (potentialX >= Infinity && potentialY >= Infinity)
                throw new Exception();

            if (potentialX >= Infinity)
            {
                return Solve(potentialY, costY*costY);
            }
            else if (potentialY >= Infinity)
            {
                return Solve(potentialX, costX* costX);
            }
            else
            {
                return Solve(potentialX, costX* costX, potentialY, costY* costY);
            }
        }

        public void Dispose()
        {
            m_GoalCells.Dispose();
            m_SpeedField.Dispose();
            m_UnitCostField.Dispose();
            m_PotentialField.Dispose();
            m_PartialPotentialField.Dispose();
            m_KnownField.Dispose();
            UnsafeHeap<float, int2>.Destroy(m_Candidates);
        }

        bool IsValidCell(int2 cell) => cell.x >= 0 && cell.y >= 0 && cell.x < m_Width && cell.y < m_Height && m_PotentialField[GetCellIndex(cell)] != Infinity;

        public int GetCellIndex(int2 cell) => cell.y * m_Width + cell.x;
        public int2 GetCell(int index) => new int2(index % m_Width, index / m_Width);
    }
}
