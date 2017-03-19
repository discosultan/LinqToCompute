using System.Numerics;
using Xunit;

namespace LinqToCompute.Tests
{
    public class Vector2Test
    {
        [Fact]
        public void MemoryLayout()
        {
            Vector2[] input = { new Vector2(1, 2), new Vector2(3, 4) };
            Vector2[] expectedOutput = { new Vector2(2, 4), new Vector2(4, 6) };

            Vector2[] output = input.AsComputeQuery().Select(x => x + new Vector2(1, 2)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Add()
        {
            var v = new Vector2(0, 1);
            var add = new Vector2(1, 2);

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v + add };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x + add).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Add(x, add)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Swap()
        {
            var v = new Vector2(1, 2);

            Vector2[] input = { v };
            Vector2[] expectedOutput = { new Vector2(v.Y, v.X) };

            Vector2[] output = input.AsComputeQuery().Select(x => new Vector2(x.Y, x.X)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AddComponents()
        {
            var v = new Vector2(1, 2);

            Vector2[] input = { v };
            float[] expectedOutput = { v.X + v.Y };

            float[] output = input.AsComputeQuery().Select(x => x.X + x.Y).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Subtract()
        {
            var v = new Vector2(1, 2);
            var subtract = new Vector2(0, 1);

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v - subtract };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x - subtract).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Subtract(x, subtract)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Multiply()
        {
            var v = new Vector2(1, 2);
            var multiply = new Vector2(0, 1);

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v * multiply };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x * multiply).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void MultiplyScalar()
        {
            var v = new Vector2(1, 2);
            const float multiply = 2.0f;

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v * multiply };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x * multiply).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Multiply(x, multiply)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Divide()
        {
            var v = new Vector2(0, 4);
            var divide = new Vector2(1, 2);

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v / divide };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x / divide).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void DivideScalar()
        {
            var v = new Vector2(0, 4);
            const float divide = 2.0f;

            Vector2[] input = { v };
            Vector2[] expectedOutput = { v / divide };

            Vector2[] output1 = input.AsComputeQuery().Select(x => x / divide).ToArray();
            Vector2[] output2 = input.AsComputeQuery().Select(x => Vector2.Divide(x, divide)).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Dot()
        {
            var v = new Vector2(1, 2);
            var dot = new Vector2(-1, 2);

            Vector2[] input = { v };
            float[] expectedOutput = { Vector2.Dot(v, dot) };

            float[] output = input.AsComputeQuery().Select(x => Vector2.Dot(x, dot)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Length()
        {
            var v = new Vector2(1, 2);

            Vector2[] input = { v };
            float[] expectedOutput = { v.Length() };

            float[] output = input.AsComputeQuery().Select(x => x.Length()).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        [Fact]
        public void Distance()
        {
            var v = new Vector2(1, 2);
            var distance = new Vector2(-1, 2);

            Vector2[] input = { v };
            float[] expectedOutput = { Vector2.Distance(v, distance) };

            float[] output = input.AsComputeQuery().Select(x => Vector2.Distance(x, distance)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Normalize()
        {
            var v = new Vector2(1, 2);
            var distance = new Vector2(-1, 2);

            Vector2[] input = { v };
            float[] expectedOutput = { Vector2.Distance(v, distance) };

            float[] output = input.AsComputeQuery().Select(x => Vector2.Distance(x, distance)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Clamp()
        {
            var v = new Vector2(1, 2);
            var min = -Vector2.One;
            var max = Vector2.One;

            Vector2[] input = { v };
            Vector2[] expectedOutput = { Vector2.Clamp(v, min, max) };

            Vector2[] output = input.AsComputeQuery().Select(x => Vector2.Clamp(x, min, max)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Lerp()
        {
            var v = new Vector2(0, 2);
            var lerp = Vector2.Zero;
            const float amount = 0.5f;

            Vector2[] input = { v };
            Vector2[] expectedOutput = { Vector2.Lerp(v, lerp, amount) };

            Vector2[] output = input.AsComputeQuery().Select(x => Vector2.Lerp(x, lerp, amount)).ToArray();

            Assert.Equal(expectedOutput, output);
        }
    }
}
