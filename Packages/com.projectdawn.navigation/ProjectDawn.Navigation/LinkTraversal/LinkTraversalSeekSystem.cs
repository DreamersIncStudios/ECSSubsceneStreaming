using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ProjectDawn.Navigation
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct LinkTraversalSeekSystem : ISystem
    {
        ComponentLookup<LinkTraversal> m_OnLinkTraversalLookup;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_OnLinkTraversalLookup = state.GetComponentLookup<LinkTraversal>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            m_OnLinkTraversalLookup.Update(ref state);
            new SeekLinkTraversalJob
            {
                OnLinkTraversalLookup = m_OnLinkTraversalLookup,
            }.ScheduleParallel();

            new StopLinkTraversalJob
            {
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(LinkTraversal))]
        unsafe partial struct SeekLinkTraversalJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LinkTraversal> OnLinkTraversalLookup;

            public void Execute(Entity entity, ref AgentBody body, in LocalTransform transform, in LinkTraversalSeek seekLinkTraversal)
            {
                float3 destination = seekLinkTraversal.End.GetClosestPortalPoint(transform.Position);
                float distance = math.distancesq(destination, transform.Position);

                if (distance < 0.01f)
                {
                    OnLinkTraversalLookup.SetComponentEnabled(entity, false);
                }
                else
                {
                    body.Force = math.normalizesafe(destination - transform.Position);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(LinkTraversal))]
        [WithNone(typeof(LinkTraversalSeek))]
        unsafe partial struct StopLinkTraversalJob : IJobEntity
        {
            public void Execute(ref AgentBody body, in LocalTransform transform)
            {
                body.Force = float3.zero;
            }
        }
    }
}
