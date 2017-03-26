using System;

namespace LinqToCompute.Glsl
{
    internal class GlslMethodSignature
    {
        public GlslMethodSignature(Type returns, params Type[] parameters)
        {
            Returns = returns;
            Parameters = parameters;
        }

        public Type Returns { get; }
        public Type[] Parameters { get; }
    }
}
