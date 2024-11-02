using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    public class EntityBehaviour : MonoBehaviour
    {
        protected Entity m_Entity;

        public bool IsEntityCreated => m_Entity != Entity.Null;

        public Entity GetOrCreateEntity()
        {
            if (m_Entity != Entity.Null)
                return m_Entity;

            var world = World.DefaultGameObjectInjectionWorld;
            var manager = world.EntityManager;

            m_Entity = manager.CreateEntity();
            manager.AddComponentData(m_Entity, new EntityGuid(gameObject.GetInstanceID(), 0, 0, 0));
            manager.SetName(m_Entity, name);

            return m_Entity;
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var manager = world.EntityManager;
            manager.DestroyEntity(m_Entity);
        }

        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var manager = world.EntityManager;
            manager.SetEnabled(m_Entity, true);
        }

        void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var manager = world.EntityManager;
            manager.SetEnabled(m_Entity, false);
        }
    }
}
