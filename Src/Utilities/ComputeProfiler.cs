using System;
using System.Diagnostics;

namespace LinqToCompute.Utilities
{
    public class ComputeProfiler
    {
        internal Stopwatch SetupQuery { get; } = new Stopwatch();
        internal Stopwatch SetupDevice { get; } = new Stopwatch();
        internal Stopwatch TransferWrite { get; } = new Stopwatch();
        internal Stopwatch TransferRead { get; } = new Stopwatch();
        internal Stopwatch Execution { get; } = new Stopwatch();

        public TimeSpan SetupElapsed => SetupQueryElapsed + SetupDeviceElapsed;
        public TimeSpan SetupQueryElapsed => SetupQuery.Elapsed;
        public TimeSpan SetupDeviceElapsed => SetupDevice.Elapsed;
        public TimeSpan TransferElapsed => TransferWriteElapsed + TransferReadElapsed;
        public TimeSpan TransferWriteElapsed => TransferWrite.Elapsed;
        public TimeSpan TransferReadElapsed => TransferRead.Elapsed;
        public TimeSpan ExecutionElapsed => Execution.Elapsed;
    }
}
