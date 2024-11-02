using UnityEngine;
using Unity.Entities;

namespace ProjectDawn.Navigation.Sample.Crowd
{
    public struct Expire : IComponentData
    {
    }

    public class ExpireAuthoring : MonoBehaviour
    {
    }

    public class ExpireBaker : Baker<ExpireAuthoring>
    {
        public override void Bake(ExpireAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Expire
            {
            });
        }
    }
}
