using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// Obstacle volume of crowd agents.
    /// </summary>
    [AddComponentMenu("Agents Navigation/Crowd/Crowd Obstacle")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/crowds/crowd-obstacle.html")]
    public class CrowdObstacleAuthoring : EntityBehaviour
    {
        [Tooltip("The shape of the obstacle.")]
        [SerializeField]
        protected CrowdObstacleType m_Type = CrowdObstacleType.Quad;

        [Tooltip("The size of the obstacle.")]
        [SerializeField]
        protected float3 m_Size = new(1, 1, 1);

        /// <summary>
        /// The shape of the obstacle.
        /// </summary>
        public CrowdObstacleType Type { get => m_Type; set => WriteObstacle(value); }

        /// <summary>
        /// The size of the obstacle.
        /// </summary>
        public float3 Size { get => m_Size; set => WriteObstacle(value); }

        /// <summary>
        /// Returns default component of <see cref="CrowdObstacle"/>.
        /// </summary>
        public CrowdObstacle DefaultObstacle => new()
        {
            Type = m_Type,
            Size = m_Size,
        };

        /// <summary>
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public CrowdObstacle EntityObstacle
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<CrowdObstacle>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        }

        /// <summary>
        /// Returns true if <see cref="EntityBehaviour"/> entity has <see cref="CrowdObstacle"/>.
        /// </summary>
        public bool HasEntityObstacle => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<CrowdObstacle>(m_Entity);

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, Agent.Default);
            world.EntityManager.AddComponentData(m_Entity, DefaultObstacle);
            world.EntityManager.AddComponentData(m_Entity, new LocalToWorld { Value = float4x4(transform.rotation, transform.position) });

            if (!gameObject.isStatic)
                world.EntityManager.AddComponentData(m_Entity, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, 1));

            // Transform access requires this
            world.EntityManager.AddComponentObject(m_Entity, transform);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawCube(transform.position, m_Size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, m_Size);
        }

        T WriteObstacle<T>(in T value)
        {
            if (m_Entity == Entity.Null)
                return value;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return value;
            world.EntityManager.SetComponentData(m_Entity, DefaultObstacle);
            return value;
        }
    }

    internal class CrowdObstacleBaker : Baker<CrowdObstacleAuthoring>
    {
        public override void Bake(CrowdObstacleAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.DefaultObstacle);
        }
    }
}
