using System;

namespace LinqToCompute.Utilities
{
    public static class ComputeMath
    {
        public static float Convolute(float[] kernel, float[] data, float denominator, float offset)
        {
            if (kernel == null)
                throw new ArgumentNullException(nameof(kernel));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (kernel.Length != data.Length)
                throw new ArgumentException("Kernel and data lengths must match.");

            float result = 0.0f;
            for (int i = 0; i < kernel.Length; i++)
            {
                result += kernel[i] * data[i];
            }
            return Clamp(result / denominator + offset, 0.0f, 1.0f);
        }

        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);
    }
}
