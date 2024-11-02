using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using static Unity.Entities.SystemAPI;
using Unity.Burst.Intrinsics;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// System that controls agent NavMesh path.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct NavMeshPathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var navmesh = GetSingletonRW<NavMeshQuerySystem.Singleton>();
            new NavMeshPathJob
            {
                NavMesh = navmesh.ValueRW,
                AreaCost = new OptionalBufferAccessor<NavMeshAreaCost>(GetBufferTypeHandle<NavMeshAreaCost>())
            }.Schedule();
            navmesh.ValueRW.World.AddDependency(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Agent))]
        partial struct NavMeshPathJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public NavMeshQuerySystem.Singleton NavMesh;
            public OptionalBufferAccessor<NavMeshAreaCost> AreaCost;

            public void Execute([EntityIndexInChunk] int index, ref DynamicBuffer<NavMeshNode> nodes, ref NavMeshPath path, in AgentBody body, in LocalTransform transform)
            {
                switch (path.State)
                {
                    case NavMeshPathState.WaitingNewPath:
                        // Release previous handle if it is valid
                        if (NavMesh.Exist(path.QueryHandle))
                            NavMesh.DestroyQuery(path.QueryHandle);

                        path.Location = NavMesh.MapLocation(transform.Position, path.MappingExtent, path.AgentTypeId, path.AreaMask);
                        path.EndLocation = NavMesh.MapLocation(body.Destination, path.MappingExtent, path.AgentTypeId, path.AreaMask);
                        path.QueryHandle = NavMesh.CreateQuery(path.Location, path.EndLocation, path.AgentTypeId, path.AreaMask,
                            AreaCost.TryGetBuffer(index, out var costs) ?
                            costs.Reinterpret<float>().AsNativeArray() : default);
                        path.State = NavMeshPathState.InProgress;
                        break;

                    case NavMeshPathState.InProgress:
                        var status = NavMesh.GetStatus(path.QueryHandle);
                        switch (status)
                        {
                            case NavMeshQueryStatus.InProgress:
                                break;

                            case NavMeshQueryStatus.FinishedFullPath:
                            case NavMeshQueryStatus.FinishedPartialPath:
                                path.State = status == NavMeshQueryStatus.FinishedFullPath ? NavMeshPathState.FinishedFullPath : NavMeshPathState.FinishedPartialPath;

                                // Copy path polygons into nodes
                                var polygons = NavMesh.GetPolygons(path.QueryHandle);
                                nodes.ResizeUninitialized(polygons.Length);
                                for (int i = 0; i < polygons.Length; ++i)
                                    nodes[i] = new NavMeshNode { Value = polygons[i] };

                                // Release query so it could be reused
                                NavMesh.DestroyQuery(path.QueryHandle);
                                path.QueryHandle = NavMeshQueryHandle.Null;
                                break;

                            case NavMeshQueryStatus.Failed:
                                path.State = NavMeshPathState.Failed;

                                nodes.ResizeUninitialized(0);

                                // Release query so it could be reused
                                NavMesh.DestroyQuery(path.QueryHandle);
                                path.QueryHandle = NavMeshQueryHandle.Null;
                                break;

                            default:
                                break;

                        }
                        break;

                    case NavMeshPathState.InValid:
                        if (path.AutoRepath)
                            path.State = NavMeshPathState.WaitingNewPath;
                        break;
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                AreaCost.Update(chunk);
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
                
            }
        }
    }
}
