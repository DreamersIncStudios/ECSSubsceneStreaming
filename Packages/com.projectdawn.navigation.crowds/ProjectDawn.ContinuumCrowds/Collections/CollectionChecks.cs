// Copied from https://assetstore.unity.com/packages/tools/utilities/dots-plus-227492

using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProjectDawn.ContinuumCrowds
{
#if UNITY_COLLECTIONS_2_0_0
    [GenerateTestsForBurstCompatibility]
#else
    [GenerateTestsForBurstCompatibility]
#endif
    public static class CollectionChecks
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckIndexInRange(int index, int length)
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"Index {index} must be positive.");

            if (index >= length)
                throw new IndexOutOfRangeException($"Index {index} is out of range in container of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckCapacityInRange(int capacity, int length)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException($"Capacity {capacity} must be positive.");

            if (capacity < length)
                throw new ArgumentOutOfRangeException($"Capacity {capacity} is out of range in container of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException($"Capacity {capacity} must be positive.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckPositive(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static unsafe void CheckNull<T>(T* listData) where T : unmanaged
        {
            if (listData == null)
                throw new ArgumentException($"{nameof(T)} has yet to be created or has been destroyed!");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard] // Must use BurstDiscard because UnsafeUtility.IsUnmanaged is not burstable.
        //[NotBurstCompatible  /* Used only for debugging. */]
        public static void CheckIsUnmanaged<T>()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                throw new ArgumentException($"{typeof(T)} used in native collection is not blittable, not primitive, or contains a type tagged as NativeContainer");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckAllocator(AllocatorManager.AllocatorHandle allocator)
        {
            if (!ShouldDeallocate(allocator))
                throw new ArgumentException($"Allocator {allocator} must not be None or Invalid");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public unsafe static void CheckReinterpret<T>(int size) where T : unmanaged
        {
            int typeSize = sizeof(T);
            if (typeSize != size)
                throw new ArgumentException($"Can no reinterpret type {typeof(T).Name} with size {typeSize} to size {size}");
        }

        public static bool ShouldDeallocate(AllocatorManager.AllocatorHandle allocator)
        {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator.ToAllocator > Allocator.None;
        }
    }
}
