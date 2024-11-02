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
    [UpdateBefore(typeof(AgentSystemGroup))]
    public partial struct SpawnerSystem : ISystem
    {
        ComponentLookup<CrowdGroup> m_GroupLookup;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //m_GroupLookup.Update(ref state);
            var ecb = GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            new SpawnerJob
            {
                //GroupLookup = m_GroupLookup,
                Ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged),
                DeltaTime = state.WorldUnmanaged.Time.DeltaTime,
            }.Schedule();
        }

        [BurstCompile]
        partial struct SpawnerJob : IJobEntity
        {
            //[ReadOnly]
            //public ComponentLookup<CrowdGroup> GroupLookup;
            public EntityCommandBuffer Ecb;
            public float DeltaTime;

            public void Execute(ref Spawner spawner, in LocalTransform transform)
            {
                if (spawner.MaxCount == spawner.Count)
                    return;

                //if (!GroupLookup.TryGetComponent(spawner.Group, out CrowdGroup group))
                //    return;

                spawner.Elapsed += DeltaTime;
                if (spawner.Elapsed >= spawner.Interval)
                {
                    spawner.Elapsed -= spawner.Interval;

                    for (int i = 0; i < spawner.Batch; i++)
                    {
                        float3 offset = spawner.Random.NextFloat3(-spawner.Size, spawner.Size);
                        float3 position = transform.Position + offset;
                        Entity unit = Ecb.Instantiate(spawner.Prefab);
                        Ecb.SetComponent(unit, new LocalTransform { Position = position, Scale = 1, Rotation = quaternion.identity });
                        Ecb.SetComponent(unit, new AgentBody { Destination = spawner.Destination, IsStopped = false });
                        Ecb.SetSharedComponent(unit, new AgentCrowdPath { Group = spawner.Group });
                        spawner.Count++;
                    }
                }
            }
        }
    }
}
