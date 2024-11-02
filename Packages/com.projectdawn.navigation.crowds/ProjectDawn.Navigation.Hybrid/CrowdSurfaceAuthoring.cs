using ProjectDawn.ContinuumCrowds;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    /// <summary>
    /// A surface on which a crowd can move.
    /// </summary>
    [AddComponentMenu("Agents Navigation/Crowd/Crowd Surface")]
    [DisallowMultipleComponent]
    [HelpURL("https://lukaschod.github.io/agents-navigation-docs/manual/crowds/crowd-surface.html")]
    public class CrowdSurfaceAuthoring : EntityBehaviour
    {
        [Tooltip("World space size.")]
        [SerializeField]
        protected float2 m_Size = new(1, 1);

        [Tooltip("Cell count horizontally.")]
        [SerializeField]
        protected int m_Width = 16;

        [Tooltip("Cell count vertically.")]
        [SerializeField]
        protected int m_Height = 16;

        [Tooltip("Crowd group density settings.")]
        [SerializeField]
        protected Density m_Density = Density.Default;

        [Tooltip("Crowd group slope settings.")]
        [SerializeField]
        protected Slope m_Slope = Slope.Default;

        [SerializeField]
        protected NavigationLayers m_Layers = NavigationLayers.Everything;

        [SerializeField]
        protected CrowdData m_Data;

        /// <summary>
        /// World space size.
        /// </summary>
        public float2 Size => m_Size;

        /// <summary>
        /// Cell count horizontally.
        /// </summary>
        public int Width => m_Width;

        /// <summary>
        /// Cell count vertically.
        /// </summary>
        public int Height => m_Height;

        /// <summary>
        /// Local space size of single cell.
        /// </summary>
        public float2 CellSize => Size / new float2(m_Width, m_Height);

        /// <summary>
        /// Transform from local to world space.
        /// </summary>
        public NonUniformTransform Transform => NonUniformTransform.FromPositionRotationScale(transform.position, transform.rotation, new float3(CellSize, 1));

        /// <summary>
        /// Up vector of surface.
        /// </summary>
        public float3 Normal => Transform.Forward();

        public CrowdData Data { get => m_Data; set => m_Data = value; }

        /// <summary>
        /// Returns default component of <see cref="CrowdSurface"/>.
        /// </summary>
        public CrowdSurface DefaultSurface => new()
        {
            Size = m_Size,
            Width = m_Width,
            Height = m_Height,
            Density = m_Density,
            Slope = m_Slope,
            Layers = m_Layers,
        };

        /// <summary>
        /// Returns true, if crowds data is valid for this surface.
        /// </summary>
        public bool IsDataValid()
        {
            if (m_Data == null)
                return false;
            if (m_Data.Width != m_Width || m_Data.Height != m_Height)
                return false;
            if (m_Data.HeightField.Length != m_Width * m_Height || m_Data.ObstacleField.Length != m_Width * m_Height)
                return false;
            return true;
        }

        /// <summary>
        /// Applies changes of data.
        /// </summary>
        public void ApplyData()
        {
            if (!IsDataValid())
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!world.EntityManager.HasComponent<CrowdSurfaceWorld>(m_Entity))
                return;

            var surfaceWorld = world.EntityManager.GetComponentData<CrowdSurfaceWorld>(m_Entity);
            var crowdWorld = surfaceWorld.World;

            crowdWorld.SetHeightField(m_Data.HeightField);
            crowdWorld.SetObstacleField(m_Data.ObstacleField);
            crowdWorld.RecalculateHeightGradientField();
        }

        void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            m_Entity = GetOrCreateEntity();
            world.EntityManager.AddComponentData(m_Entity, DefaultSurface);
            world.EntityManager.AddComponentData(m_Entity, LocalTransform.FromPositionRotationScale(transform.position, transform.rotation, 1));
            world.EntityManager.AddSharedComponentManaged(m_Entity, new CrowdSurfaceData { Data = Data});
        }
    }

    internal class CrowdSurfaceBaker : Baker<CrowdSurfaceAuthoring>
    {
        public override void Bake(CrowdSurfaceAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.DefaultSurface);
            AddSharedComponentManaged(GetEntity(TransformUsageFlags.Dynamic), new CrowdSurfaceData { Data = authoring.Data});
        }
    }
}
