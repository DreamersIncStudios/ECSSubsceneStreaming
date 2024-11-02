using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// Discomfort volume of crowd agents.
    /// </summary>
    [AddComponentMenu("Agents Navigation/Crowd/Crowd Discomfort")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/crowds/crowd-discomfort.html")]
    public class CrowdDiscomfortAuthoring : EntityBehaviour
    {
        [Tooltip("The shape of the discomfort.")]
        [SerializeField]
        protected CrowdDiscomfortType m_Type = CrowdDiscomfortType.Quad;

        [Tooltip("The size of the discomfort.")]
        [SerializeField]
        protected float3 m_Size = new(1, 1, 1);

        [Tooltip("The gradient of discomfort. The x-component represents the discomfort value at the center of the shape, and the y-component represents it at the edge.")]
        [SerializeField]
        protected float2 m_Gradient = new(1, 0);

        /// <summary>
        /// The shape of the discomfort.
        /// </summary>
        public CrowdDiscomfortType Type { get => m_Type; set => WriteDiscomfort(value); }

        /// <summary>
        /// The size of the discomfort.
        /// </summary>
        public float3 Size { get => m_Size; set => WriteDiscomfort(value); }

        /// <summary>
        /// The gradient of discomfort. The x-component represents the discomfort value at the center of the shape, and the y-component represents it at the edge.
        /// </summary>
        public float2 Gradient { get => m_Gradient; set => WriteDiscomfort(value); }

        /// <summary>
        /// Returns default component of <see cref="CrowdDiscomfort"/>.
        /// </summary>
        public CrowdDiscomfort DefaultDiscomfort => new()
        {
            Type = m_Type,
            Size = m_Size,
            Gradient = m_Gradient,
        };

        /// <summary>
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public CrowdDiscomfort EntityDiscomfort
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<CrowdDiscomfort>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        }

        /// <summary>
        /// Returns true if <see cref="EntityBehaviour"/> entity has <see cref="CrowdDiscomfort"/>.
        /// </summary>
        public bool HasEntityDiscomfort => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<CrowdDiscomfort>(m_Entity);

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, Agent.Default);
            world.EntityManager.AddComponentData(m_Entity, DefaultDiscomfort);
            world.EntityManager.AddComponentData(m_Entity, new LocalToWorld { Value = float4x4(transform.rotation, transform.position) });

            if (!gameObject.isStatic)
                world.EntityManager.AddComponentData(m_Entity, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, 1));

            // Transform access requires this
            world.EntityManager.AddComponentObject(m_Entity, transform);
        }

        void OnDrawGizmos()
        {
            switch (m_Type)
            {
                case CrowdDiscomfortType.Quad:
                    Gizmos.color = new Color(1, 0, 1, 1);
                    Gizmos.DrawWireCube(transform.position, m_Size);
                    break;
                case CrowdDiscomfortType.Circle:
                    float radius = 0.5f * max(m_Size.x, max(m_Size.y, m_Size.z));
                    Gizmos.color = new Color(1, 0, 1, 1);
                    Gizmos.DrawWireSphere(transform.position, radius);
                    break;
            }
        }


        T WriteDiscomfort<T>(in T value)
        {
            if (m_Entity == Entity.Null)
                return value;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return value;
            world.EntityManager.SetComponentData(m_Entity, DefaultDiscomfort);
            return value;
        }
    }

    internal class CrowdDiscomfortBaker : Baker<CrowdDiscomfortAuthoring>
    {
        public override void Bake(CrowdDiscomfortAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.DefaultDiscomfort);
        }
    }
}
