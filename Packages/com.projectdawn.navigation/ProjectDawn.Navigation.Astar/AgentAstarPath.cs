#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Unity.Entities;
using Pathfinding.ECS;

namespace ProjectDawn.Navigation.Astar
{
    /// <summary>
    /// Agent uses A* Pathfinding Project path.
    /// </summary>
    [System.Serializable]
    public struct AgentAstarPath : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Policy for how often to recalculate an agent's path.
        /// </summary>
        public AutoRepathPolicy AutoRepath;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static AgentAstarPath Default => new()
        {
            AutoRepath = AutoRepathPolicy.Default,
        };
    }
}
#endif
