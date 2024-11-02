#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Entities.SystemAPI;
using Pathfinding.ECS;
using System.Runtime.InteropServices;
using Pathfinding;
using UnityEngine.Profiling;
using Pathfinding.PID;
using static Pathfinding.ECS.FollowerControlSystem;

namespace ProjectDawn.Navigation.Astar
{
    /// <summary>
    /// System that steers agents within the NavMesh path.
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(NavMeshBoundarySystem))]
    [UpdateAfter(typeof(NavMeshPathSystem))]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    public partial struct AstarPathSystem : ISystem
    {
        EntityQuery m_Query;
        GCHandle m_EntityManagerHandle;
        ComponentTypeHandle<LocalTransform> LocalTransformTypeHandleRO;
        ComponentTypeHandle<MovementState> MovementStateTypeHandleRW;
        ComponentTypeHandle<ManagedState> ManagedStateTypeHandleRW;
        ComponentTypeHandle<LinkTraversal> OnLinkTraversalTypeHandleRW;
        ComponentTypeHandle<LinkTraversalSeek> SeekTraversalTypeHandleRW;
        ComponentTypeHandle<AgentShape> ShapeTypeHandleRO;
        ComponentTypeHandle<AgentBody> BodyTypeHandleRW;

        void ISystem.OnCreate(ref SystemState state)
        {
            m_EntityManagerHandle = GCHandle.Alloc(state.EntityManager);
            m_Query = QueryBuilder()
                .WithAll<AgentAstarPath>()
                .WithAll<LocalTransform>()
                .WithAllRW<MovementState>()
                .WithAll<AgentShape>()
                .WithAllRW<AgentBody>()
                .WithAll<ManagedState>()
                .Build();

            LocalTransformTypeHandleRO = state.GetComponentTypeHandle<LocalTransform>(true);
            MovementStateTypeHandleRW = state.GetComponentTypeHandle<MovementState>(false);
            ManagedStateTypeHandleRW = state.EntityManager.GetComponentTypeHandle<ManagedState>(false);
            OnLinkTraversalTypeHandleRW = state.GetComponentTypeHandle<LinkTraversal>(false);
            SeekTraversalTypeHandleRW = state.GetComponentTypeHandle<LinkTraversalSeek>(false);
            ShapeTypeHandleRO = state.GetComponentTypeHandle<AgentShape>(true);
            BodyTypeHandleRW = state.GetComponentTypeHandle<AgentBody>(false);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            m_EntityManagerHandle.Free();
        }

        //[BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (AstarPath.active == null)
                return;

            var readLock = AstarPath.active.LockGraphDataForReading();

            LocalTransformTypeHandleRO.Update(ref state);
            MovementStateTypeHandleRW.Update(ref state);
            ManagedStateTypeHandleRW.Update(ref state);
            OnLinkTraversalTypeHandleRW.Update(ref state);
            SeekTraversalTypeHandleRW.Update(ref state);
            ShapeTypeHandleRO.Update(ref state);
            BodyTypeHandleRW.Update(ref state);

            // Copied from FollowerControlSystem
            Profiler.BeginSample("Schedule search");
            // Block the pathfinding threads from starting new path calculations while this loop is running.
            // This is done to reduce lock contention and significantly improve performance.
            // If we did not do this, all pathfinding threads would immediately wake up when a path was pushed to the queue.
            // Immediately when they wake up they will try to acquire a lock on the path queue.
            // If we are scheduling a lot of paths, this causes significant contention, and can make this loop take 100 times
            // longer to complete, compared to if we block the pathfinding threads.
            // TODO: Switch to a lock-free queue to avoid this issue altogether.
            var pathfindingLock = AstarPath.active.PausePathfindingSoon(); // TODO: Astar to public
            var time = (float) SystemAPI.Time.ElapsedTime;

            foreach (var (managedState, pathing, shape, transform, body, entity) in
                SystemAPI.Query<ManagedState, RefRW<AgentAstarPath>, RefRO<AgentShape>, RefRO<LocalTransform>, RefRO<AgentBody>>()
                .WithNone<LinkTraversal>()
                .WithEntityAccess())
            {
                if (!managedState.pathTracer.isCreated)
                    managedState.pathTracer = new PathTracer(Allocator.Persistent);

                var transformRW = transform.ValueRO;

                var destination = new DestinationPoint
                {
                    destination = body.ValueRO.Destination,
                    facingDirection = body.ValueRO.Force,
                };

                var movementPlane = new AgentMovementPlane(shape.ValueRO.Orentation);

                ref var autoRepath = ref pathing.ValueRW.AutoRepath;// new Pathfinding.ECS.AutoRepathPolicy(managedState.autoRepath);
                bool wantsToRecalculatePath = autoRepath.ShouldRecalculatePath(transform.ValueRO.Position, shape.ValueRO.Radius, destination.destination, time);
#if MODULE_ASTAR_5_2_0_OR_NEWER
                RepairPathSystem.JobRecalculatePaths.MaybeRecalculatePath(managedState, ref pathing.ValueRW.AutoRepath, ref transformRW, ref destination, ref movementPlane, time, wantsToRecalculatePath);
#else
                JobRecalculatePaths.MaybeRecalculatePath(managedState, ref pathing.ValueRW.AutoRepath, ref transformRW, ref destination, ref movementPlane, time, wantsToRecalculatePath);
#endif
            }

            pathfindingLock.Release();
            Profiler.EndSample();

            // Handle state machine link traversal
            foreach (var (managedLinkInfo, managedState, transform, body, shape, entity) in
                SystemAPI.Query<AstarLinkTraversalStateMachine, ManagedState, RefRW<LocalTransform>, RefRW<AgentBody>, RefRO<AgentShape>>()
                .WithAll<LinkTraversal>()
                .WithEntityAccess())
            {
                // We need these dummy for context
                var movementPlane = new AgentMovementPlane(shape.ValueRO.Orentation);
                var movementControl = new MovementControl();
                var movementSettings = new MovementSettings();

                // Initialize state machine with coroutine
                if (managedLinkInfo.context == null)
                {
#if MODULE_ASTAR_5_2_0_OR_NEWER
                    var linkInfo = RepairPathSystem.NextLinkToTraverse(managedState);
#else
                    var linkInfo = FollowerControlSystem.NextLinkToTraverse(managedState);
#endif

                    var ctx = new AstarLinkTraversalContext(linkInfo.link);
                    managedLinkInfo.link = new AgentOffMeshLinkTraversal(linkInfo);
                    managedLinkInfo.context = ctx;
#if MODULE_ASTAR_5_2_0_OR_NEWER
                    managedLinkInfo.handler = RepairPathSystem.ResolveOffMeshLinkHandler(managedState, ctx);
#else
                    managedLinkInfo.handler = FollowerControlSystem.ResolveOffMeshLinkHandler(managedState, ctx);
#endif
                    managedLinkInfo.stateMachine = null;
                    managedLinkInfo.coroutine = null;
                }

                if (JobManagedOffMeshLinkTransition.MoveNext(entity, managedState, ref transform.ValueRW, ref movementPlane, ref movementControl, ref movementSettings, ref managedLinkInfo.link, managedLinkInfo, SystemAPI.Time.DeltaTime))
                {
                    body.ValueRW.Force = math.normalizesafe(managedLinkInfo.context.movementControl.targetPoint - transform.ValueRO.Position);
                }
                else
                {
                    SystemAPI.SetComponentEnabled<LinkTraversal>(entity, false);
                    managedLinkInfo.context = null;
                }
            }

            state.Dependency = new AstarPathJob
            {
                EntityManagerHandle = m_EntityManagerHandle,

                LocalTransformTypeHandleRO = LocalTransformTypeHandleRO,
                MovementStateTypeHandleRW = MovementStateTypeHandleRW,
                ManagedStateTypeHandleRW = ManagedStateTypeHandleRW,
                OnLinkTraversalTypeHandleRW = OnLinkTraversalTypeHandleRW,
                SeekTraversalTypeHandleRW = SeekTraversalTypeHandleRW,
                ShapeTypeHandleRO = ShapeTypeHandleRO,
                BodyTypeHandleRW = BodyTypeHandleRW,

                OnlyApplyPendingPaths = true,
            }.ScheduleParallel(m_Query, state.Dependency);

            state.Dependency = new AstarPathJob
            {
                EntityManagerHandle = m_EntityManagerHandle,

                LocalTransformTypeHandleRO = LocalTransformTypeHandleRO,
                MovementStateTypeHandleRW = MovementStateTypeHandleRW,
                ManagedStateTypeHandleRW = ManagedStateTypeHandleRW,
                OnLinkTraversalTypeHandleRW = OnLinkTraversalTypeHandleRW,
                SeekTraversalTypeHandleRW = SeekTraversalTypeHandleRW,
                ShapeTypeHandleRO = ShapeTypeHandleRO,
                BodyTypeHandleRW = BodyTypeHandleRW,
            }.ScheduleParallel(m_Query, state.Dependency);

            var navmeshEdgeData = AstarPath.active.GetNavmeshBorderData(out var readLock2); // TODO: Astar to public
            state.Dependency = new AstarBoundaryJob
            {
                NavmeshEdgeData = navmeshEdgeData,
            }.ScheduleParallel(state.Dependency);
            readLock2.UnlockAfter(state.Dependency);

            // TODO:
            readLock.UnlockAfter(state.Dependency);
        }

        partial struct AstarBoundaryJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public NavmeshEdges.NavmeshBorderData NavmeshEdgeData;

            [NativeDisableContainerSafetyRestriction]
            NativeList<float2> EdgesScratch;

            public void Execute(DynamicBuffer<NavMeshWall> walls, in AgentShape shape, in AgentBody body, in MovementState movementState, in LocalTransform transform)
            {
                if (body.IsStopped)
                    return;

                walls.Clear();

                if (!movementState.isOnValidNode)
                    return;

                var movementPlane = new AgentMovementPlane(shape.Orentation);

                // TODO: Astar
                var movement = new PIDMovement
                {
                    speed = 5.0f,
                    rotationSpeed = 600,
                    desiredWallDistance = 0.5f,
                    leadInRadiusWhenApproachingDestination = 1.0f,
                };

                movement.ScaleByAgentScale(transform.Scale);

                var localBounds = PIDMovement.InterestingEdgeBounds(ref movement, transform.Position, movementState.nextCorner, shape.Height, movementPlane.value);
                EdgesScratch.Clear();
                NavmeshEdgeData.GetEdgesInRange(movementState.hierarchicalNodeIndex, localBounds, EdgesScratch, movementPlane.value);

                for (int i = 0; i < EdgesScratch.Length;)
                {
                    // TODO: It should use node height not agent
                    float2 start = EdgesScratch[i++];
                    float2 end = EdgesScratch[i++];
                    walls.Add(new NavMeshWall { Start = new float3(start.x, transform.Position.y, start.y), End = new float3(end.x, transform.Position.y, end.y) });
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!EdgesScratch.IsCreated)
                    EdgesScratch = new NativeList<float2>(64, Allocator.Temp);
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }

        struct AstarPathJob : IJobChunk
        {
            public GCHandle EntityManagerHandle;
            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> LocalTransformTypeHandleRO;
            public ComponentTypeHandle<MovementState> MovementStateTypeHandleRW;
            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public ComponentTypeHandle<ManagedState> ManagedStateTypeHandleRW;

            [ReadOnly]
            public ComponentTypeHandle<AgentShape> ShapeTypeHandleRO;
            public ComponentTypeHandle<AgentBody> BodyTypeHandleRW;

            public ComponentTypeHandle<LinkTraversal> OnLinkTraversalTypeHandleRW;
            public bool HasOnLinkTraversal;

            public ComponentTypeHandle<LinkTraversalSeek> SeekTraversalTypeHandleRW;
            public bool HasSeekLinkTraversal;

            [NativeDisableContainerSafetyRestriction]
            NativeArray<int> m_IndicesScratch;
            [NativeDisableContainerSafetyRestriction]
            NativeList<float3> m_NextCornersScratch;

            public bool OnlyApplyPendingPaths;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                if (!m_IndicesScratch.IsCreated)
                {
                    m_NextCornersScratch = new NativeList<float3>(Allocator.Temp);
                    m_IndicesScratch = new NativeArray<int>(8, Allocator.Temp);
                    HasOnLinkTraversal = chunk.Has<LinkTraversal>();
                    HasSeekLinkTraversal = chunk.Has<LinkTraversalSeek>();
                }

                unsafe
                {
                    var localTransforms = (LocalTransform*) chunk.GetNativeArray(ref LocalTransformTypeHandleRO).GetUnsafeReadOnlyPtr();
                    var movementStates = (MovementState*) chunk.GetNativeArray(ref MovementStateTypeHandleRW).GetUnsafePtr();
                    var shapes = (AgentShape*) chunk.GetNativeArray(ref ShapeTypeHandleRO).GetUnsafeReadOnlyPtr();
                    var bodies = (AgentBody*) chunk.GetNativeArray(ref BodyTypeHandleRW).GetUnsafePtr();
                    var managedStates = chunk.GetManagedComponentAccessor(ref ManagedStateTypeHandleRW, (EntityManager) EntityManagerHandle.Target);
                    var linkTraversal = HasOnLinkTraversal ? chunk.GetEnabledMask(ref OnLinkTraversalTypeHandleRW) : default;
                    var seekLinkTraversal = HasOnLinkTraversal ? (LinkTraversalSeek*) chunk.GetNativeArray(ref SeekTraversalTypeHandleRW).GetUnsafeReadOnlyPtr(): default;

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        if (bodies[i].IsStopped)
                            continue;

                        var agentCylinderShape = new AgentCylinderShape
                        {
                            height = shapes[i].Height,
                            radius = shapes[i].Radius,
                        };

                        var destinationPoint = new DestinationPoint
                        {
                            destination = bodies[i].Destination,
                            facingDirection = bodies[i].Force,
                        };

                        var agentMovementPlane = new AgentMovementPlane(shapes[i].Orentation);

                        // TODO: Astar
                        var movementSetting = new MovementSettings
                        {
                            stopDistance = 0.5f
                        };

                        ref var transform = ref localTransforms[i];
                        ref var body = ref bodies[i];

                        var managedState = managedStates[i];

                        if (OnlyApplyPendingPaths)
                        {
                            JobRepairPath.Execute(
                                ref localTransforms[i],
                                ref movementStates[i],
                                ref agentCylinderShape,
                                ref agentMovementPlane,
                                ref destinationPoint,
                                default,
                                managedStates[i],
                                in movementSetting,
                                m_NextCornersScratch,
                                ref m_IndicesScratch,
                                Allocator.Temp,
                                true
                                );
                            continue;
                        }

                        if (HasOnLinkTraversal)
                        {
                            var linkTraversalMask = linkTraversal.GetEnabledRefRW<LinkTraversal>(i);
                            if (linkTraversalMask.ValueRO)
                                continue;
                        }

                        JobRepairPath.Execute(
                            ref localTransforms[i],
                            ref movementStates[i],
                            ref agentCylinderShape,
                            ref agentMovementPlane,
                            ref destinationPoint,
                            default,
                            managedStates[i],
                            in movementSetting,
                            m_NextCornersScratch,
                            ref m_IndicesScratch,
                            Allocator.Temp, // TODO: Astar move to scratch buffer?
                            false
                            );

                        if (HasOnLinkTraversal &&
                            movementStates[i].reachedEndOfPart &&
                            managedState.pathTracer.isNextPartValidLink)
                        {
                            var linkTraversalMask = linkTraversal.GetEnabledRefRW<LinkTraversal>(i);
                            linkTraversalMask.ValueRW = true;

                            if (HasSeekLinkTraversal)
                            {
#if MODULE_ASTAR_5_2_0_OR_NEWER
                                var linkInfo = RepairPathSystem.NextLinkToTraverse(managedState);
#else
                                var linkInfo = FollowerControlSystem.NextLinkToTraverse(managedState);
#endif
                                managedState.PopNextLinkFromPath();
                                seekLinkTraversal[i] = new LinkTraversalSeek
                                {
                                    Start = new Portal(linkInfo.relativeStart),
                                    End = new Portal(linkInfo.relativeEnd),
                                };
                            }

                            movementStates[i].hierarchicalNodeIndex = -1;

                            continue;
                        }

                        // Do not traverse stale path
                        if (managedState.pathTracer.isStale)
                            continue;

                        body.Force = math.normalizesafe(movementStates[i].nextCorner - localTransforms[i].Position);
                        body.RemainingDistance = movementStates[i].remainingDistanceToEndOfPart;
                    }
                }

                m_NextCornersScratch.Dispose();
                m_IndicesScratch.Dispose();
            }
        }
    }
}
#endif
