using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [RequireMatchingQueriesForUpdate()]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class BuilderSystem : SystemBase
    {
        BuildConfirmation m_BuildConfirmation;
        BuildActionBar m_BuildActionBar;

        protected override void OnCreate()
        {
            m_BuildConfirmation = GameObject.FindObjectOfType<BuildConfirmation>(true);
            m_BuildActionBar = GameObject.FindObjectOfType<BuildActionBar>(true);
        }

        protected override void OnUpdate()
        {
            if (m_BuildConfirmation == null)
                m_BuildConfirmation = GameObject.FindObjectOfType<BuildConfirmation>(true);
            if (m_BuildConfirmation == null)
            {
                Debug.LogError("Failed to find BuildConfirmation game object in the scene!");
                return;
            }

            if (m_BuildActionBar == null)
                m_BuildActionBar = GameObject.FindObjectOfType<BuildActionBar>(true);
            if (m_BuildActionBar == null)
            {
                Debug.LogError("Failed to find m_BuildActionBar game object in the scene!");
                return;
            }

            // Check, if build placement is set
            if (!m_BuildConfirmation.IsValid)
                return;

            if (!m_BuildActionBar.CheckAndResetButtonState(out var state))
                return;

            var entity = SystemAPI.GetSingletonEntity<Builder>();
            var builder = EntityManager.GetComponentData<Builder>(entity);

            switch (state)
            {
                case BuildActionBar.State.ArcherTower:
                    {
                        var building = EntityManager.Instantiate(builder.ArcherTowerPrefab);
                        EntityManager.SetComponentData(building, new LocalTransform
                        {
                            Position = m_BuildConfirmation.Position,
                            Rotation = quaternion.identity,
                            Scale = 1,
                        });
                        break;
                    }
                case BuildActionBar.State.BarracksTower:
                    {
                        var building = EntityManager.Instantiate(builder.BarracksTowerPrefab);
                        EntityManager.SetComponentData(building, new LocalTransform
                        {
                            Position = m_BuildConfirmation.Position,
                            Rotation = quaternion.identity,
                            Scale = 1,
                        });
                        break;
                    }
            }

            // Avoid building two times
            m_BuildConfirmation.Hide();
        }
    }
}
