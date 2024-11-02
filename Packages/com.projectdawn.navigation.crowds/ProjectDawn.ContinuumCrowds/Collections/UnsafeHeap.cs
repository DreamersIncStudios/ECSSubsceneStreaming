// Based on https://www.geeksforgeeks.org/binary-heap/

// Copied from https://assetstore.unity.com/packages/tools/utilities/dots-plus-227492

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ProjectDawn.ContinuumCrowds
{
    internal sealed class UnsafeHeapDebugView<TKey, TValue> where TKey : unmanaged, IComparable<TKey>
        where TValue : unmanaged
    {
        UnsafeHeap<TKey, TValue> Data;

        public UnsafeHeapDebugView(UnsafeHeap<TKey, TValue> data)
        {
            Data = data;
        }

        public unsafe TValue[] Items
        {
            get
            {
                TValue[] result = new TValue[Data.Length];

                int index = 0;
                for (int i = 0; i < Data.Length; i++)
                {
                    result[index++] = Data.m_Values[i];
                }

                return result;
            }
        }
    }

    /// <summary>
    /// An unmanaged, resizable min heap, without any thread safety check features.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the element.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(UnsafeHeapDebugView<,>))]
    public unsafe struct UnsafeHeap<TKey, TValue>
        : IDisposable
        where TKey : unmanaged, IComparable<TKey>
        where TValue : unmanaged
    {
        /// <summary>
        /// Pointer to keys.
        /// </summary>
        internal TKey* m_Keys;

        /// <summary>
        /// Pointer to values.
        /// </summary>
        internal TValue* m_Values;

        /// <summary>
        /// Allocator.
        /// </summary>
        internal AllocatorManager.AllocatorHandle m_Allocator;

        int m_Capacity;

        int m_Length;

        /// <summary>
        /// Whether the heap is empty.
        /// </summary>
        /// <value>True if the heap is empty or the queue has not been constructed.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get => m_Length;
            set
            {
                if (value > m_Capacity)
                {
                    Resize(value);
                }
                else
                {
                    m_Length = value;
                }
            }
        }

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that can fit in the internal buffer.</value>
        public int Capacity
        {
            get => m_Capacity;
            set
            {
                SetCapacity(value);
            }
        }

        /// <summary>
        /// Whether this heap has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this heap has been allocated (and not yet deallocated).</value>
        public bool IsCreated => m_Keys != null && m_Values != null;

        /// <summary>
        /// Creates a new container with the specified initial capacity and type of memory allocation.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list. If the list grows larger than its capacity,
        /// the internal array is copied to a new, larger array.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public static UnsafeHeap<TKey, TValue>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeHeap<TKey, TValue>* data = AllocatorManager.Allocate<UnsafeHeap<TKey, TValue>>(allocator);

            data->m_Allocator = allocator;
            data->m_Keys = null;
            data->m_Values = null;
            data->m_Capacity = 0;
            data->m_Length = 0;
            data->Realloc(initialCapacity);

            return data;
        }

        /// <summary>
        /// Destroys container.
        /// </summary>
        public static void Destroy(UnsafeHeap<TKey, TValue>* data)
        {
            CollectionChecks.CheckNull(data);
            var allocator = data->m_Allocator;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
        }

        /// <summary>
        /// Initialized and returns an instance of heap.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHeap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Allocator = allocator;
            m_Keys = null;
            m_Values = null;
            m_Capacity = 0;
            m_Length = 0;
            Realloc(initialCapacity);
        }

        /// <summary>
        /// Adds new key and element to the heap.
        /// </summary>
        /// <param name="key">The key to be added.</param>
        /// <param name="value">The value to be added.</param>
        public bool Push(TKey key, TValue value)
        {
            var i = m_Length;

            if (m_Length + 1 > m_Capacity)
            {
                Resize(i + 1);
            }
            else
            {
                m_Length += 1;
            }

            m_Keys[i] = key;
            m_Values[i] = value;

            // Fix the min heap property if it is violated 
            while (i != 0 && m_Keys[i].CompareTo(m_Keys[Parent(i)]) == -1)
            {
                Swap(ref m_Keys[i], ref m_Keys[Parent(i)]);
                Swap(ref m_Values[i], ref m_Values[Parent(i)]);
                i = Parent(i);
            }
            return true;
        }

        /// <summary>
        /// Returns value with minimum key.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="value">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryPop(out (TKey, TValue) value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            value = Pop();
            return true;
        }

        /// <summary>
        /// Returns value with minimum key.
        /// </summary>
        /// <returns>Returns value with minimum key.</returns>
        public (TKey, TValue) Pop()
        {
            if (m_Length == 0)
                ThrowHeapEmpty();

            // Handle special case with length 1
            if (m_Length == 1)
            {
                m_Length = 0;
                return (m_Keys[0], m_Values[0]);
            }

            // Store the minimum value, 
            // and remove it from heap 
            TKey key = m_Keys[0];
            TValue value = m_Values[0];

            // Remove at swapback
            m_Keys[0] = m_Keys[m_Length - 1];
            m_Values[0] = m_Values[m_Length - 1];
            m_Length--;

            // Resort
            Heapify(0);

            return (key, value);
        }

        /// <summary>
        /// Returns value with minimum key.
        /// </summary>
        /// <returns>Returns value with minimum key.</returns>
        public TValue Peek()
        {
            if (m_Length == 0)
                ThrowHeapEmpty();

            return m_Values[0];
        }

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Length = 0;
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            if (CollectionChecks.ShouldDeallocate(m_Allocator))
            {
                AllocatorManager.Free(m_Allocator, m_Keys);
                AllocatorManager.Free(m_Allocator, m_Values);
                m_Allocator = AllocatorManager.Invalid;
            }

            m_Keys = null;
            m_Values = null;
            m_Capacity = 0;
            m_Length = 0;
        }

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var oldLength = m_Length;

            if (length > Capacity)
            {
                SetCapacity(length);
            }

            m_Length = length;

            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                UnsafeUtility.MemClear((byte*)m_Keys + oldLength * sizeof(TKey), num * sizeof(TKey));
                UnsafeUtility.MemClear((byte*)m_Values + oldLength * sizeof(TValue), num * sizeof(TValue));
            }
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
            CollectionChecks.CheckCapacityInRange(capacity, Length);

            var sizeOf = sizeof(TValue);
            var newCapacity = math.max(capacity, 64 / sizeOf);
            newCapacity = math.ceilpow2(newCapacity);

            if (newCapacity == Capacity)
            {
                return;
            }

            Realloc(newCapacity);
        }

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
            if (m_Capacity != m_Length)
            {
                Realloc(m_Length);
            }
        }

        /// <summary>
        /// Returns an array with a copy of all this heap map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(m_Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(result.GetUnsafePtr(), m_Keys, sizeof(TKey) * m_Length);
            return result;
        }

        /// <summary>
        /// Returns an array with a copy of all this heap map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(m_Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(result.GetUnsafePtr(), m_Keys, sizeof(TValue) * m_Length);
            return result;
        }

        void Realloc(int newCapacity)
        {
            CollectionChecks.CheckAllocator(m_Allocator);
            TKey* newKeys = null;
            TValue* newValues = null;

            if (newCapacity > 0)
            {
                newKeys = (TKey*)AllocatorManager.Allocate(m_Allocator, sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), newCapacity);
                newValues = (TValue*)AllocatorManager.Allocate(m_Allocator, sizeof(TValue), UnsafeUtility.AlignOf<TValue>(), newCapacity);

                if (m_Capacity > 0)
                {
                    var itemsToCopy = math.min(newCapacity, Capacity);
                    UnsafeUtility.MemCpy(newKeys, m_Keys, itemsToCopy * sizeof(TKey));
                    UnsafeUtility.MemCpy(newValues, m_Values, itemsToCopy * sizeof(TValue));
                }
            }

            AllocatorManager.Free(m_Allocator, m_Keys, Capacity);
            AllocatorManager.Free(m_Allocator, m_Values, Capacity);

            m_Keys = newKeys;
            m_Values = newValues;
            m_Capacity = newCapacity;
            m_Length = math.min(m_Length, newCapacity);
        }

        void Heapify(int handle)
        {
            int l = Left(handle);
            int r = Right(handle);

            int smallest = handle;
            if (l < m_Length && m_Keys[l].CompareTo(m_Keys[smallest]) == -1)
            {
                smallest = l;
            }
            if (r < m_Length && m_Keys[r].CompareTo(m_Keys[smallest]) == -1)
            {
                smallest = r;
            }

            if (smallest != handle)
            {
                Swap(ref m_Keys[handle], ref m_Keys[smallest]);
                Swap(ref m_Values[handle], ref m_Values[smallest]);
                Heapify(smallest);
            }
        }

        static int Parent(int handle) => (handle - 1) / 2;

        static int Left(int handle) => 2 * handle + 1;

        static int Right(int handle) => 2 * handle + 2;

        static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowHeapEmpty()
        {
            throw new InvalidOperationException("Trying to pop from an empty heap");
        }
    }
}
