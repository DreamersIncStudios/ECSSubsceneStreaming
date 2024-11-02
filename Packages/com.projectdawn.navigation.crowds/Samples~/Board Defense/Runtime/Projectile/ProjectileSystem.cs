using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [UpdateBefore(typeof(AgentSystemGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ProjectileSystem : ISystem
    {
        ComponentLookup<LocalTransform> m_TransformLookup;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_TransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly:false);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            m_TransformLookup.Update(ref state);

            new ProjectileMoveJob
            {
                Ecb = ecb,
                TransformLookup = m_TransformLookup,
                DeltaTime = Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ProjectileMoveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> TransformLookup;
            public float DeltaTime;

            public void Execute(Entity entity, ref ProjectileTarget target, in Projectile projectile)
            {
                if (!TransformLookup.TryGetComponent(entity, out LocalTransform transform))
                    return;

                if (TransformLookup.TryGetComponent(target.Entity, out LocalTransform targetTransform))
                {
                    target.Position = targetTransform.Position;
                }

                float3 towards = target.Position - transform.Position;
                float distance = length(towards);

                if (distance < projectile.Radius)
                {
                    Ecb.DestroyEntity(0, entity);
                    if (TransformLookup.HasComponent(target.Entity))
                        Ecb.DestroyEntity(0, target.Entity);
                    return;
                }

                float3 direction = towards / distance;
                transform.Position += direction * DeltaTime * projectile.Speed;
                transform.Rotation = Unity.Mathematics.quaternion.LookRotation(direction, float3(0, 1, 0));
                TransformLookup[entity] = transform;
            }
        }
    }
}
