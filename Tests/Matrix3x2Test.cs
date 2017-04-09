using System.Numerics;
using Xunit;

namespace LinqToCompute.Tests
{
    public class Matrix3x2Test
    {
        [Fact]
        public void MemoryLayout()
        {
            Matrix3x2[] input = { new Matrix3x2(1, 2, 3, 4, 5, 6), new Matrix3x2(7, 8, 9, 10, 11, 12) };
            Matrix3x2[] expectedOutput = { new Matrix3x2(2, 4, 6, 8, 10, 12), new Matrix3x2(8, 10, 12, 14, 16, 18) };

            Matrix3x2[] output = input.AsCompute().Select(x => x + new Matrix3x2(1, 2, 3, 4, 5, 6)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Negate()
        {
            var m = Matrix3x2.Identity;

            Matrix3x2[] input = { m };
            Matrix3x2[] expectedOutput = { -m };

            Matrix3x2[] output1 = input.AsCompute().Select(x => Matrix3x2.Negate(x)).ToArray();
            Matrix3x2[] output2 = input.AsCompute().Select(x => -x).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact(Skip = "Undefined behavior? - number of columns of m1 must match number of rows of m2")]
        public void Multiply()
        {
            var m = Matrix3x2.Identity;
            var mMultiply = Matrix3x2.CreateScale(2.0f);

            Matrix3x2[] input = { m };
            Matrix3x2[] expectedOutput = { m * mMultiply };

            Matrix3x2[] output1 = input.AsCompute().Select(x => Matrix3x2.Multiply(x, mMultiply)).ToArray();
            Matrix3x2[] output2 = input.AsCompute().Select(x => x * mMultiply).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void MultiplyScalar()
        {
            var m = Matrix3x2.Identity;
            const float fMultiply = 2.0f;

            Matrix3x2[] input = { m };
            Matrix3x2[] expectedOutput = { m * fMultiply };

            Matrix3x2[] output1 = input.AsCompute().Select(x => Matrix3x2.Multiply(x, fMultiply)).ToArray();
            Matrix3x2[] output2 = input.AsCompute().Select(x => x * fMultiply).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Add()
        {
            var m = Matrix3x2.Identity;

            Matrix3x2[] input = { m };
            Matrix3x2[] expectedOutput = { Matrix3x2.Identity * 2 };

            Matrix3x2[] output1 = input.AsCompute().Select(x => Matrix3x2.Add(x, Matrix3x2.Identity)).ToArray();
            Matrix3x2[] output2 = input.AsCompute().Select(x => x + Matrix3x2.Identity).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }

        [Fact]
        public void Subtract()
        {
            var m = Matrix3x2.Identity * 2;

            Matrix3x2[] input = { m };
            Matrix3x2[] expectedOutput = { m - Matrix3x2.Identity };

            Matrix3x2[] output1 = input.AsCompute().Select(x => Matrix3x2.Subtract(x, Matrix3x2.Identity)).ToArray();
            Matrix3x2[] output2 = input.AsCompute().Select(x => x - Matrix3x2.Identity).ToArray();

            Assert.Equal(expectedOutput, output1);
            Assert.Equal(expectedOutput, output2);
        }
    }
}
