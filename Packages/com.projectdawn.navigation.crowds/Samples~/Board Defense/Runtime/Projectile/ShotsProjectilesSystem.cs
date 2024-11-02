using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Entities.SystemAPI;
using Unity.Collections;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [UpdateBefore(typeof(AgentSystemGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ShotProjectilesSystem : ISystem
    {
        ComponentLookup<LocalTransform> m_TransformLookup;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_TransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var spatial = GetSingleton<AgentSpatialPartitioningSystem.Singleton>();
            m_TransformLookup.Update(ref state);

            new ProjectileMoveJob
            {
                Spatial = spatial,
                TransformLookup = m_TransformLookup,
                Ecb = ecb,
                ElapsedTime = Time.ElapsedTime,
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ProjectileMoveJob : IJobEntity
        {
            [ReadOnly]
            public AgentSpatialPartitioningSystem.Singleton Spatial;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> TransformLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public double ElapsedTime;

            public void Execute(Entity entity, ref ShotsProjectiles shotsProjectiles)
            {
                var transform = TransformLookup[entity];

                if ((ElapsedTime - shotsProjectiles.ElapsedTime) < shotsProjectiles.Cooldown)
                    return;

                if (!TransformLookup.TryGetComponent(shotsProjectiles.Target, out var targetTransform))
                {
                    var action = new Action();
                    Spatial.QueryCylinder(transform.Position, shotsProjectiles.Radius, shotsProjectiles.Radius, 2, ref action);

                    shotsProjectiles.Target = action.Target;
                    targetTransform.Position = action.Position;

                    if (shotsProjectiles.Target == Entity.Null)
                        return;
                }

                var crossbowTransform = TransformLookup[shotsProjectiles.Crossbow];
                float3 facingDirection = normalize(targetTransform.Position - transform.Position);
                float angle = atan2(facingDirection.x, facingDirection.z) + radians(45);
                crossbowTransform.Rotation = Unity.Mathematics.quaternion.RotateY(angle);
                TransformLookup[shotsProjectiles.Crossbow] = crossbowTransform;

                var projectile = Ecb.Instantiate(0, shotsProjectiles.Projectile);
                Ecb.SetComponent(0, projectile, new ProjectileTarget { Entity = shotsProjectiles.Target, Position = targetTransform.Position});
                Ecb.SetComponent(0, projectile, transform.TransformTransform(shotsProjectiles.Start));
                shotsProjectiles.ElapsedTime = ElapsedTime;

                // It kills in one shot, lets switch target
                shotsProjectiles.Target = Entity.Null;
            }
        }

        struct Action : ISpatialQueryEntity
        {
            public Entity Target;
            public float3 Position;
            public void Execute(Entity entity, AgentBody body, AgentShape shape, LocalTransform transform)
            {
                if (entity == Target)
                    return;

                Target = entity;
                Position = transform.Position;
            }
        }
    }
}
