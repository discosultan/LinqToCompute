using System;
using System.Collections.Generic;

namespace LinqToCompute.Tests
{
    public class FloatComparer : IEqualityComparer<float>
    {
        public static FloatComparer Default { get; } = new FloatComparer();

        private const float Precision = 0.0001f;

        public bool Equals(float x, float y) => Math.Abs(x - y) < Precision;

        public int GetHashCode(float value) => value.GetHashCode();
    }
}