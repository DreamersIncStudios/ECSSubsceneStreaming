using System.Xml;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Serialization;

namespace ProjectDawn.Navigation.Hybrid
{
    public interface INavMeshWallProvider { }

    /// <summary>
    /// Agent uses NavMesh for pathfinding.
    /// </summary>
    [RequireComponent(typeof(AgentAuthoring))]
    [AddComponentMenu("Agents Navigation/Agent NavMesh Pathing")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/game-objects/pathing/nav-mesh.html")]
    public class AgentNavMeshAuthoring : MonoBehaviour, INavMeshWallProvider
    {
        [SerializeField]
        protected int AgentTypeId = 0;

        [SerializeField]
        protected int AreaMask = -1;

        [SerializeField]
        protected bool AutoRepath = true;

        [FormerlySerializedAs("m_Constrained")]
        [SerializeField]
        protected bool m_Grounded = true;

        [SerializeField]
        internal bool m_OverrideAreaCosts;

        [SerializeField]
        internal NavMeshLinkTraversalMode m_LinkTraversalMode = NavMeshLinkTraversalMode.None;

        [SerializeField]
        protected float3 MappingExtent = 10;

        Entity m_Entity;

        /// <summary>
        /// Returns default component of <see cref="NavMeshPath"/>.
        /// </summary>
        [System.Obsolete("This property has been renamed. Please use DefaultPath!")]
        public NavMeshPath DefaulPath => DefaultPath;

        /// <summary>
        /// Returns default component of <see cref="NavMeshPath"/>.
        /// </summary>
        public NavMeshPath DefaultPath => new()
        {
            State = NavMeshPathState.FinishedFullPath,
            AgentTypeId = AgentTypeId,
            AreaMask = AreaMask,
            AutoRepath = AutoRepath,
            Grounded = m_Grounded,
            MappingExtent = MappingExtent,
        };

        /// <summary>
        /// <see cref="NavMeshPath"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public NavMeshPath EntityPath
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<NavMeshPath>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_Entity, value);
        }

        /// <summary>
        /// <see cref="NavMeshLinkTraversal"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public bool OnLinkTraversal
        {
            get => World.DefaultGameObjectInjectionWorld.EntityManager.IsComponentEnabled<LinkTraversal>(m_Entity);
            set => World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentEnabled<LinkTraversal>(m_Entity, value);
        }

        public ref LinkTraversalSeek SeekLinkTraversal => ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<LinkTraversalSeek>(m_Entity).ValueRW;

        public ref NavMeshLinkTraversal NavMeshLinkTraversal => ref World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentDataRW<NavMeshLinkTraversal>(m_Entity).ValueRW;

        /// <summary>
        /// <see cref="NavMeshNode"/> component of this <see cref="AgentAuthoring"/> Entity.
        /// Accessing this property is potentially heavy operation as it will require wait for agent jobs to finish.
        /// </summary>
        public DynamicBuffer<NavMeshNode> EntityNodes => World.DefaultGameObjectInjectionWorld.EntityManager.GetBuffer<NavMeshNode>(m_Entity);

        /// <summary>
        /// Returns true if <see cref="AgentAuthoring"/> entity has <see cref="NavMeshPath"/>.
        /// </summary>
        public bool HasEntityPath => World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<NavMeshPath>(m_Entity);

        /// <summary>
        /// Creates structure for accessing the corners of current agent path.
        /// </summary>
        public NavMeshCorners CreateCorners(int capacity, Allocator allocator = Allocator.Persistent) { return new NavMeshCorners(capacity, m_Entity, allocator); }

        /// <summary>
        /// Sets the cost for traversing over areas of the area type.
        /// </summary>
        /// <param name="areaIndex"></param>
        /// <param name="areaCost"></param>
        public void SetAreaCost(int areaIndex, float areaCost)
        {
            if (!World.DefaultGameObjectInjectionWorld.EntityManager.HasBuffer<NavMeshAreaCost>(m_Entity))
                throw new System.InvalidOperationException($"Missing NavMeshAreaCost buffer. Makes sure that {gameObject.name} has Override Area Costs enabled.");
            var costs = World.DefaultGameObjectInjectionWorld.EntityManager.GetBuffer<NavMeshAreaCost>(m_Entity);
            costs[areaIndex] = new NavMeshAreaCost { Value = areaCost };
        }

