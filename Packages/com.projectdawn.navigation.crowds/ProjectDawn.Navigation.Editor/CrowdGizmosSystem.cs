using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;
using UnityEngine;
using System;
using ProjectDawn.ContinuumCrowds;

namespace ProjectDawn.Navigation.Editor
{
    /// <summary>
    /// Tag for drawing gizmos.
    /// </summary>
    public struct CrowdSurfaceDrawGizmos : IComponentData
    {
        public Mode Value;

        public enum Mode
        {
            Density,
            Height,
            HeightGradient,
            AverageVelocity,
            Discomfort,
        }
    }

    /// <summary>
    /// Tag for drawing gizmos.
    /// </summary>
    public struct CrowdGroupDrawGizmos : IComponentData
    {
        public Mode Value;

        public enum Mode
        {
            Speed,
            Potential,
            Velocity,
            Goal,
        }
    }

    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    [UpdateAfter(typeof(CrowdFlowSystem))]
    public partial struct CrowdGizmosSystem : ISystem
    {
        ComponentLookup<CrowdSurfaceWorld> m_WorldLookup;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_WorldLookup = state.GetComponentLookup<CrowdSurfaceWorld>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_WorldLookup.Update(ref state);
            var gizmos = GetSingletonRW<GizmosSystem.Singleton>();
            new CrowdSurfaceGizmosJob
            {
                Gizmos = gizmos.ValueRW.CreateCommandBuffer(),
            }.Schedule();
            new CrowdGroupGizmosJob
            {
                Gizmos = gizmos.ValueRW.CreateCommandBuffer(),
                SurfaceLookup = m_WorldLookup,
            }.Schedule();
        }

        [BurstCompile]
        partial struct CrowdSurfaceGizmosJob : IJobEntity
        {
            public GizmosCommandBuffer Gizmos;

            public void Execute(in CrowdSurfaceDrawGizmos drawGizmos, in CrowdSurfaceWorld world)
            {
                Draw(Gizmos, world.World, drawGizmos.Value);
            }
        }

        [BurstCompile]
        partial struct CrowdGroupGizmosJob : IJobEntity
        {
            public GizmosCommandBuffer Gizmos;
            [ReadOnly]
            public ComponentLookup<CrowdSurfaceWorld> SurfaceLookup;

            public void Execute(in CrowdGroupDrawGizmos drawGizmos, in CrowdGroupFlow flow, in CrowdGroup group)
            {
                if (!SurfaceLookup.TryGetComponent(group.Surface, out CrowdSurfaceWorld surface))
                    return;
                Draw(Gizmos, surface.World, flow.Flow, drawGizmos.Value);
            }
        }

        static void Draw(GizmosCommandBuffer cmd, CrowdWorld world, CrowdSurfaceDrawGizmos.Mode mode)
        {
            NativeArray<Color> colors = new NativeArray<Color>(world.Length, Allocator.Temp);
            switch (mode)
            {
                case CrowdSurfaceDrawGizmos.Mode.Density:
                    var densityField = world.DensityField;
                    for (int i = 0; i < densityField.Length; i++)
                    {
                        float density = (densityField[i] - world.Density.Min) / (world.Density.Max - world.Density.Min);
                        colors[i] = new Color(density, density, density, 1);
                    }
                    break;

                case CrowdSurfaceDrawGizmos.Mode.Height:
                    {
                        var field = world.HeightField;
                        var fieldMin = GetMin(field);
                        var fieldMax = GetMax(field);
                        for (int i = 0; i < field.Length; i++)
                        {
                            float value = (field[i] - fieldMin) / (fieldMax - fieldMin);
                            colors[i] = new Color(value, value, value, 1);
                        }
                        break;
                    }

                case CrowdSurfaceDrawGizmos.Mode.HeightGradient:
                    {
                        var field = world.HeightGradientField;
                        for (int i = 0; i < field.Length; i++)
                        {
                            int2 cell = world.GetCell(i);
                            float4 value = field[i];
                            float2 direction = math.normalizesafe(new float2(value.x - value.y, value.z - value.w));
                            DrawCellArrow(cmd, world, cell, direction, Color.white);
                        }
                        break;
                    }

                case CrowdSurfaceDrawGizmos.Mode.AverageVelocity:
                    {
                        var field = world.AverageVelocityField;
                        for (int i = 0; i < field.Length; i++)
                        {
                            int2 cell = world.GetCell(i);

                            float2 value = field[i];
                            DrawCellArrow(cmd, world, cell, value, Color.white);
                        }
                        break;
                    }
                case CrowdSurfaceDrawGizmos.Mode.Discomfort:
                    {
                        var field = world.DiscomfortField;
                        var fieldMin = GetMin(field);
                        var fieldMax = GetMax(field);
                        for (int i = 0; i < field.Length; i++)
                        {
                            float value = (field[i] - fieldMin) / (fieldMax - fieldMin);
                            colors[i] = new Color(value, value, value, 1);
                        }
                        break;
                    }
            }
            cmd.DrawField(world.HeightField, world.ObstacleField, colors, world.Width, world.Height, world.Transform.ToMatrix(), Color.white);
            colors.Dispose();
        }

