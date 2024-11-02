using ProjectDawn.ContinuumCrowds;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;

namespace ProjectDawn.Navigation
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial class CrowdSurfaceSystem : SystemBase
    {
        List<CrowdWorld> m_Worlds = new();
        List<CrowdFlow> m_Flows = new();

        protected override void OnUpdate()
        {
            Entities.WithNone<CrowdSurfaceWorld>().ForEach((Entity entity, in CrowdSurfaceData data, in CrowdSurface surface, in LocalTransform transform) =>
            {
                float3 scale = new float3(surface.Size.xy / new float2(surface.Width, surface.Height), 1);

                var world = new CrowdWorld(surface.Width, surface.Height, NonUniformTransform.FromPositionRotationScale(transform.Position, transform.Rotation, scale), Allocator.Persistent);
                EntityManager.AddComponentData(entity, new CrowdSurfaceWorld { World = world });
                m_Worlds.Add(world);

                if (data.Data)
                {
                    world.SetHeightField(data.Data.HeightField);
                    world.SetObstacleField(data.Data.ObstacleField);
                    world.RecalculateHeightGradientField();
                }
            }).WithStructuralChanges().Run();

            Entities.WithNone<CrowdSurface>().ForEach((Entity entity, in CrowdSurfaceWorld world) =>
            {
                world.World.Dispose();
                m_Worlds.Remove(world.World);
                EntityManager.RemoveComponent<CrowdSurfaceWorld>(entity);
            }).WithStructuralChanges().Run();

            Entities.WithNone<CrowdGroupFlow>().ForEach((Entity entity, in CrowdGroup group) =>
            {
                if (!EntityManager.HasComponent<CrowdSurfaceWorld>(group.Surface))
                    throw new System.InvalidOperationException("CrowdGroup does not have valid surface entity set!");

                var surface = EntityManager.GetComponentData<CrowdSurfaceWorld>(group.Surface);

                if (!surface.World.IsCreated)
                    throw new System.InvalidOperationException("CrowdGroup surface has to be created!");

                var layer = new CrowdFlow(surface.World.Width, surface.World.Height, surface.World.Transform, Allocator.Persistent);
                EntityManager.AddComponentData(entity, new CrowdGroupFlow { Flow = layer });
                m_Flows.Add(layer);
            }).WithStructuralChanges().Run();

            Entities.WithNone<CrowdGroup>().ForEach((Entity entity, in CrowdGroupFlow flow) =>
            {
                flow.Flow.Dispose();
                m_Flows.Remove(flow.Flow);
                EntityManager.RemoveComponent<CrowdGroupFlow>(entity);
            }).WithStructuralChanges().Run();
        }

        protected override void OnDestroy()
        {
            foreach (var world in m_Worlds)
                world.Dispose();
            m_Worlds.Clear();
            foreach (var flow in m_Flows)
                flow.Dispose();
            m_Flows.Clear();
        }
    }
}
