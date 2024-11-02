using Unity.Collections;
using Unity.Entities;
using ProjectDawn.ContinuumCrowds;
using Unity.Burst;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.Navigation
{
    [System.Serializable]
    class CrowdSubSettings : ISubSettings
    {
        [SerializeField]
        [Tooltip("The maximum number of iterations the agent will use per frame. A langer number results in faster path finding, but it also incurs a greater performance cost.")]
        int m_IterationsPerFrame = 1024;

        /// <summary>
        /// The maximum number of iterations the agent will use per frame. A langer number results in faster path finding, but it also incurs a greater performance cost.
        /// </summary>
        public int IterationsPerFrame => m_IterationsPerFrame;
    }

    [UpdateAfter(typeof(CrowdWorldSystem))]
    [UpdateAfter(typeof(CrowdGoalSystem))]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct CrowdFlowSystem : ISystem
    {
        EntityQuery m_Query;
        ComponentLookup<CrowdSurfaceWorld> m_WorldLookup;
        int m_Iterations;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = state.GetEntityQuery(ComponentType.ReadOnly<CrowdGroup>(), ComponentType.ReadOnly<CrowdGroupFlow>());
            m_WorldLookup = state.GetComponentLookup<CrowdSurfaceWorld>();
            m_Iterations = AgentsNavigationSettings.Get<CrowdSubSettings>().IterationsPerFrame;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            m_WorldLookup.Update(ref state);

            int count = m_Query.CalculateEntityCount();
            var flows = new NativeArray<CrowdFlow>(count, Allocator.TempJob);
            var worlds = new NativeArray<CrowdWorld>(count, Allocator.TempJob);

            state.Dependency = new CollectLayerJob
            {
                Flows = flows,
                Worlds = worlds,
                WorldLookup = m_WorldLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new UpdateLayerJob
            {
                Flows = flows,
                Worlds = worlds,
                Iterations = m_Iterations,
            }.Schedule(count, 1, state.Dependency);
            state.Dependency = JobHandle.CombineDependencies(flows.Dispose(state.Dependency), worlds.Dispose(state.Dependency));
        }

        [BurstCompile]
        partial struct CollectLayerJob : IJobEntity
        {
            public NativeArray<CrowdFlow> Flows;
            public NativeArray<CrowdWorld> Worlds;
            [ReadOnly]
            public ComponentLookup<CrowdSurfaceWorld> WorldLookup;
            public void Execute([EntityIndexInQuery] int entityInQueryIndex, ref CrowdGroupFlow groupFlow, in CrowdGroup group)
            {
                if (!WorldLookup.TryGetComponent(group.Surface, out CrowdSurfaceWorld world))
                    return;

                var flow = groupFlow.Flow;
                flow.CostWeights = group.CostWeights;
                flow.Speed = group.Speed;
                Flows[entityInQueryIndex] = flow;
                Worlds[entityInQueryIndex] = world.World;
            }
        }

        [BurstCompile]
        struct UpdateLayerJob : IJobParallelFor
        {
            public NativeArray<CrowdFlow> Flows;
            public NativeArray<CrowdWorld> Worlds;
            public int Iterations;

            void IJobParallelFor.Execute(int index)
            {
                var flow = Flows[index];
                var world = Worlds[index];
                flow.RecalculateSpeedAndCostField(world);
                flow.RecalculatePotentialField(world, Iterations);
            }
        }
    }
}
