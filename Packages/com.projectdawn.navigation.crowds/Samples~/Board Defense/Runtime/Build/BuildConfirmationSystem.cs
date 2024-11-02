using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectDawn.Navigation.Sample.BoardDefense
{
    [RequireMatchingQueriesForUpdate()]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class BuildSystem : SystemBase
    {
        BuildConfirmation m_BuildConfirmation;

        protected override void OnCreate()
        {
            m_BuildConfirmation = GameObject.FindObjectOfType<BuildConfirmation>(true);
        }

        protected override void OnUpdate()
        {
            if (m_BuildConfirmation == null)
            {
                return;
            }

            var entity = SystemAPI.GetSingletonEntity<CrowdSurfaceWorld>();
            var surface = EntityManager.GetComponentData<CrowdSurfaceWorld>(entity);
            var world = surface.World;

            if (!Input.GetMouseButtonUp(0))
                return;

            // Skip, if UI is clicked for desktop
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // Skip, if UI is clicked for mobile
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!world.RaycastHeightField(ray.origin, ray.direction, out float3 hit))
            {
                m_BuildConfirmation.Hide();
                return;
            }

            if (!world.TryGetCell(hit, out int2 cell))
            {
                m_BuildConfirmation.Hide();
                return;
            }

            if (!world.IsValidQuad(cell - 1, cell + 1))
            {
                m_BuildConfirmation.Hide();
                return;
            }

            float3 position = world.GetCellPosition(cell);
            m_BuildConfirmation.Show(position);
        }
    }
}
