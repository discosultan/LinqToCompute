using System.Numerics;
using Xunit;

namespace LinqToCompute.Tests
{
    public class Matrix4x4Test
    {
        [Fact]
        public void MemoryLayout()
        {
            Matrix4x4[] input =
            {
                new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                new Matrix4x4(17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32)
            };
            Matrix4x4[] expectedOutput =
            {
                new Matrix4x4(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32),
                new Matrix4x4(18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48)
            };

            Matrix4x4[] output = input.AsCompute().Select(x => x + new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Negate()
        {
            var m = Matrix4x4.Identity;

            Matrix4x4[] input = { m };
            Matrix4x4[] expectedOutput = { -m };

            Matrix4x4[] output1 = input.AsCompute().Select(x => Matrix4x4.Negate(x)).ToArray();
            Matrix4x4[] output2 = input.AsCompute().Select(x => -x).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Multiply()
        {
            var m = Matrix4x4.Identity;
            var mMultiply = Matrix4x4.CreateScale(2.0f);

            Matrix4x4[] input = { m };
            Matrix4x4[] expectedOutput = { m * mMultiply };

            Matrix4x4[] output1 = input.AsCompute().Select(x => Matrix4x4.Multiply(x, mMultiply)).ToArray();
            Matrix4x4[] output2 = input.AsCompute().Select(x => x * mMultiply).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void MultiplyScalar()
        {
            var m = Matrix4x4.Identity;
            const float fMultiply = 2.0f;

            Matrix4x4[] input = { m };
            Matrix4x4[] expectedOutput = { m * fMultiply };

            Matrix4x4[] output1 = input.AsCompute().Select(x => Matrix4x4.Multiply(x, fMultiply)).ToArray();
            Matrix4x4[] output2 = input.AsCompute().Select(x => x * fMultiply).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Add()
        {
            var m = Matrix4x4.Identity;

            Matrix4x4[] input = { m };
            Matrix4x4[] expectedOutput = { Matrix4x4.Identity * 2 };

            Matrix4x4[] output1 = input.AsCompute().Select(x => Matrix4x4.Add(x, Matrix4x4.Identity)).ToArray();
            Matrix4x4[] output2 = input.AsCompute().Select(x => x + Matrix4x4.Identity).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Subtract()
        {
            var m = Matrix4x4.Identity * 2;

            Matrix4x4[] input = { m };
            Matrix4x4[] expectedOutput = { m - Matrix4x4.Identity };

            Matrix4x4[] output1 = input.AsCompute().Select(x => Matrix4x4.Subtract(x, Matrix4x4.Identity)).ToArray();
            Matrix4x4[] output2 = input.AsCompute().Select(x => x - Matrix4x4.Identity).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }
    }
}
