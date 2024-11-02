#define CROWD_WORLD_PARALLEL

using Unity.Collections;
using Unity.Entities;
using ProjectDawn.ContinuumCrowds;
using Unity.Burst;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation
{
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct CrowdWorldSystem : ISystem
    {
        NativeList<CrowdWorld> m_Worlds;
        NativeList<CrowdWorld.ParallelWriter> m_WorldParallelWriters;
        NativeList<NavigationLayers> m_Layers;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_Worlds = new NativeList<CrowdWorld>(Allocator.Persistent);
            m_WorldParallelWriters = new NativeList<CrowdWorld.ParallelWriter>(Allocator.Persistent);
            m_Layers = new NativeList<NavigationLayers>(Allocator.Persistent);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            m_Worlds.Dispose();
            m_WorldParallelWriters.Dispose();
            m_Layers.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency = new ClearDensityJob
            {
                Worlds = m_Worlds,
                WorldParallelWriters = m_WorldParallelWriters,
                Layers = m_Layers,
            }.Schedule(state.Dependency);

#if CROWD_WORLD_PARALLEL
            // Schedule discomfort jobs
            var ecb = GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var discomfortJobHandle = new SplatterDiscomfortParallelJob
            {
                Worlds = m_WorldParallelWriters,
            }.ScheduleParallel(state.Dependency);
            discomfortJobHandle = new CreateDiscomfortJob
            {
                Worlds = m_WorldParallelWriters,
                Ecb = ecb,
            }.ScheduleParallel(discomfortJobHandle);
            discomfortJobHandle = new RemoveDiscomfortJob
            {
                Worlds = m_WorldParallelWriters,
                Ecb = ecb,
            }.ScheduleParallel(discomfortJobHandle);

            // Schedule obstacle jobs
            var ecb2 = GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var obstacleJobHandle = new SplatterObstacleParallelJob
            {
                Worlds = m_WorldParallelWriters,
            }.ScheduleParallel(state.Dependency);
            obstacleJobHandle = new CreateObstacleJob
            {
                Worlds = m_WorldParallelWriters,
                Ecb = ecb2,
            }.ScheduleParallel(obstacleJobHandle);
            obstacleJobHandle = new RemoveObstacleJob
            {
                Worlds = m_WorldParallelWriters,
                Ecb = ecb2,
            }.ScheduleParallel(obstacleJobHandle);

            // Schedule density job
            var densityJobHandle = new SplatterDensityParallelJob
            {
                Worlds = m_WorldParallelWriters,
                Layers = m_Layers,
            }.ScheduleParallel(state.Dependency);

            // Combine them
            state.Dependency = JobHandle.CombineDependencies(obstacleJobHandle, discomfortJobHandle, densityJobHandle);
#else
            var discomfortJobHandle = new SplatterDiscomfortJob
            {
                Worlds = m_Worlds,
                Layers = m_Layers,
            }.Schedule(state.Dependency);
            var densityJobHandle = new SplatterDensityJob
            {
                Worlds = m_Worlds,
                Layers = m_Layers,
            }.Schedule(state.Dependency);
            state.Dependency = JobHandle.CombineDependencies(discomfortJobHandle, densityJobHandle);
#endif

            state.Dependency = new NormalizeDensityJob
            {
                Worlds = m_Worlds,
                WorldParallelWriters = m_WorldParallelWriters,
                Layers = m_Layers,
            }.Schedule(state.Dependency);
        }

        #region Obstacle
        [BurstCompile]
        [WithNone(typeof(CrowdObstacleSplat))]
        partial struct CreateObstacleJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(Entity entity, in CrowdObstacle obstacle, in LocalToWorld transform)
            {
                switch (obstacle.Type)
                {
                    case CrowdObstacleType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleQuad(transform.Position, obstacle.Size, 1);
                        }
                        break;
                    case CrowdObstacleType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleCircle(transform.Position, obstacle.Size, 1);
                        }
                        break;
                }

                Ecb.AddComponent(0, entity, new CrowdObstacleSplat
                {
                    Position = transform.Position,
                    Type = obstacle.Type,
                    Size = obstacle.Size,
                });
            }
        }

        [BurstCompile]
        [WithNone(typeof(CrowdObstacle))]
        partial struct RemoveObstacleJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(Entity entity, in CrowdObstacleSplat obstacle)
            {
                switch (obstacle.Type)
                {
                    case CrowdObstacleType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleQuad(obstacle.Position, obstacle.Size, -1);
                        }
                        break;
                    case CrowdObstacleType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleCircle(obstacle.Position, obstacle.Size, -1);
                        }
                        break;
                }

                Ecb.RemoveComponent<CrowdObstacleSplat>(0, entity);
            }
        }

        [BurstCompile]
        partial struct SplatterObstacleParallelJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public void Execute(ref CrowdObstacleSplat splat, in CrowdObstacle obstacle, in LocalTransform transform)
            {
                var newSplat = new CrowdObstacleSplat
                {
                    Position = transform.Position,
                    Type = obstacle.Type,
                    Size = obstacle.Size,
                };

                if (newSplat == splat)
                    return;

                switch (obstacle.Type)
                {
                    case CrowdObstacleType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleQuad(splat.Position, splat.Size, -1);
                            Worlds[i].SplatObstacleQuad(transform.Position, obstacle.Size, 1);
                        }
                        break;
                    case CrowdObstacleType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatObstacleCircle(splat.Position, splat.Size, -1);
                            Worlds[i].SplatObstacleCircle(transform.Position, obstacle.Size, 1);
                        }
                        break;
                }
                splat = newSplat;
            }
        }
        #endregion

        #region Discomfort
        [BurstCompile]
        [WithNone(typeof(CrowdDiscomfortSplat))]
        partial struct CreateDiscomfortJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(Entity entity, in CrowdDiscomfort discomfort, in LocalToWorld transform)
            {
                switch (discomfort.Type)
                {
                    case CrowdDiscomfortType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortQuad(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                    case CrowdDiscomfortType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortCircle(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                }

                Ecb.AddComponent(0, entity, new CrowdDiscomfortSplat
                {
                    Position = transform.Position,
                    Type = discomfort.Type,
                    Size = discomfort.Size,
                    Gradient = discomfort.Gradient,
                });
            }
        }

        [BurstCompile]
        [WithNone(typeof(CrowdDiscomfort))]
        partial struct RemoveDiscomfortJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(Entity entity, in CrowdDiscomfortSplat discomfort)
            {
                switch (discomfort.Type)
                {
                    case CrowdDiscomfortType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortQuad(discomfort.Position, discomfort.Size, -discomfort.Gradient);
                        }
                        break;
                    case CrowdDiscomfortType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortCircle(discomfort.Position, discomfort.Size, -discomfort.Gradient);
                        }
                        break;
                }

                Ecb.RemoveComponent<CrowdDiscomfortSplat>(0, entity);
            }
        }

        [BurstCompile]
        partial struct SplatterDiscomfortJob : IJobEntity
        {
            public NativeList<CrowdWorld> Worlds;
            [ReadOnly]
            public NativeList<NavigationLayers> Layers;
            public void Execute(in Agent agent, in CrowdDiscomfort discomfort, in LocalTransform transform)
            {
                switch (discomfort.Type)
                {
                    case CrowdDiscomfortType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            if (!Layers[i].Any(agent.Layers))
                                continue;
                            Worlds[i].SplatDiscomfortQuad(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                    case CrowdDiscomfortType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            if (!Layers[i].Any(agent.Layers))
                                continue;
                            Worlds[i].SplatDiscomfortCircle(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                }
            }
        }

        [BurstCompile]
        partial struct SplatterDiscomfortParallelJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;

            public void Execute(ref CrowdDiscomfortSplat splat, in CrowdDiscomfort discomfort, in LocalTransform transform)
            {
                var newSplat = new CrowdDiscomfortSplat
                {
                    Position = transform.Position,
                    Type = discomfort.Type,
                    Size = discomfort.Size,
                    Gradient = discomfort.Gradient,
                };

                if (newSplat == splat)
                    return;

                switch (discomfort.Type)
                {
                    case CrowdDiscomfortType.Quad:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortQuad(splat.Position, splat.Size, -splat.Gradient);
                            Worlds[i].SplatDiscomfortQuad(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                    case CrowdDiscomfortType.Circle:
                        for (int i = 0; i < Worlds.Length; i++)
                        {
                            Worlds[i].SplatDiscomfortCircle(splat.Position, splat.Size, -splat.Gradient);
                            Worlds[i].SplatDiscomfortCircle(transform.Position, discomfort.Size, discomfort.Gradient);
                        }
                        break;
                }
                splat = newSplat;
            }
        }
        #endregion

        #region Density
        [BurstCompile]
        partial struct ClearDensityJob : IJobEntity
        {
            public NativeList<CrowdWorld> Worlds;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> WorldParallelWriters;
            public NativeList<NavigationLayers> Layers;
            public void Execute(ref CrowdSurfaceWorld surfaceWorld, in CrowdSurface surface)
            {
                var world = surfaceWorld.World;
                world.ClearDensity();

                // Gather data
                Worlds.Add(world);
                WorldParallelWriters.Add(world.AsParallelWriter());
                Layers.Add(surface.Layers);

                // Update properties
                world.Density = surface.Density;
                world.Slope = surface.Slope;
            }
        }

        [BurstCompile]
        partial struct SplatterDensityJob : IJobEntity
        {
            public NativeList<CrowdWorld> Worlds;
            [ReadOnly]
            public NativeList<NavigationLayers> Layers;

            public void Execute(in Agent agent, in AgentBody body, in AgentShape shape, in LocalTransform transform)
            {
                for (int i = 0; i < Worlds.Length; i++)
                {
                    if (!Layers[i].Any(agent.Layers))
                        continue;
                    Worlds[i].SplatDensity(transform.Position, body.Velocity);
                }
            }
        }

        [BurstCompile]
        partial struct SplatterDensityParallelJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> Worlds;
            [ReadOnly]
            public NativeList<NavigationLayers> Layers;

            public void Execute(in Agent agent, in AgentBody body, in AgentShape shape, in LocalTransform transform)
            {
                for (int i = 0; i < Worlds.Length; i++)
                {
                    if (!Layers[i].Any(agent.Layers))
                        continue;
                    Worlds[i].SplatDensity(transform.Position, body.Velocity);
                }
            }
        }

        [BurstCompile]
        struct NormalizeDensityJob : IJob
        {
            public NativeList<CrowdWorld> Worlds;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<CrowdWorld.ParallelWriter> WorldParallelWriters;
            public NativeList<NavigationLayers> Layers;
            public void Execute()
            {
                foreach (var world in Worlds)
                {
                    world.NormalizeAverageVelocityField();
                }
                Worlds.Clear();
                WorldParallelWriters.Clear();
                Layers.Clear();
            }
        }
        #endregion
    }
}
