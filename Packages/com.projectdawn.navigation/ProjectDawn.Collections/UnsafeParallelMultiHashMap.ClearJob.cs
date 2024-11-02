
using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    [BurstCompile]
    public unsafe static class UnsafeParallelMultiHashMapExt
    {
        [BurstCompile]
        public static JobHandle Clear<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> value, int jobCount, JobHandle dependency)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return value.m_MultiHashMapData.Clear(jobCount, dependency);
        }

        [BurstCompile]
        public static JobHandle Clear<TKey, TValue>(this UnsafeParallelMultiHashMap<TKey, TValue> value, int jobCount, JobHandle dependency)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = value.m_Buffer;

            var clearBuckets =  new HashMapClearBucketsJob
            {
                m_Buffer = data,
                m_JobCount = jobCount,
            }.Schedule(jobCount, 1, dependency);

            var clearNext = new HashMapClearNextJob
            {
                m_Buffer = data,
                m_JobCount = jobCount,
            }.Schedule(jobCount, 1, dependency);

            var clear = new HashMapClearJob
            {
                m_Buffer = data,
            }.Schedule(dependency);

            return JobHandle.CombineDependencies(clearBuckets, clearNext, clear);
        }

        [BurstCompile]
        internal struct HashMapClearBucketsJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;
            internal int m_JobCount;

            public void Execute(int index)
            {
                var data = m_Buffer;
                var capacity = (data->bucketCapacityMask + 1) * 4;

                var stride = (capacity + m_JobCount - 1) / m_JobCount;

                var start = index * stride;
                var end = math.min(start + stride, capacity);

                UnsafeUtility.MemSet(data->buckets + start, 0xff, end - start);
            }
        }

        [BurstCompile]
        internal struct HashMapClearNextJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;
            internal int m_JobCount;

            public void Execute(int index)
            {
                var data = m_Buffer;
                var capacity = (data->keyCapacity) * 4;

                var stride = (capacity + m_JobCount - 1) / m_JobCount;

                var start = index * stride;
                var end = math.min(start + stride, capacity);

                UnsafeUtility.MemSet(data->next + start, 0xff, end - start);
            }
        }

        [BurstCompile]
        internal struct HashMapClearJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;

            public void Execute()
            {
                var data = m_Buffer;

#if UNITY_2022_2_14F1_OR_NEWER
                int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
                int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
                for (int tls = 0; tls < maxThreadCount; ++tls)
                {
                    data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] = -1;
                }

                data->allocatedIndexLength = 0;
            }
        }
    }
}
