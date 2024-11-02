using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using ProjectDawn.ContinuumCrowds;
using Unity.Burst.Intrinsics;

namespace ProjectDawn.Navigation
{
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct CrowdGoalSystem : ISystem
    {
        ComponentLookup<CrowdGroupFlow> m_FlowLookup;
        ComponentLookup<CrowdGroup> m_GroupLookup;
        SharedComponentTypeHandle<AgentCrowdPath> m_AgentCrowdPathHandle;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_FlowLookup = state.GetComponentLookup<CrowdGroupFlow>(isReadOnly: false);
            m_GroupLookup = state.GetComponentLookup<CrowdGroup>(isReadOnly: false);
            m_AgentCrowdPathHandle = state.GetSharedComponentTypeHandle<AgentCrowdPath>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            m_FlowLookup.Update(ref state);
            m_GroupLookup.Update(ref state);
            m_AgentCrowdPathHandle.Update(ref state);

            state.Dependency = new ClearGoalJob().ScheduleParallel(state.Dependency);
            state.Dependency = new UpdateGoalJob
            {
                FlowLookup = m_FlowLookup,
                GroupLookup = m_GroupLookup,
                AgentCrowdPathHandle = m_AgentCrowdPathHandle,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        partial struct ClearGoalJob : IJobEntity
        {
            public void Execute(ref CrowdGroupFlow flow, in CrowdGroup group)
            {
                if (group.GoalSource != CrowdGoalSource.AgentDestination)
                    return;

                flow.Flow.ClearGoals();
            }
        }

        [BurstCompile]
        partial struct UpdateGoalJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public ComponentLookup<CrowdGroupFlow> FlowLookup;
            [ReadOnly]
            public ComponentLookup<CrowdGroup> GroupLookup;
            public SharedComponentTypeHandle<AgentCrowdPath> AgentCrowdPathHandle;

            [NativeDisableParallelForRestriction]
            CrowdFlow m_Flow;

            public void Execute(ref AgentBody body, in AgentCrowdPath path)
            {
                if (!m_Flow.IsCreated)
                    throw new System.InvalidOperationException();

                m_Flow.AddGoal(body.Destination);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var groupEntity = chunk.GetSharedComponent(AgentCrowdPathHandle).Group;
                if (!FlowLookup.TryGetComponent(groupEntity, out CrowdGroupFlow flow))
                    return false;

                if (!GroupLookup.TryGetComponent(groupEntity, out CrowdGroup group))
                    return false;

                m_Flow = flow.Flow;
                return group.GoalSource == CrowdGoalSource.AgentDestination;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}
