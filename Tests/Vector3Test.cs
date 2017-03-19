using System.Numerics;
using Xunit;

namespace LinqToCompute.Tests
{
    public class Vector3Test
    {
        [Fact]
        public void Noop()
        {
            Vector3[] input = { new Vector3(0, 1, 2), new Vector3(3, 4, 5) };
            Vector3[] expectedOutput = { new Vector3(0, 1, 2), new Vector3(3, 4, 5) };

            Vector3[] output = input.AsComputeQuery().Select(x => x).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void MemoryLayout()
        {
            Vector3[] input = { new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
            Vector3[] expectedOutput = { new Vector3(2, 4, 6), new Vector3(5, 7, 9) };

            Vector3[] output = input.AsComputeQuery().Select(x => x + new Vector3(1, 2, 3)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Add()
        {
            var v = new Vector3(0, 1, 2);
            var add = new Vector3(1, 2, 3);

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v + add };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x + add).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Add(x, add)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Subtract()
        {
            var v = new Vector3(1, 2, 3);
            var subtract = new Vector3(0, 1, 2);

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v - subtract };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x - subtract).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Subtract(x, subtract)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Multiply()
        {
            var v = new Vector3(1, 2, 3);
            var multiply = new Vector3(0, 1, 2);

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v * multiply };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x * multiply).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void MultiplyScalar()
        {
            var v = new Vector3(1, 2, 3);
            const float multiply = 2.0f;

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v * multiply };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x * multiply).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Divide()
        {
            var v = new Vector3(0, 4, 9);
            var divide = new Vector3(1, 2, 3);

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v / divide };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x / divide).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void DivideScalar()
        {
            var v = new Vector3(0, 4, 6);
            const float divide = 2.0f;

            Vector3[] input = { v };
            Vector3[] expectedOutput = { v / divide };

            Vector3[] output1 = input.AsComputeQuery().Select(x => x / divide).ToArray();
            Vector3[] output2 = input.AsComputeQuery().Select(x => Vector3.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Dot()
        {
            var v = new Vector3(1, 2, 3);
            var dot = new Vector3(-1, 2, 3);

            Vector3[] input = { v };
            float[] expectedOutput = { Vector3.Dot(v, dot) };

            float[] output = input.AsComputeQuery().Select(x => Vector3.Dot(x, dot)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Length()
        {
            var v = new Vector3(1, 2, 3);

            Vector3[] input = { v };
            float[] expectedOutput = { v.Length() };

            float[] output = input.AsComputeQuery().Select(x => x.Length()).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Cross()
        {
            var v = new Vector3(1, 2, 3);
            var cross = new Vector3(-1, 2, 3);

            Vector3[] input = { v };
            Vector3[] expectedOutput = { Vector3.Cross(v, cross) };

            Vector3[] output = input.AsComputeQuery().Select(x => Vector3.Cross(x, cross)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Distance()
        {
            var v = new Vector3(1, 2, 3);
            var distance = new Vector3(-1, 2, 3);

            Vector3[] input = { v };
            float[] expectedOutput = { Vector3.Distance(v, distance) };

            float[] output = input.AsComputeQuery().Select(x => Vector3.Distance(x, distance)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Clamp()
        {
            var v = new Vector3(1, 2, 3);
            var min = -Vector3.One;
            var max = Vector3.One;

            Vector3[] input = { v };
            Vector3[] expectedOutput = { Vector3.Clamp(v, min, max) };

            Vector3[] output = input.AsComputeQuery().Select(x => Vector3.Clamp(x, min, max)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Lerp()
        {
            var v = new Vector3(0, 2, 4);
            var lerp = Vector3.Zero;
            const float amount = 0.5f;

            Vector3[] input = { v };
            Vector3[] expectedOutput = { Vector3.Lerp(v, lerp, amount) };

            Vector3[] output = input.AsComputeQuery().Select(x => Vector3.Lerp(x, lerp, amount)).ToArray();

            Assert.Equal(expectedOutput, output);
        }
    }
}
