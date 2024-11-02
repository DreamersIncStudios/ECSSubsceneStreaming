using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    public struct Projectile : IComponentData
    {
        public float Speed;
        public float Radius;
    }

    public struct ProjectileTarget : IComponentData
    {
        public Entity Entity;
        public float3 Position;
    }

    public class ProjectileAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal float m_Speed = 5f;
        [SerializeField]
        internal float m_Radius = 0.2f;
    }

    internal class ProjectileBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Projectile
            {
                Speed = authoring.m_Speed,
                Radius = authoring.m_Radius,
            });
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new ProjectileTarget
            {
            });
        }
    }
}
