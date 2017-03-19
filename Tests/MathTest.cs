using System;
using LinqToCompute.Utilities;
using Xunit;

namespace LinqToCompute.Tests
{
    public class MathTest
    {
        [Fact]
        public void Sin()
        {
            const float v = 1.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Sin(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Sin(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Cos()
        {
            const float v = 1.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Cos(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Cos(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Tan()
        {
            const float v = 1.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Tan(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Tan(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Abs()
        {
            const int v = 1;

            int[] input = { v };
            int[] expectedOutput = { Math.Abs(v) };

            int[] output = input.AsComputeQuery().Select(x => Math.Abs(x)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Log()
        {
            const float v = 1.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Log(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Log(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Floor()
        {
            const float v = 1.5f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Floor(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Floor(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void FloorToInt()
        {
            const float v = 1.5f;

            float[] input = { v };
            int[] expectedOutput = { (int)Math.Floor(v) };

            int[] output = input.AsComputeQuery().Select(x => (int)Math.Floor(x)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Ceil()
        {
            const float v = 0.5f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Ceiling(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Ceiling(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Round()
        {
            const float v = 0.75f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Round(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Round(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Exp()
        {
            const float v = 1.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Exp(v) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Exp(x)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Pow()
        {
            const float v = 2.0f;
            const float power = 2.0f;

            float[] input = { v };
            float[] expectedOutput = { (float)Math.Pow(v, power) };

            float[] output = input.AsComputeQuery().Select(x => (float)Math.Pow(x, power)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Clamp()
        {
            const float v = 1.0f;
            const float min = 2.0f;
            const float max = 3.0f;

            float[] input = { v };
            float[] expectedOutput = { ComputeMath.Clamp(v, min, max) };

            float[] output = input.AsComputeQuery().Select(x => ComputeMath.Clamp(x, min, max)).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }
    }
}
