using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToCompute.Glsl;
using LinqToCompute.Utilities;

namespace LinqToCompute
{
    /// <summary>
    /// Provides extension methods related to LINQ GPGPU queries.
    /// </summary>
    public static class VulkanComputeExtensions
    {
        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="IQueryable{T}"/> that is
        /// capable of executing the query on GPU.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence to convert.</param>
        /// <param name="profiler">
        /// An optional profiler that will measure the timings of operating with GPU.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that represents the input sequence as a GPU compute query.
        /// </returns>
        public static VulkanComputeQuery<T> AsCompute<T>(this IEnumerable<T> source, ComputeProfiler profiler = null)
        {
            return new VulkanComputeProvider(VulkanDevice.Default, profiler)
                .CreateQuery<T>(source.AsQueryable().Expression);
        }
    }

    internal class VulkanComputeProvider : IQueryProvider
    {
        private readonly VulkanDevice _device;
        private readonly ComputeProfiler _profiler;

        public VulkanComputeProvider(VulkanDevice device, ComputeProfiler profiler)
        {
            _device = device;
            _profiler = profiler;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(
                    typeof(VulkanComputeQuery<>).MakeGenericType(elementType), this, expression);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => CreateQuery<TElement>(expression);

        public VulkanComputeQuery<TResult> CreateQuery<TResult>(Expression expression) => new VulkanComputeQuery<TResult>(this, expression);

        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression);

        public object Execute(Expression expression)
        {
            // If expression represents an unqueried IQueryable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression.
            if (!(expression is MethodCallExpression))
                throw new InvalidOperationException("Compute queries are only allowed over data sources.");

            _profiler?.SetupQuery.Start();

            // Simplify the expression tree.
            // Identify sub-trees that can be immediately evaluated and turned into values.
            expression = Evaluator.PartialEval(expression);

            // Use GlslComputeTranslator for translation. In future, this could be swapped out for
            // direct LINQ to SPIR-V translator for slightly improved performance.
            var translator = new GlslComputeTranslator();
            using (VulkanComputeContext executionCtx = translator.Translate(expression, _device))
            {
                _profiler?.SetupQuery.Stop();
                _profiler?.SetupDevice.Start();

                executionCtx.GpuSetup();

                _profiler?.SetupDevice.Stop();
                _profiler?.TransferWrite.Start();

                executionCtx.GpuTransferInput();

                _profiler?.TransferWrite.Stop();
                _profiler?.Execution.Start();

                executionCtx.GpuExecute();

                _profiler?.Execution.Stop();
                _profiler?.TransferRead.Start();

                executionCtx.GpuTransferOutput();

                _profiler?.TransferRead.Stop();

                return executionCtx.Output.HostResource;
            }
        }
    }
}
