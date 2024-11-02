using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Experimental.AI;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Helper structure for getting corners of agent navmesh path.
    /// </summary>
    public struct NavMeshCorners : System.IDisposable
    {
        Entity m_Entity;
        NavMeshFunnel m_Funnel;

        public bool TryGetCorners(out NativeSlice<NavMeshLocation> corners)
        {
            var world = World.DefaultGameObjectInjectionWorld;

            if (world == null)
            {
                corners = default;
                return false;
            }

            var entityManager = world.EntityManager;

            var navmeshSystem = world.GetExistingSystem<NavMeshQuerySystem>();
            var navmesh = entityManager.GetComponentData<NavMeshQuerySystem.Singleton>(navmeshSystem);

            var polygons = entityManager.GetBuffer<NavMeshNode>(m_Entity).AsNativeArray().Reinterpret<PolygonId>();

            // Funnel requires at least two points start and end
            if (polygons.Length > 2)
            {
                corners = default;
                return false;
            }

            if (!navmesh.TryCreateFunnel(ref m_Funnel, polygons, entityManager.GetComponentData<LocalTransform>(m_Entity).Position, entityManager.GetComponentData<AgentBody>(m_Entity).Destination))
            {
                corners = default;
                return false;
            }

            corners = m_Funnel.AsLocations();
            return true;
        }

        public NavMeshCorners(int capacity, Entity entity, Allocator allocator)
        {
            m_Entity = entity;
            m_Funnel = new NavMeshFunnel(capacity, allocator);
        }

        public void Dispose()
        {
            m_Funnel.Dispose();
        }
    }
}
