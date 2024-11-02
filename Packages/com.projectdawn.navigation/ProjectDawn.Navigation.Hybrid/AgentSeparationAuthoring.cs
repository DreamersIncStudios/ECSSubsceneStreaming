using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// Agent separation from nearby agents.
    /// </summary>
    [RequireComponent(typeof(AgentAuthoring))]
    [AddComponentMenu("Agents Navigation/Agent Separation")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/game-objects/avoidance/separation.html")]
    public class AgentSeparationAuthoring : MonoBehaviour
    {
        [SerializeField]
        protected float Radius = 3;

        [SerializeField, Range(0f, 2f)]
        protected float Weight = 1;

        [SerializeField]
        protected NavigationLayers m_Layers = NavigationLayers.Everything;

        Entity m_Entity;

        [System.Obsolete("Please use DefaultSeparation")]
        public AgentSeparation DefaulSeparation => DefaultSeparation;

        /// <summary>
        /// Returns default component of <see cref="AgentSeparation"/>.
        /// </summary>
        public AgentSeparation DefaultSeparation => new AgentSeparation
        {
            Radius = Radius,
            Weight = Weight,
            Layers = m_Layers,
        };

        /// <summary>
        /// <see cref="AgentSeparation"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public AgentSeparation EntitySeparation
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AgentSeparation>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        }

        /// <summary>
        /// Returns true if <see cref="AgentAuthoring"/> entity has <see cref="AgentSeparation"/>.
        /// </summary>
        public bool HasEntitySeparation => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<AgentSeparation>(m_Entity);

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, DefaultSeparation);

            // Sync in case it was created as disabled
            if (!enabled)
                world.EntityManager.SetComponentEnabled<AgentSeparation>(m_Entity, false);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                world.EntityManager.RemoveComponent<AgentSeparation>(m_Entity);
        }

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentSeparation>(m_Entity, true);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentSeparation>(m_Entity, false);
        }
    }

    internal class AgentSeparationBaker : Baker<AgentSeparationAuthoring>
    {
        public override void Bake(AgentSeparationAuthoring authoring) => AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.DefaultSeparation);
    }
}
