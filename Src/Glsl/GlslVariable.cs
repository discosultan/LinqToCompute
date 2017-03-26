using System;
using System.Threading;

namespace LinqToCompute.Glsl
{
    internal class GlslVariable
    {
        private static int _sequence;

        public GlslVariable(Type type, string name, bool isGlobal)
        {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
        }

        public string Name { get; }
        public bool IsGlobal { get; }
        public Type Type { get; }

        public override string ToString() => Type.GlslName() + " " + Name;

        public static GlslVariable Next(Type type)
        {
            int id = Interlocked.Increment(ref _sequence);
            return new GlslVariable(type, "x" + id, false);
        }
    }
}
