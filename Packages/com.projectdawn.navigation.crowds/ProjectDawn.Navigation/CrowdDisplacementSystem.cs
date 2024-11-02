using Unity.Collections;
using Unity.Entities;
using ProjectDawn.ContinuumCrowds;
using Unity.Burst;
using Unity.Transforms;
using Unity.Burst.Intrinsics;

namespace ProjectDawn.Navigation
{
    [UpdateInGroup(typeof(AgentDisplacementSystemGroup))]
    [UpdateAfter(typeof(AgentColliderSystem))]
    public partial struct CrowdDisplacementSystem : ISystem
    {
        ComponentLookup<CrowdGroup> m_GroupLookup;
        ComponentLookup<CrowdSurfaceWorld> m_WorldLooukup;
        SharedComponentTypeHandle<AgentCrowdPath> m_AgentCrowdPathHandle;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_GroupLookup = state.GetComponentLookup<CrowdGroup>(isReadOnly: true);
            m_WorldLooukup = state.GetComponentLookup<CrowdSurfaceWorld>(isReadOnly: true);
            m_AgentCrowdPathHandle = state.GetSharedComponentTypeHandle<AgentCrowdPath>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            m_GroupLookup.Update(ref state);
            m_WorldLooukup.Update(ref state);
            m_AgentCrowdPathHandle.Update(ref state);

            new CrowdDisplacementJob
            {
                GroupLookup = m_GroupLookup,
                WorldLookup = m_WorldLooukup,
                AgentCrowdPathHandle = m_AgentCrowdPathHandle,
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct CrowdDisplacementJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public ComponentLookup<CrowdGroup> GroupLookup;
            [ReadOnly]
            public ComponentLookup<CrowdSurfaceWorld> WorldLookup;
            public SharedComponentTypeHandle<AgentCrowdPath> AgentCrowdPathHandle;

            CrowdGroup m_Group;
            CrowdWorld m_World;

            public void Execute(ref LocalTransform transform, in AgentCrowdPath path)
            {
                if (!m_World.IsCreated)
                    throw new System.InvalidOperationException();

                if (m_Group.Grounded)
                    m_World.MapLocation(transform.Position, m_Group.MappingRadius, out transform.Position);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var groupEntity = chunk.GetSharedComponent(AgentCrowdPathHandle).Group;
                if (!GroupLookup.TryGetComponent(groupEntity, out CrowdGroup group))
                    return false;

                m_Group = group;

                if (!WorldLookup.TryGetComponent(group.Surface, out CrowdSurfaceWorld world))
                    return false;

                m_World = world.World;
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}
