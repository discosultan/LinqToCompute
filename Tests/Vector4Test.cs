using System.Numerics;
using Xunit;

namespace LinqToCompute.Tests
{
    public class Vector4Test
    {
        [Fact]
        public void MemoryLayout()
        {
            Vector4[] input = { new Vector4(1, 2, 3, 4), new Vector4(5, 6, 7, 8) };
            Vector4[] expectedOutput = { new Vector4(2, 4, 6, 8), new Vector4(6, 8, 10, 12) };

            Vector4[] output = input.AsCompute().Select(x => x + new Vector4(1, 2, 3, 4)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Add()
        {
            var v = new Vector4(0, 1, 2, 3);
            var add = new Vector4(1, 2, 3, 4);

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v + add };

            Vector4[] output1 = input.AsCompute().Select(x => x + add).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Add(x, add)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Subtract()
        {
            var v = new Vector4(1, 2, 3, 4);
            var subtract = new Vector4(0, 1, 2, 3);

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v - subtract };

            Vector4[] output1 = input.AsCompute().Select(x => x - subtract).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Subtract(x, subtract)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Multiply()
        {
            var v = new Vector4(1, 2, 3, 4);
            var multiply = new Vector4(0, 1, 2, 3);

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v * multiply };

            Vector4[] output1 = input.AsCompute().Select(x => x * multiply).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void MultiplyScalar()
        {
            var v = new Vector4(1, 2, 3, 4);
            const float multiply = 2.0f;

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v * multiply };

            Vector4[] output1 = input.AsCompute().Select(x => x * multiply).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Divide()
        {
            var v = new Vector4(0, 4, 9, 16);
            var divide = new Vector4(1, 2, 3, 4);

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v / divide };

            Vector4[] output1 = input.AsCompute().Select(x => x / divide).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void DivideScalar()
        {
            var v = new Vector4(0, 4, 6, 8);
            const float divide = 2.0f;

            Vector4[] input = { v };
            Vector4[] expectedOutput = { v / divide };

            Vector4[] output1 = input.AsCompute().Select(x => x / divide).ToArray();
            Vector4[] output2 = input.AsCompute().Select(x => Vector4.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Dot()
        {
            var v = new Vector4(1, 2, 3, 4);
            var dot = new Vector4(-1, 2, 3, 4);

            Vector4[] input = { v };
            float[] expectedOutput = { Vector4.Dot(v, dot) };

            float[] output = input.AsCompute().Select(x => Vector4.Dot(x, dot)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Length()
        {
            var v = new Vector4(1, 2, 3, 4);

            Vector4[] input = { v };
            float[] expectedOutput = { v.Length() };

            float[] output = input.AsCompute().Select(x => x.Length()).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Distance()
        {
            var v = new Vector4(1, 2, 3, 4);
            var distance = new Vector4(-1, 2, 3, 4);

            Vector4[] input = { v };
            float[] expectedOutput = { Vector4.Distance(v, distance) };

            float[] output = input.AsCompute().Select(x => Vector4.Distance(x, distance)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Clamp()
        {
            var v = new Vector4(1, 2, 3, 4);
            var min = -Vector4.One;
            var max = Vector4.One;

            Vector4[] input = { v };
            Vector4[] expectedOutput = { Vector4.Clamp(v, min, max) };

            Vector4[] output = input.AsCompute().Select(x => Vector4.Clamp(x, min, max)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Lerp()
        {
            var v = new Vector4(0, 2, 4, 8);
            var lerp = Vector4.Zero;
            const float amount = 0.5f;

            Vector4[] input = { v };
            Vector4[] expectedOutput = { Vector4.Lerp(v, lerp, amount) };

            Vector4[] output = input.AsCompute().Select(x => Vector4.Lerp(x, lerp, amount)).ToArray();

            Assert.Equal(expectedOutput, output);
        }
    }
}
