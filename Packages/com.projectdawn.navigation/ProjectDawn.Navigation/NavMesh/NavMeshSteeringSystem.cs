using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine.Experimental.AI;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// System that steers agents within the NavMesh path.
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(NavMeshBoundarySystem))]
    [UpdateAfter(typeof(NavMeshPathSystem))]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct NavMeshSteeringSystem : ISystem
    {
        ComponentLookup<LinkTraversal> m_OnLinkTraversalLookup;
        ComponentLookup<LinkTraversalSeek> m_SeekLinkTraversalLookup;
        ComponentLookup<NavMeshLinkTraversal> m_NavMeshLinkTraversalLookup;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_OnLinkTraversalLookup = state.GetComponentLookup<LinkTraversal>();
            m_SeekLinkTraversalLookup = state.GetComponentLookup<LinkTraversalSeek>();
            m_NavMeshLinkTraversalLookup = state.GetComponentLookup<NavMeshLinkTraversal>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navmesh = GetSingleton<NavMeshQuerySystem.Singleton>();
            m_OnLinkTraversalLookup.Update(ref state);
            m_SeekLinkTraversalLookup.Update(ref state);
            m_NavMeshLinkTraversalLookup.Update(ref state);
            new NavMeshSteeringJob
            {
                NavMesh = navmesh,
                OnLinkTraversalLookup = m_OnLinkTraversalLookup,
                SeekLinkTraversalLookup = m_SeekLinkTraversalLookup,
                NavMeshLinkTraversalLookup = m_NavMeshLinkTraversalLookup,
                DeltaTime = Time.DeltaTime,
            }.ScheduleParallel();
            navmesh.World.AddDependency(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Agent))]
        [WithNone(typeof(LinkTraversal))]
        unsafe partial struct NavMeshSteeringJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public NavMeshQuerySystem.Singleton NavMesh;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LinkTraversal> OnLinkTraversalLookup;
            bool HasOnLinkTreaversal;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LinkTraversalSeek> SeekLinkTraversalLookup;
            bool HasSeekLinkTreaversal;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<NavMeshLinkTraversal> NavMeshLinkTraversalLookup;
            bool HasNavMeshLinkTreaversal;

            [NativeDisableContainerSafetyRestriction]
            NavMeshFunnel Funnel;

            public float DeltaTime;

            public void Execute(Entity entity, ref AgentBody body, ref NavMeshPath path, ref DynamicBuffer<NavMeshNode> nodes, in LocalTransform transform)
            {
                // Update current location if changed
                if (!NavMesh.IsValid(path.Location.polygon) || math.distancesq(transform.Position, (float3) path.Location.position) > 0.01f)
                {
                    NavMeshLocation location = NavMesh.MapLocation(transform.Position, path.MappingExtent, path.AgentTypeId, path.AreaMask);

                    // Handle case if failde to map location
                    if (location.polygon.IsNull())
                    {
                        UnityEngine.Debug.LogWarning("Failed to map agent position to nav mesh location. This can happen either if nav mesh is not present or property MappingExtent value is too low.");
                        return;
                    }

                    // In case location polygon changed, we need to progress path so funnel would work correctly
                    if (location.polygon != path.Location.polygon)
                        NavMesh.ProgressPath(ref nodes, path.Location.polygon, location.polygon);

                    path.Location = location;
                }

                if (body.IsStopped)
                    return;

                // Skip if path is not finished
                if (path.State != NavMeshPathState.FinishedFullPath && path.State != NavMeshPathState.FinishedPartialPath && path.State != NavMeshPathState.Failed)
                {
                    // TODO: Check would be best way to handle un finished path
                    // Now it continues to move to destination without accounting navmesh
                    //body.Force = 0;
                    //destination.RemainingDistance = 0;
                    return;
                }

                // Update end location if changed
                if (!NavMesh.IsValid(path.EndLocation.polygon) || math.distancesq(body.Destination, (float3) path.EndLocation.position) > 0.01f)
                {
                    NavMeshLocation location = NavMesh.MapLocation(body.Destination, path.MappingExtent, path.AgentTypeId, path.AreaMask);

                    // Handle case if failde to map location
                    if (location.polygon.IsNull())
                    {
                        UnityEngine.Debug.LogWarning("Failed to map agent destination to nav mesh location. This can happen either if nav mesh is not present or property MappingExtent value is too low.");
                    }

                    // Update destination to avoid mapping location again
                    body.Destination = location.position;

                    // Handle the case if destination is no longer within the path
                    if (path.EndLocation.polygon != location.polygon)
                    {
                        path.State = NavMeshPathState.WaitingNewPath;
                        return;
                    }

                    path.EndLocation = location;
                }

                // If path failed simply stop
                if (path.State == NavMeshPathState.Failed)
                {
                    body.Stop();
                    return;
                }

                var polygons = nodes.AsNativeArray().Reinterpret<PolygonId>();

                // With empty polygons we can assume destination reached
                if (polygons.Length == 0)
                {
                    body.Stop();
                    return;
                }

                // Path nodes only contains optimal path using navigation mesh polygons
                // Here we create the corridor from those polygons
                // It basically finds the shortest path using polygons vertices (a.k.a. corners)
                if (NavMesh.TryCreateFunnel(ref Funnel, polygons, transform.Position, body.Destination))
                {
                    var locations = Funnel.AsLocations();
                    if (locations.Length > 1)
                    {
                        body.Force = math.normalizesafe((float3) locations[1].position - transform.Position);
                        body.RemainingDistance = Funnel.IsEndReachable ? Funnel.GetCornersDistance() : float.MaxValue;

                        if (HasOnLinkTreaversal)
                        {
                            var flags = Funnel.AsFlags();
                            if (locations.Length > 2 && flags[1] == StraightPathFlags.OffMeshConnection)
                            {
                                var endPortal = new Portal();
                                float remainingDistanceSq = math.lengthsq(body.Velocity) * 1.5f * DeltaTime * DeltaTime;
                                if (math.distancesq(locations[1].position, transform.Position) <= remainingDistanceSq &&
                                    NavMesh.GetPortalPoints(locations[1].polygon, locations[2].polygon, out endPortal.Left, out endPortal.Right))
                                {
                                    if (HasOnLinkTreaversal)
                                        OnLinkTraversalLookup.SetComponentEnabled(entity, true);

                                    if (HasSeekLinkTreaversal)
                                    {
                                        SeekLinkTraversalLookup[entity] = new LinkTraversalSeek
                                        {
                                            Start = new Portal(locations[1].position, locations[1].position),
                                            End = endPortal,
                                        };
                                        //nodes.RemoveAt(1);
                                    }


                                    if (HasNavMeshLinkTreaversal)
                                    {
                                        NavMeshLinkTraversalLookup[entity] = new NavMeshLinkTraversal
                                        {
                                            StartPolygon = locations[1].polygon,
                                            EndPolygon = locations[2].polygon,
                                            Seek = new LinkTraversalSeek
                                            {
                                                Start = new Portal(locations[1].position, locations[1].position),
                                                End = endPortal,
                                            }
                                        };
                                        //nodes.RemoveAt(1);
                                    }

                                    path.Location = default;
                                }
                            }
                        }
                     }
                }
                else
                {
                    // If we can not create corridor from polygons, request new path
                    // Usually it can happen if some nodes are not connected
                    path.State = NavMeshPathState.InValid;
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Create tunnel that will allow finding optimal path is navmesh nodes
                // Only 4 positions constructed as, because of local changes there is change path will need change
                Funnel = new NavMeshFunnel(4, Allocator.Temp);
                HasSeekLinkTreaversal = chunk.Has<LinkTraversalSeek>();
                HasOnLinkTreaversal = chunk.Has<LinkTraversal>();
                HasNavMeshLinkTreaversal = chunk.Has<NavMeshLinkTraversal>();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
                Funnel.Dispose();
            }
        }
    }
}
