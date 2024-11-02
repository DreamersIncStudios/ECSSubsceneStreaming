using Unity.Collections;
using Unity.Entities;
using ProjectDawn.ContinuumCrowds;
using Unity.Burst;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst.Intrinsics;

namespace ProjectDawn.Navigation
{
    [UpdateAfter(typeof(CrowdFlowSystem))]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct CrowdSteeringSystem : ISystem
    {
        ComponentLookup<CrowdGroupFlow> m_GroupLookup;
        SharedComponentTypeHandle<AgentCrowdPath> m_AgentCrowdPathHandle;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_GroupLookup = state.GetComponentLookup<CrowdGroupFlow>(isReadOnly: true);
            m_AgentCrowdPathHandle = state.GetSharedComponentTypeHandle<AgentCrowdPath>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            m_GroupLookup.Update(ref state);
            m_AgentCrowdPathHandle.Update(ref state);

            new CrowdSteeringJob
            {
                GroupLookup = m_GroupLookup,
                AgentCrowdPathHandle = m_AgentCrowdPathHandle,
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct CrowdSteeringJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public ComponentLookup<CrowdGroupFlow> GroupLookup;
            public SharedComponentTypeHandle<AgentCrowdPath> AgentCrowdPathHandle;

            CrowdFlow m_Flow;

            public void Execute(ref AgentBody body, in AgentCrowdPath path, in LocalTransform transform)
            {
                if (!m_Flow.IsCreated)
                    throw new System.InvalidOperationException();

                if (m_Flow.IsGoalReached(transform.Position))
                    return;

                float3 velocity = m_Flow.SampleVelocity(transform.Position);
                body.Force = velocity / m_Flow.Speed.Max;
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var groupEntity = chunk.GetSharedComponent(AgentCrowdPathHandle).Group;
                if (!GroupLookup.TryGetComponent(groupEntity, out CrowdGroupFlow group))
                    return false;

                m_Flow = group.Flow;
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}
