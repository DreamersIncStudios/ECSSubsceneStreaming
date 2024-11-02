using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using ProjectDawn.Navigation.Hybrid;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    public struct Spawner : IComponentData
    {
        public Entity Group;
        public Entity Prefab;
        public float Interval;
        public int Batch;
        public float3 Size;
        public int Count;
        public int MaxCount;
        public float3 Destination;

        public Unity.Mathematics.Random Random;
        public float Elapsed;
    }

    public class SpawnerAuthoring : MonoBehaviour
    {
        public CrowdGroupAuthoring Group;
        public GameObject Prefab;
        public float Interval = 4;
        public int Batch = 40;
        public float3 Size = new float3(1, 0, 1);
        public int Count;
        public int MaxCount = 1000;
        public Transform Destination;
        public bool DestinationDeferred = true;
    }

    public class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Spawner
            {
                Group = GetEntity(authoring.Group, TransformUsageFlags.Dynamic),
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Interval = authoring.Interval,
                Batch = authoring.Batch,
                Size = authoring.Size,
                Count = authoring.Count,
                MaxCount = authoring.MaxCount,
                Destination = authoring.Destination.transform.position,
                Random = new Unity.Mathematics.Random(1),
            });
        }
    }
}
