using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ProjectDawn.Navigation;
using static Unity.Entities.SystemAPI;
using Unity.Collections;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(AgentSystemGroup))]
    public partial struct ExpireSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            new ExpireJob
            {
                Ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged),
            }.Schedule();
        }

        [WithAll(typeof(Expire))]
        [BurstCompile]
        partial struct ExpireJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            public void Execute(Entity entity, in AgentBody body)
            {
                if (!body.IsStopped)
                    return;
                Ecb.DestroyEntity(entity);
            }
        }
    }
}
