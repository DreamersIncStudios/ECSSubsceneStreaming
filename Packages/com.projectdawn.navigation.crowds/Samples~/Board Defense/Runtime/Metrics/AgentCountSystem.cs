using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [RequireMatchingQueriesForUpdate()]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class AgentCountSystem : SystemBase
    {
        EntityQuery m_AgentQuery;
        TextAgentCount m_AgentCount;

        protected override void OnCreate()
        {
            m_AgentQuery = this.GetEntityQuery(typeof(Agent));
            m_AgentCount = GameObject.FindObjectOfType<TextAgentCount>(true);
        }

        protected override void OnUpdate()
        {
            if (m_AgentCount == null)
            {
                return;
            }

            int agentCount = m_AgentQuery.CalculateEntityCount();
            m_AgentCount.UpdateCount(agentCount);
        }
    }
}
