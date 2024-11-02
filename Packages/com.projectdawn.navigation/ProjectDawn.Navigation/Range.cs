using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    [System.Serializable]
    public struct Range
    {
        public float Start;
        public float End;

        public float Length => End - Start;

        public Range(float start, float end)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (start > end)
                throw new System.ArgumentException("Start can not be greated than end");
#endif
            Start = start;
            End = end;
        }

        public bool Contains(float value) => Start <= value && End <= value;
    }

    public static class RangeExt
    {
        public static float NextFloat(this ref Random random, Range range) => random.NextFloat(range.Start, range.End);
    }
}
