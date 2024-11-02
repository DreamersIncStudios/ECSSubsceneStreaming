using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// Agent uses Continuum Crowd for pathfinding in specific <see cref="CrowdGroupAuthoring"/>.
    /// </summary>
    [RequireComponent(typeof(AgentAuthoring))]
    [AddComponentMenu("Agents Navigation/Crowd/Agent Crowd Pathing")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/game-objects/pathing/crowds.html")]
    public class AgentCrowdPathingAuthoring : MonoBehaviour
    {
        [Tooltip("Current crowds group this agent belongs too. Groups always share same destination. Agents in null crowds group will skip pathing.")]
        [SerializeField]
        internal CrowdGroupAuthoring m_Group;

        Entity m_Entity;

        /// <summary>
        /// Current crowds group this agent belongs too.
        /// Groups always share same destination.
        /// Agents in null crowds group will skip pathing.
        /// </summary>
        public CrowdGroupAuthoring Group
        {
            get => m_Group;
            set
            {
                m_Group = value;
                if (m_Entity == Entity.Null)
                    return;
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                world.EntityManager.SetSharedComponent(m_Entity, DefaultPath);
            }
        }

        /// <summary>
        /// Returns default component of <see cref="AgentSonarAvoid"/>.
        /// </summary>
        public AgentCrowdPath DefaultPath => new()
        {
            Group = m_Group ? m_Group.GetOrCreateEntity() : Entity.Null,
        };

        /// <summary>
        /// <see cref="AgentCrowdPath"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public AgentCrowdPath EntityPath
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetSharedComponent<AgentCrowdPath>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetSharedComponent(m_Entity, value);
        }

        /// <summary>
        /// Returns true if <see cref="AgentAuthoring"/> entity has <see cref="AgentCrowdPath"/>.
        /// </summary>
        public bool HasEntityPath => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<AgentCrowdPath>(m_Entity);

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
            world.EntityManager.AddSharedComponent(m_Entity, DefaultPath);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.RemoveComponent<AgentCrowdPath>(m_Entity);
        }
    }

    internal class AgentCrowdPathingBaker : Baker<AgentCrowdPathingAuthoring>
    {
        public override void Bake(AgentCrowdPathingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddSharedComponent(entity, new AgentCrowdPath { Group = GetEntity(authoring.m_Group, TransformUsageFlags.None) });
        }
    }
}
