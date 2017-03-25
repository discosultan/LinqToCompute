using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToCompute.Utilities;

namespace LinqToCompute
{
    /// <summary>
    /// Provides extension methods related to LINQ GPGPU queries.
    /// </summary>
    public static class VulkanComputeExtensions
    {
        public static VulkanComputeQuery<T> AsComputeQuery<T>(this IEnumerable<T> sequence, ComputeProfiler profiler = null)
            => new VulkanComputeProvider(VulkanDevice.Default, profiler).CreateQuery<T>(sequence.AsQueryable().Expression);
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

        public VulkanComputeQuery<TResult> CreateQuery<TResult>(Expression expression)
            => new VulkanComputeQuery<TResult>(this, expression);

        public object Execute(Expression expression)
        {
            // If expression represents an unqueried IQueryable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression.
            if (!(expression is MethodCallExpression))
                throw new InvalidOperationException("Compute queries are only allowed over data sources.");

            _profiler?.Setup.Start();

            // Simplify the expression tree.
            // Identify sub-trees that can be immediately evaluated and turned into values.
            expression = Evaluator.PartialEval(expression);

            var translator = new GlslComputeTranslator();
            using (VulkanComputeExecutor executionCtx = translator.Translate(expression, _device))
            {
                executionCtx.GpuSetup();

                _profiler?.Setup.Stop();
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

        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression);
    }
}
