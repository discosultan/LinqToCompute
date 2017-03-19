using System;
using System.Diagnostics;

namespace LinqToCompute.Utilities
{
    public class ComputeProfiler
    {
        internal Stopwatch Setup { get; } = new Stopwatch();
        internal Stopwatch TransferWrite { get; } = new Stopwatch();
        internal Stopwatch TransferRead { get; } = new Stopwatch();
        internal Stopwatch Execution { get; } = new Stopwatch();

        public virtual TimeSpan SetupElapsed => Setup.Elapsed;
        public virtual TimeSpan TransferElapsed => TransferWriteElapsed + TransferReadElapsed;
        public virtual TimeSpan TransferWriteElapsed => TransferWrite.Elapsed;
        public virtual TimeSpan TransferReadElapsed => TransferRead.Elapsed;
        public virtual TimeSpan ExecutionElapsed => Execution.Elapsed;
    }
}
