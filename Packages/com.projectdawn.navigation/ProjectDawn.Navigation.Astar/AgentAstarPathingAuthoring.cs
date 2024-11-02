using Unity.Entities;
using UnityEngine;
using ProjectDawn.Navigation.Hybrid;
#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Pathfinding.ECS;
using Pathfinding;
#endif

namespace ProjectDawn.Navigation.Astar
{
    /// <summary>
    /// Agent uses NavMesh for pathfinding.
    /// </summary>
    [RequireComponent(typeof(AgentAuthoring))]
    [AddComponentMenu("Agents Navigation/Agent Astar Pathing")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/game-objects/pathing/astar.html")]
    public class AgentAstarPathingAuthoring : MonoBehaviour, INavMeshWallProvider
    {
#if ENABLE_ASTAR_PATHFINDING_PROJECT
        [SerializeField]
        AstarLinkTraversalMode m_LinkTraversalMode = AstarLinkTraversalMode.StateMachine;

        [SerializeField]
        AgentAstarPath m_Path = AgentAstarPath.Default;

        [SerializeField]
        ManagedState m_ManagedState = new()
        {
            enableLocalAvoidance = false,
            pathfindingSettings = PathRequestSettings.Default,
        };

        Entity m_Entity;

        /// <summary>
        /// Returns default component of <see cref="AgentAstarPath"/>.
        /// </summary>
        public AgentAstarPath DefaultPath => m_Path;

        /// <summary>
        /// Returns default component of <see cref="Pathfinding.ECS.MovementState"/>.
        /// </summary>
        public MovementState DefaultMovementState => new(transform.position);

        /// <summary>
        /// <see cref="Pathfinding.ECS.ManagedState"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// </summary>
        public ManagedState ManagedState => m_ManagedState;

        /// <summary>
        /// <see cref="AgentAstarPath"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public ref AgentAstarPath Path
        {
            get
            {
                if (World.DefaultGameObjectInjectionWorld == null)
                    return ref m_Path;

                return ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<AgentAstarPath>(m_Entity).ValueRW;
            }
        }

        /// <summary>
        /// <see cref="Pathfinding.ECS.MovementState"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public ref MovementState MovementState => ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<MovementState>(m_Entity).ValueRW;

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, m_Path);
            world.EntityManager.AddComponentData(m_Entity, m_ManagedState);
            world.EntityManager.AddComponentData(m_Entity, DefaultMovementState);

            if (m_LinkTraversalMode != AstarLinkTraversalMode.None)
            {
                world.EntityManager.AddComponent<LinkTraversal>(m_Entity);
                world.EntityManager.SetComponentEnabled<LinkTraversal>(m_Entity, false);
            }
            if (m_LinkTraversalMode == AstarLinkTraversalMode.Seeking)
                world.EntityManager.AddComponent<LinkTraversalSeek>(m_Entity);
            if (m_LinkTraversalMode == AstarLinkTraversalMode.StateMachine)
                world.EntityManager.AddComponentData(m_Entity, new AstarLinkTraversalStateMachine{});

            // Sync in case it was created as disabled
            if (!enabled)
                world.EntityManager.SetComponentEnabled<AgentAstarPath>(m_Entity, false);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                world.EntityManager.RemoveComponent<AgentAstarPath>(m_Entity);
                world.EntityManager.RemoveComponent<ManagedState>(m_Entity);
                world.EntityManager.RemoveComponent<MovementState>(m_Entity);
                if (m_LinkTraversalMode != AstarLinkTraversalMode.None)
                    world.EntityManager.RemoveComponent<LinkTraversal>(m_Entity);
                if (m_LinkTraversalMode == AstarLinkTraversalMode.Seeking)
                    world.EntityManager.RemoveComponent<LinkTraversalSeek>(m_Entity);
                if (m_LinkTraversalMode == AstarLinkTraversalMode.StateMachine)
                    world.EntityManager.RemoveComponent<AstarLinkTraversalStateMachine>(m_Entity);
            }
            m_ManagedState.Dispose();
        }

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentAstarPath>(m_Entity, true);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<AgentAstarPath>(m_Entity, false);
        }

        class AgentAstarPathingBaker : Baker<AgentAstarPathingAuthoring>
        {
            public override void Bake(AgentAstarPathingAuthoring authoring)
            {
                throw new System.NotImplementedException("Currently AgentAstarPathingBaker does not support subscene workflow. This will be implemented in the future patches.");

                /*var entity = GetEntity(TransformUsageFlags.Dynamic);
                var clonable = authoring.m_ManagedState as System.ICloneable;
                AddComponent(entity, new AstarPathing { });
                AddComponentObject(entity, Clone(authoring.m_ManagedState));
                AddComponent(entity, authoring.DefaulMovementState);
                AddComponentObject(entity, new AstarLinkTraversal
                {
                    UseDefaultSeeking = authoring.m_LinkTraversalMode == AstarLinkTraversalMode.Seeking,
                });
                if (authoring.m_LinkTraversalMode == AstarLinkTraversalMode.StateMachine)
                {
                    AddComponentObject(entity, new AstarLinkTraversalStateMachine
                    {

                    });
                }*/
            }

            /*ManagedState Clone(ManagedState other)
            {
                return new ManagedState
                {
                    autoRepath = other.autoRepath.Clone(),
                    pathTracer = default,
                    rvoSettings = other.rvoSettings,
                    pathfindingSettings = new PathRequestSettings
                    {
                        graphMask = other.pathfindingSettings.graphMask,
                        tagPenalties = other.pathfindingSettings.tagPenalties != null ? (int[]) other.pathfindingSettings.tagPenalties.Clone() : null,
                        traversableTags = other.pathfindingSettings.traversableTags,
                        traversalProvider = null, // Cannot be safely cloned or copied
                    },
                    enableLocalAvoidance = other.enableLocalAvoidance,
                    enableGravity = other.enableGravity,
                    onTraverseOffMeshLink = null, // Cannot be safely cloned or copied
                };
            }*/
        }
#endif
    }
}
