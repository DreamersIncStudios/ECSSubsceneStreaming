using DreamersInc.QuadrantSystems;
using MotionSystem.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DreamersStudio.SubSceneStream
{

    partial struct LargeStreamingSystem : ISystem
    {
        #region Settings
        private StreamingSettings test; 
        private NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
        private const int quadrantYMultiplier = 1000;
        private const int quadrantCellSize = 300;
        public EntityQuery query;
        
        #endregion
       
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StreamingSettings>();
            if (SystemAPI.TryGetSingleton<StreamingSettings>(out var settings))
            {
                Debug.Log("got it");
            }

            query = state.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[] { ComponentType.ReadWrite(typeof(LocalTransform)),
                    ComponentType.ReadWrite(typeof(Animator))},
                Any = new ComponentType[] { ComponentType.ReadWrite(typeof(CharControllerE)), ComponentType.ReadWrite(typeof(BeastControllerComponent)) }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            if (query.CalculateEntityCount() == quadrantMultiHashMap.Capacity) return;
            quadrantMultiHashMap.Clear();
            quadrantMultiHashMap.Capacity = query.CalculateEntityCount();

            new SetQuadrantDataHashMapJob() { quadrantMap = quadrantMultiHashMap.AsParallelWriter() }
                .ScheduleParallel(query);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

            quadrantMultiHashMap.Dispose();
        }
        
        public static int GetPositionHashMapKey(float3 position)
        {
            return (int)(Mathf.Floor(position.x / quadrantCellSize) +
                         (quadrantYMultiplier * Mathf.Floor(position.y / quadrantCellSize)));
        }
        
        [BurstCompile]
        partial struct SetQuadrantDataHashMapJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMap;

            public void Execute(Entity entity, [ReadOnly] in LocalTransform transform)
            {
                int hashMapKey = GetPositionHashMapKey(transform.Position);
                quadrantMap.Add(hashMapKey, new QuadrantData
                {
                    entity = entity,
                    position = transform.Position
                });
            }
        }
    }
}