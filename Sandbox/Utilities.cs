using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using LinqToCompute.Utilities;

namespace Sandbox
{
    public class FloatComparer : IEqualityComparer<float>
    {
        public static FloatComparer Default { get; } = new FloatComparer();

        private const float Precision = 0.00001f;

        public bool Equals(float x, float y) => Math.Abs(x - y) < Precision;

        public int GetHashCode(float value) => value.GetHashCode();
    }

    public class Vector2Comparer : IEqualityComparer<Vector2>
    {
        public static Vector2Comparer Default { get; } = new Vector2Comparer();

        private const float Precision = 1f;

        public bool Equals(Vector2 x, Vector2 y) =>
            Math.Abs(x.X - y.X) < Precision &&
            Math.Abs(x.Y - y.Y) < Precision;

        public int GetHashCode(Vector2 value) => value.GetHashCode();
    }

    public static class EnumerableExtensions
    {
        public static int MinIndexBy<T, TSelector>(this IEnumerable<T> sequence, Func<T, TSelector> selector)
            where TSelector : IComparable<TSelector>
        {
            int maxIndex = -1;
            var maxValue = default(TSelector);

            int index = 0;
            foreach (T value in sequence)
            {
                TSelector selected = selector(value);
                if (selected.CompareTo(maxValue) < 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = selected;
                }
                index++;
            }
            return maxIndex;
        }
    }

    public class Profiler : ComputeProfiler
    {
        public Profiler(string name, bool isGpu)
        {
            Name = name;
            IsGpu = isGpu;
        }

        public string Name { get; }
        public Stopwatch Total { get; } = new Stopwatch();
        public bool IsGpu { get; }

        public TimeSpan TotalElapsed => Total.Elapsed;
    }
}