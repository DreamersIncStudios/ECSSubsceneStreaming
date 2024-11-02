using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    public unsafe partial struct EntityManager : IEquatable<EntityManager>
    {
        // From https://forum.unity.com/threads/really-hoped-for-refrw-refro-getcomponentrw-ro-entity.1369275/
        /// <summary>
        /// Gets the value of a component for an entity associated with a system.
        /// </summary>
        /// <param name="system">The system handle.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A <see cref="RefRW{T}"/> struct of type T containing access to the component value.</returns>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the system isn't from thie world.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public RefRW<T> GetComponentDataRW<T>(Entity entity) where T : unmanaged, IComponentData
        {
            var access = GetUncheckedEntityDataAccess();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            var data = access->GetComponentDataRW_AsBytePointer(entity, typeIndex);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRW<T>(data, access->DependencyManager->Safety.GetSafetyHandle(typeIndex, false));
#else
            return new RefRW<T>(data);
#endif
        }
    }

    public struct OptionalBufferAccessor<T> where T : unmanaged, IBufferElementData
    {
        BufferAccessor<T> m_BufferAccessor;
        BufferTypeHandle<T> m_TypeHandle;

        public DynamicBuffer<T> this[int index] => m_BufferAccessor.Length != 0 ? m_BufferAccessor[index] : default;

        public OptionalBufferAccessor(BufferTypeHandle<T> handle)
        {
            m_BufferAccessor = default;
            m_TypeHandle = handle;
        }

        public void Update(in ArchetypeChunk chunk)
        {
            if (chunk.Has<T>())
                m_BufferAccessor = chunk.GetBufferAccessor(ref m_TypeHandle);
        }

        public bool TryGetBuffer(int index, out DynamicBuffer<T> buffer)
        {
            if (m_BufferAccessor.Length == 0)
            {
                buffer = default;
                return false;
            }

            buffer = m_BufferAccessor[index];
            return true;
        }
    }
}
