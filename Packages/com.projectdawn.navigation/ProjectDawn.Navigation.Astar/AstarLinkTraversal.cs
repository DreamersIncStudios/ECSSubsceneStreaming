#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Pathfinding.ECS;
using Pathfinding;
using Unity.Mathematics;

namespace ProjectDawn.Navigation.Astar
{
    public enum AstarLinkTraversalMode
    {
        None,
        [UnityEngine.Tooltip("Uses agent locomotion to traverse links.")]
        Seeking,
        [UnityEngine.Tooltip("Uses A* Pathfinding Project link traversal logic. Check https://www.arongranberg.com/astar/docs/offmeshlinks2.html for more details.")]
        StateMachine,
        Custom,
    }

    public class AstarLinkTraversalStateMachine : ManagedAgentOffMeshLinkTraversal
    {
        public AgentOffMeshLinkTraversal link;
    }

    public class AstarLinkTraversalContext : AgentOffMeshLinkTraversalContext
    {
        public AstarLinkTraversalContext(OffMeshLinks.OffMeshLinkConcrete concreteLink) : base(concreteLink)
        {
        }

        /// <summary>
        /// Move towards a point while ignoring the navmesh.
        /// This method should be called repeatedly until the returned <see cref="MovementTarget.reached"/> property is true.
        ///
        /// Returns: A <see cref="MovementTarget"/> struct which can be used to check if the target has been reached.
        ///
        /// Note: This method completely ignores the navmesh. It also overrides local avoidance, if enabled (other agents will still avoid it, but this agent will not avoid other agents).
        ///
        /// TODO: The gravity property is not yet implemented. Gravity is always applied.
        /// </summary>
        /// <param name="position">The position to move towards.</param>
        /// <param name="rotation">The rotation to rotate towards.</param>
        /// <param name="gravity">If true, gravity will be applied to the agent.</param>
        /// <param name="slowdown">If true, the agent will slow down as it approaches the target.</param>
        public override MovementTarget MoveTowards(float3 position, quaternion rotation, bool gravity, bool slowdown)
        {
            // If rotation smoothing was enabled, it could cause a very slow convergence to the target rotation.
            // Therefore, we disable it here.
            // The agent will try to remove its remaining rotation smoothing offset as quickly as possible.
            // After the off-mesh link is traversed, the rotation smoothing will be automatically restored.
            DisableRotationSmoothing();

            var dirInPlane = movementPlane.ToPlane(position - transform.Position);
            var remainingDistance = math.length(dirInPlane);

            movementControl = new MovementControl
            {
                targetPoint = position,
                endOfPath = position,
                hierarchicalNodeIndex = -1,
                overrideLocalAvoidance = true,
                targetRotationOffset = 0,
                rotationSpeed = math.radians(movementSettings.follower.rotationSpeed),
            };

            return new MovementTarget(remainingDistance <= 0.1f);
        }
    }
}
#endif
