using System.Linq;
using Xunit;

namespace LinqToCompute.Tests
{
    public class GeneralTest
    {
        [Fact]
        public void Noop()
        {
            int[] input = { 0, 1 };
            int[] expectedOutput = { 0, 1 };

            int[] output = input.AsCompute().Select(x => x).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void NoopBoolean()
        {
            bool[] input = { true, true };
            bool[] expectedOutput = { true, true };

            bool[] output = input.AsCompute().Select(x => x).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Constant()
        {
            int[] input = { 0, 1 };
            int[] expectedOutput = { 1, 1 };

            int[] output = input.AsCompute().Select(x => 1).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Increment()
        {
            int[] input = { 0, 1 };
            int[] expectedOutput = { 1, 2 };

            int[] output = input.AsCompute().Select(x => x + 1).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void SequentialIncrements()
        {
            int[] input = { 0, 1 };
            int[] expectedOutput = { 3, 4 };

            int[] output = input.AsCompute().Select(x => x + 1).Select(x => x + 2).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void ConvertType()
        {
            float[] input = { 0, 1 };

            uint[] expectedOutput = { 0, 1 };

            uint[] output = input.AsCompute().Select(x => (uint)x).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AddLocalVariable()
        {
            int[] input = { 0, 1 };
            const int localVariable = 1;
            int[] expectedOutput = { 1, 2 };

            int[] output = input.AsCompute().Select(x => x + localVariable).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AddToLocalVariable()
        {
            int[] input = { 0, 1 };
            const int localVariable = 1;
            int[] expectedOutput = { 1, 2 };

            int[] output = input.AsCompute().Select(x => localVariable + x).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void DecimalValue()
        {
            float[] input = { 0 };
            float[] expectedOutput = { 0.5f };

            float[] output = input.AsCompute().Select(x => 0.5f).ToArray();

            Assert.Equal(expectedOutput, output, FloatComparer.Default);
        }

        private struct CustomStructure
        {
            public int Value;

            public CustomStructure(int value) => Value = value;
        }

        [Fact(Skip = "Not implemented")]
        public void CustomStructureCtorIncrement()
        {
            CustomStructure[] input = { new CustomStructure(0), new CustomStructure(1) };
            CustomStructure[] expectedOutput = { new CustomStructure(1), new CustomStructure(2) };

            CustomStructure[] output = input.AsCompute().Select(x => new CustomStructure(x.Value + 1)).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact(Skip = "Not implemented")]
        public void CustomStructureInitializerIncrement()
        {
            CustomStructure[] input = { new CustomStructure(0), new CustomStructure(1) };
            CustomStructure[] expectedOutput = { new CustomStructure(1), new CustomStructure(2) };

            CustomStructure[] output = input.AsCompute().Select(x => new CustomStructure { Value = x.Value + 1 }).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void ConvertCustomStructure()
        {
            CustomStructure[] input = { new CustomStructure(0), new CustomStructure(1) };
            int[] expectedOutput = { 0, 1 };

            int[] output = input.AsCompute().Select(x => x.Value).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void ZipDifferentArrays()
        {
            int[] input1 = { 1, 2 };
            int[] input2 = { 2, 4 };
            int[] expectedOutput = input1.Zip(input2, (x, y) => x + y).ToArray();

            int[] output = input1.AsCompute().Zip(input2, (x, y) => x + y).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void ZipSameArray()
        {
            int[] input = { 1, 2 };
            int[] expectedOutput = input.Zip(input, (x, y) => x + y).ToArray();

            int[] output = input.AsCompute().Zip(input, (x, y) => x + y).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AnonymousClass()
        {
            int[] input = { 1 };
            int[] expectedOutput = { 2 };

            int[] output = input.AsCompute().Select(x => new { x }).Select(x => 2).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void TwoAnonymousClasses()
        {
            int[] input = { 1 };
            int[] expectedOutput = { 2 };

            int[] output = input.AsCompute().Select(x => new { x, y = 2 }).Select(x => new { z = x.x, w = x.y }).Select(x => x.w).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AnonymousClassInsideAnonymousClass()
        {
            int[] input = { 1 };
            int[] expectedOutput = { 2 };

            int[] output = input.AsCompute().Select(x => new { x }).Select(x => new { x, y = 2 }).Select(x => x.y).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void ArrayIndexer()
        {
            int[] input = { 1 };
            int[] array = { 1, 2 };
            int[] expectedOutput = { 2 };

            int[] output = input.AsCompute().Select(x => array[x]).ToArray();

            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void AnonymousClassWithQuerySyntax()
        {
            int[] input = { 1 };
            int[] expectedOutput = { 3 };

            int[] output = (
                from x in input.AsCompute()
                let y = x + 1
                let z = y + 1
                select z).ToArray();

            Assert.Equal(expectedOutput, output);
        }
    }
}
