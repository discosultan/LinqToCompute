using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToCompute
{
    public class VulkanComputeQuery<T> : IQueryable<T>
    {
        public VulkanComputeQuery(IQueryProvider provider, Expression expression = null)
        {
            if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException(nameof(expression));

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
        }

        public Expression Expression { get; }
        public Type ElementType => typeof(T);
        public IQueryProvider Provider { get; }

        public IEnumerator<T> GetEnumerator() => (Provider.Execute<IEnumerable<T>>(Expression)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (Provider.Execute<IEnumerable>(Expression)).GetEnumerator();

        public VulkanComputeQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
            => (VulkanComputeQuery<TResult>)Queryable.Select(this, selector);

        public VulkanComputeQuery<TResult> Zip<T2, TResult>(IEnumerable<T2> source2, Expression<Func<T, T2, TResult>> resultSelector)
            => (VulkanComputeQuery<TResult>)Queryable.Zip(this, source2, resultSelector);

        public T[] ToArray()
        {
            IEnumerable<T> result = Provider.Execute<IEnumerable<T>>(Expression);
            var array = result as T[];
            return array ?? result.ToArray();
        }

        // TODO fast path for ToList()
    }
}
