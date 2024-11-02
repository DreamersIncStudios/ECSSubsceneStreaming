using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    public struct ShotsProjectiles : IComponentData
    {
        public Entity Projectile;
        public Entity Crossbow;
        public Entity Target;
        public LocalTransform Start;
        public float Cooldown;
        public float Radius;
        public double ElapsedTime;
    }

    public class ShotsProjectilesAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal ProjectileAuthoring m_Projectile;
        [SerializeField]
        internal Transform m_Crossbow;
        [SerializeField]
        internal Transform m_Start;
        [SerializeField]
        internal float m_Cooldown = 1.0f;
        [SerializeField]
        internal float m_Radius = 10.0f;

        internal class ShotsProjectileBaker : Baker<ShotsProjectilesAuthoring>
        {
            public override void Bake(ShotsProjectilesAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.Dynamic), new ShotsProjectiles
                {
                    Projectile = GetEntity(authoring.m_Projectile, TransformUsageFlags.Dynamic),
                    Crossbow = GetEntity(authoring.m_Crossbow, TransformUsageFlags.Dynamic),
                    Start = LocalTransform.FromMatrix(authoring.transform.localToWorldMatrix.inverse * authoring.m_Start.localToWorldMatrix),
                    Cooldown = authoring.m_Cooldown,
                    Radius = authoring.m_Radius,
                });
            }
        }
    }
}