        static void Draw(GizmosCommandBuffer cmd, CrowdWorld world, CrowdFlow flow, CrowdGroupDrawGizmos.Mode mode)
        {
            NativeArray<Color> colors = new NativeArray<Color>(world.Length, Allocator.Temp);
            switch (mode)
            {
                case CrowdGroupDrawGizmos.Mode.Speed:
                    {
                        var field = flow.SpeedField;
                        var fieldMin = GetMin(field);
                        var fieldMax = GetMax(field);
                        for (int i = 0; i < field.Length; i++)
                        {
                            float4 value = (field[i] - fieldMin) / (fieldMax - fieldMin);
                            colors[i] = new Color(value.x, value.y, value.z, 1);
                        }
                        break;
                    }

                case CrowdGroupDrawGizmos.Mode.Potential:
                    {
                        var potentialField = flow.PotentialField;
                        float potentialMin = 0;
                        float potentialMax = 10;
                        for (int i = 0; i < potentialField.Length; i++)
                        {
                            float potential = (potentialField[i] - potentialMin) / (potentialMax - potentialMin);
                            colors[i] = new Color(potential, potential, potential, 1);
                        }
                        break;
                    }

                case CrowdGroupDrawGizmos.Mode.Velocity:
                    {
                        for (int y = 0; y < flow.Height; y++)
                        {
                            for (int x = 0; x < flow.Width; x++)
                            {
                                int2 cell = new int2(x, y);
                                float2 velocity = flow.GetVelocity(cell);
                                float2 direction = math.normalizesafe(velocity);
                                DrawCellArrow(cmd, world, cell, direction, Color.white);
                            }
                        }

                        break;
                    }

                case CrowdGroupDrawGizmos.Mode.Goal:
                    {
                        foreach (var goal in flow.GoalCells)
                        {
                            int2 cell = goal;
                            if (world.IsValidCell(cell))
                                colors[world.GetCellIndex(cell)] = Color.white;
                        }
                        break;
                    }
            }
            cmd.DrawField(world.HeightField, world.ObstacleField, colors, world.Width, world.Height, world.Transform.ToMatrix(), Color.white);
            colors.Dispose();
        }

        static void DrawCellArrow(GizmosCommandBuffer cmd, CrowdWorld world, int2 cell, float2 direction, Color color)
        {
            if (!world.IsValidCell(cell))
                return;

            float height = world.HeightField[world.GetCellIndex(cell)];

            float3 position = new float3(cell.x + 0.5f, cell.y + 0.5f, -height);
            float3 dir = new float3(direction, 0);
            float3 dirLeft = mul(Unity.Mathematics.quaternion.RotateZ(radians(-30.0f)), new float3(-direction, 0));
            float3 dirRight = mul(Unity.Mathematics.quaternion.RotateZ(radians(30.0f)), new float3(-direction, 0));

            var transform = world.Transform;
            position = transform.TransformPoint(position);
            dir = transform.TransformDirection(dir * transform.Scale * 0.5f);
            dirLeft = transform.TransformDirection(dirLeft * transform.Scale * 0.25f);
            dirRight = transform.TransformDirection(dirRight * transform.Scale * 0.25f);

            cmd.DrawLine(position, position + dir, color);
            cmd.DrawLine(position + dir, position + dir + dirLeft, color);
            cmd.DrawLine(position + dir, position + dir + dirRight, color);
        }

        static void DrawCellText(GizmosCommandBuffer cmd, CrowdWorld world,int2 cell, string text, Color color)
        {
            /*float height = HeightField[GetCellIndex(cell)];

            float3 position = new float3(cell.x + 0.5f, cell.y + 0.5f, -height);

            position = Transform.TransformPoint(position);

            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(position, text);*/
        }

        static void DrawCell(GizmosCommandBuffer cmd, CrowdWorld world, int2 cell, Color color)
        {
            if (!world.IsValidCell(cell))
                return;

            float height = world.HeightField[world.GetCellIndex(cell)];

            int2 cellA = cell;
            int2 cellB = cell + new int2(1, 0);
            int2 cellC = cell + new int2(1, 1);
            int2 cellD = cell + new int2(0, 1);

            var a = new float3(cellA, -(world.SampleHeight(cellA) + 0.05f));
            var b = new float3(cellB, -(world.SampleHeight(cellB) + 0.05f));
            var c = new float3(cellC, -(world.SampleHeight(cellC) + 0.05f));
            var d = new float3(cellD, -(world.SampleHeight(cellD) + 0.05f));

            var transform = world.Transform;
            a = transform.TransformPoint(a);
            b = transform.TransformPoint(b);
            c = transform.TransformPoint(c);
            d = transform.TransformPoint(d);

            cmd.DrawQuad(a, b, c, d, color, true);
        }

        static float GetMin(ReadOnlySpan<float> value)
        {
            float min = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i] < min)
                    min = value[i];
            }
            return min;
        }

        static float GetMax(ReadOnlySpan<float> value)
        {
            float max = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i] > max)
                    max = value[i];
            }
            return max;
        }

        static float4 GetMin(ReadOnlySpan<float4> value)
        {
            float4 min = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                min = math.min(min, value[i]);
            }
            return min;
        }

        static float4 GetMax(ReadOnlySpan<float4> value)
        {
            float4 max = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                max = math.max(max, value[i]);
            }
            return max;
        }

        static float2 GetMin(ReadOnlySpan<float2> value)
        {
            float2 min = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                min = math.min(min, value[i]);
            }
            return min;
        }

        static float2 GetMax(ReadOnlySpan<float2> value)
        {
            float2 max = value[0];
            for (int i = 1; i < value.Length; i++)
            {
                max = math.max(max, value[i]);
            }
            return max;
        }
    }
}