        /// <summary>
        /// Gets the cost for path calculation when crossing area of a particular type.
        /// The cost of a path is the amount of "difficulty" involved in calculating it - the shortest path may not be the best if it passes over difficult ground, such as mud, snow, etc.Different types of areas are denoted by navmesh areas in Unity.The cost of a particular area is given in cost units per distance unit.Note that the cost of a path applies to the pathfinding only and does not automatically affect the movement speed of the agent when following the path.Indeed, the path's cost may denote other factors such as danger (safe but long path through a minefield) or visibility (long path that keeps a character in the shadows)
        /// </summary>
        /// <param name="areaIndex"></param>
        /// <returns></returns>
        public float GetAreaCost(int areaIndex)
        {
            if (!World.DefaultGameObjectInjectionWorld.EntityManager.HasBuffer<NavMeshAreaCost>(m_Entity))
                throw new System.InvalidOperationException($"Missing NavMeshAreaCost buffer. Makes sure that {gameObject.name} has Override Area Costs enabled.");
            var costs = World.DefaultGameObjectInjectionWorld.EntityManager.GetBuffer<NavMeshAreaCost>(m_Entity);
            return costs[areaIndex].Value;
        }

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetComponent<AgentAuthoring>().GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, DefaultPath);
            world.EntityManager.AddBuffer<NavMeshNode>(m_Entity);

            if (m_OverrideAreaCosts)
            {
                var costs = world.EntityManager.AddBuffer<NavMeshAreaCost>(m_Entity);
                costs.Resize(32, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < 32; i++)
                {
                    costs[i] = new NavMeshAreaCost { Value = 1.0f };
                }
            }

            // Sync in case it was created as disabled
            if (!enabled)
                world.EntityManager.SetComponentEnabled<NavMeshPath>(m_Entity, false);

            if (m_LinkTraversalMode != NavMeshLinkTraversalMode.None)
            {
                world.EntityManager.AddComponent<LinkTraversal>(m_Entity);
                world.EntityManager.SetComponentEnabled<LinkTraversal>(m_Entity, false);
            }
            if (m_LinkTraversalMode == NavMeshLinkTraversalMode.Seeking)
                world.EntityManager.AddComponent<LinkTraversalSeek>(m_Entity);
            if (m_LinkTraversalMode == NavMeshLinkTraversalMode.Custom)
                world.EntityManager.AddComponent<NavMeshLinkTraversal>(m_Entity);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                world.EntityManager.RemoveComponent<NavMeshPath>(m_Entity);
                world.EntityManager.RemoveComponent<NavMeshNode>(m_Entity);
                if (m_OverrideAreaCosts)
                    world.EntityManager.RemoveComponent<NavMeshAreaCost>(m_Entity);
                if (m_LinkTraversalMode != NavMeshLinkTraversalMode.None)
                    world.EntityManager.RemoveComponent<LinkTraversal>(m_Entity);
                if (m_LinkTraversalMode == NavMeshLinkTraversalMode.Seeking)
                    world.EntityManager.RemoveComponent<LinkTraversalSeek>(m_Entity);
                if (m_LinkTraversalMode == NavMeshLinkTraversalMode.Custom)
                    world.EntityManager.RemoveComponent<NavMeshLinkTraversal>(m_Entity);
            }
        }

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<NavMeshPath>(m_Entity, true);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;
            world.EntityManager.SetComponentEnabled<NavMeshPath>(m_Entity, false);
        }
    }

    internal class AgentNavMeshBaker : Baker<AgentNavMeshAuthoring>
    {
        public override void Bake(AgentNavMeshAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, authoring.DefaultPath);
            AddBuffer<NavMeshNode>(entity);

            if (authoring.m_OverrideAreaCosts)
            {
                var costs = AddBuffer<NavMeshAreaCost>(entity);
                costs.Resize(32, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < 32; i++)
                {
                    costs[i] = new NavMeshAreaCost { Value = 1.0f };
                }
            }

            if (authoring.m_LinkTraversalMode != NavMeshLinkTraversalMode.None)
            {
                AddComponent<LinkTraversal>(entity);
                SetComponentEnabled<LinkTraversal>(entity, false);
            }
            if (authoring.m_LinkTraversalMode == NavMeshLinkTraversalMode.Seeking)
                AddComponent<LinkTraversalSeek>(entity);
            if (authoring.m_LinkTraversalMode == NavMeshLinkTraversalMode.Custom)
                AddComponent<NavMeshLinkTraversal>(entity);
        }
    }
}
