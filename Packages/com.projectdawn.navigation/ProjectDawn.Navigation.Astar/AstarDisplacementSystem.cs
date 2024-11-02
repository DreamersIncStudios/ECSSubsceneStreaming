#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using static Unity.Entities.SystemAPI;
using Unity.Mathematics;
using Pathfinding.ECS;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.Navigation.Astar
{
    /// <summary>
    /// System that forces agents to stay within NavMesh surface.
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentDisplacementSystemGroup))]
    [UpdateAfter(typeof(AgentColliderSystem))]
    public partial struct AstarDisplacementSystem : ISystem
    {
        EntityQuery m_Query;
        GCHandle m_EntityManagerHandle;

        ComponentTypeHandle<LocalTransform> LocalTransformTypeHandleRO;
        ComponentTypeHandle<MovementState> MovementStateTypeHandleRW;
        ComponentTypeHandle<ManagedState> ManagedStateTypeHandleRW;
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
                .WithNone<LinkTraversal>()
                .Build();

            LocalTransformTypeHandleRO = state.GetComponentTypeHandle<LocalTransform>(true);
            MovementStateTypeHandleRW = state.GetComponentTypeHandle<MovementState>(false);
            ManagedStateTypeHandleRW = state.EntityManager.GetComponentTypeHandle<ManagedState>(false);
            ShapeTypeHandleRO = state.GetComponentTypeHandle<AgentShape>(true);
            BodyTypeHandleRW = state.GetComponentTypeHandle<AgentBody>(false);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            m_EntityManagerHandle.Free();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            LocalTransformTypeHandleRO.Update(ref state);
            MovementStateTypeHandleRW.Update(ref state);
            ManagedStateTypeHandleRW.Update(ref state);
            ShapeTypeHandleRO.Update(ref state);
            BodyTypeHandleRW.Update(ref state);

            state.Dependency = new AstarDisplacementJob
            {
                EntityManagerHandle = m_EntityManagerHandle,

                LocalTransformTypeHandleRO = LocalTransformTypeHandleRO,
                MovementStateTypeHandleRW = MovementStateTypeHandleRW,
                ManagedStateTypeHandleRW = ManagedStateTypeHandleRW,
                ShapeTypeHandleRO = ShapeTypeHandleRO,
                BodyTypeHandleRW = BodyTypeHandleRW,
            }.ScheduleParallel(m_Query, state.Dependency);
        }

        struct AstarDisplacementJob : IJobChunk
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

            [NativeDisableContainerSafetyRestriction]
            NativeArray<int> m_IndicesScratch;
            [NativeDisableContainerSafetyRestriction]
            NativeList<float3> m_NextCornersScratch;

            static float3 ClampToNavmesh(float3 position, float3 closestOnNavmesh, in AgentCylinderShape shape, in AgentMovementPlane movementPlane)
            {
                // Don't clamp the elevation except to make sure it's not too far below the navmesh.
                var clamped2D = movementPlane.value.ToPlane(closestOnNavmesh, out float clampedElevation);
                movementPlane.value.ToPlane(position, out float currentElevation);
                currentElevation = math.max(currentElevation, clampedElevation - shape.height * 0.4f);
                position = movementPlane.value.ToWorld(clamped2D, currentElevation);
                return position;
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                if (!m_IndicesScratch.IsCreated)
                {
                    m_NextCornersScratch = new NativeList<float3>(Allocator.Temp);
                    m_IndicesScratch = new NativeArray<int>(8, Allocator.Temp);
                }

                unsafe
                {
                    var localTransforms = (LocalTransform*) chunk.GetNativeArray(ref LocalTransformTypeHandleRO).GetUnsafeReadOnlyPtr();
                    var movementStates = (MovementState*) chunk.GetNativeArray(ref MovementStateTypeHandleRW).GetUnsafePtr();
                    var shapes = (AgentShape*) chunk.GetNativeArray(ref ShapeTypeHandleRO).GetUnsafeReadOnlyPtr();
                    var bodies = (AgentBody*) chunk.GetNativeArray(ref BodyTypeHandleRW).GetUnsafePtr();
                    var managedStates = chunk.GetManagedComponentAccessor(ref ManagedStateTypeHandleRW, (EntityManager) EntityManagerHandle.Target);

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

                        var previousCloesestOnNavmesh = movementStates[i].closestOnNavmesh;
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
                            false
                            );

                        localTransforms[i].Position = movementStates[i].closestOnNavmesh;
                        //localTransforms[i].Position = ClampToNavmesh(localTransforms[i].Position, movementStates[i].closestOnNavmesh, agentCylinderShape, agentMovementPlane);
                    }
                }

                m_NextCornersScratch.Dispose();
                m_IndicesScratch.Dispose();
            }
        }
    }
}
#endif
