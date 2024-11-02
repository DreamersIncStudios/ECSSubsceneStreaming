using ProjectDawn.ContinuumCrowds;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// A single group that will navigate on the <see cref="CrowdSurfaceAuthoring"/>.
    /// </summary>
    [AddComponentMenu("Agents Navigation/Crowd/Crowd Group")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/crowds/crowd-group.html")]
    public class CrowdGroupAuthoring : EntityBehaviour
    {
        [SerializeField]
        protected CrowdSurfaceAuthoring m_Surface;

        [Tooltip("Crowd group speed settings.")]
        [SerializeField]
        protected Speed m_Speed = Speed.Default;

        [Tooltip("Controls the cost weights of constructing crowd flow fields.")]
        [SerializeField]
        protected CostWeights m_CostWeights = CostWeights.Default;

        [Tooltip("Source from which crowd will get goal.")]
        [SerializeField]
        protected CrowdGoalSource m_GoalSource = CrowdGoalSource.AgentDestination;

        [Tooltip("Anchors the agent to the surface. It is useful to disable then used with physics, to allow more freedom motion and precision.")]
        [SerializeField]
        protected bool m_Grounded = true;

        [Tooltip("Maximum distance that will be used when attempting to map the agent's position or destination onto surface. The higher the value, the bigger the performance cost.")]
        [SerializeField]
        protected float m_MappingRadius = 5.0f;

        /// <summary>
        /// A surface which will be used by this group.
        /// </summary>
        public CrowdSurfaceAuthoring Surface { get => m_Surface; }

        /// <summary>
        /// Crowd group speed settings.
        /// </summary>
        public Speed Speed { get => m_Speed; set => m_Speed = WriteGroup(value); }

        /// <summary>
        /// Controls the cost weights of constructing crowd flow fields.
        /// </summary>
        public CostWeights CostWeight { get => m_CostWeights; set => m_CostWeights = WriteGroup(value); }

        /// <summary>
        /// Source from which crowd will get goal.
        /// </summary>
        public CrowdGoalSource GoalSource { get => m_GoalSource; set => m_GoalSource = WriteGroup(value); }

        /// <summary>
        /// Anchors the agent to the surface. It is useful to disable then used with physics, to allow more freedom motion and precision.
        /// </summary>
        public bool Grounded { get => m_Grounded; set => m_Grounded = WriteGroup(value); }

        /// <summary>
        /// Maximum distance that will be used when attempting to map the agent's position or destination onto surface. The higher the value, the bigger the performance cost.
        /// </summary>
        public float MappingRadius { get => m_MappingRadius; set => m_MappingRadius = WriteGroup(value); }

        /// <summary>
        /// Returns default component of <see cref="CrowdGroup"/>.
        /// </summary>
        public CrowdGroup DefaultGroup => new()
        {
            Surface = m_Surface ? m_Surface.GetOrCreateEntity() : Entity.Null,
            Speed = m_Speed,
            CostWeights = m_CostWeights,
            GoalSource = m_GoalSource,
            Grounded = m_Grounded,
            MappingRadius = m_MappingRadius,
        };

        /// <summary>
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public CrowdGroup EntityGroup
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<CrowdGroup>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        }

        /// <summary>
        /// Returns true if <see cref="EntityBehaviour"/> entity has <see cref="CrowdGroup"/>.
        /// </summary>
        public bool HasEntityGroup => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<CrowdGroup>(m_Entity);

        /// <summary>
        /// Set goals of crowds group. This method requires goal source to be set to Script.
        /// </summary>
        public void SetGoals(ReadOnlySpan<float3> goals)
        {
            if (m_GoalSource != CrowdGoalSource.Script)
                throw new InvalidOperationException("SetGoals requires goal source to be set to `Script`!");

            if (m_Entity == Entity.Null)
                return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            var group = world.EntityManager.GetComponentData<CrowdGroupFlow>(m_Entity);
            group.Flow.ClearGoals();
            foreach (var goal in goals)
            {
                group.Flow.AddGoal(goal);
            }
        }

        /// <summary>
        /// Set goals of crowds group. This method requires goal source to be set to Script.
        /// </summary>
        public void SetGoals(float3 min, float3 max)
        {
            if (m_GoalSource != CrowdGoalSource.Script)
                throw new InvalidOperationException("SetGoals requires goal source to be set to `Script`!");

            if (m_Entity == Entity.Null)
                return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            var group = world.EntityManager.GetComponentData<CrowdGroupFlow>(m_Entity);
            group.Flow.ClearGoals();
            group.Flow.AddGoal(min, max);
        }

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, DefaultGroup);
        }

        void Reset()
        {
            m_Surface = GameObject.FindAnyObjectByType<CrowdSurfaceAuthoring>();
        }

        T WriteGroup<T>(in T value)
        {
            if (m_Entity == Entity.Null)
                return value;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return value;
            world.EntityManager.SetComponentData(m_Entity, DefaultGroup);
            return value;
        }
    }

    internal class CrowdGroupBaker : Baker<CrowdGroupAuthoring>
    {
        public override void Bake(CrowdGroupAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CrowdGroup
            {
                Surface = GetEntity(authoring.Surface, TransformUsageFlags.Dynamic),
                Speed = authoring.Speed,
                CostWeights = authoring.CostWeight,
                GoalSource = authoring.GoalSource,
                Grounded = authoring.Grounded,
                MappingRadius = authoring.MappingRadius,
            });
        }
    }
}
