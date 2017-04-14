using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using LinqToCompute;
using static System.Console;

namespace Sandbox
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            VulkanDevice.Default.DebugLog += (s, msg) => Debug.WriteLine(msg);

            // Notify the OS that this is a high priority process.
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            WriteLine("Running as x64: " + Environment.Is64BitProcess);
            WriteLine();

            var results = new List<ExecutionResult>();

            const int min = 1_024 * 4;
            //const int max = 32_768;
            const int max = 8_388_608;

            // Warm up (JIT).
            RunFourierTransformBenchmark(new List<ExecutionResult>(), min);

            //for (int size = min; size <= max; size *= 2)
            //    RunFourierTransformBenchmark(results, size);

            RunBufferBenchmarks(results);

            foreach (var result in results)
                result.Print();

            SaveResultsToFile(results, "all_results.csv", "gpu_results.csv");

            WriteLine("Finished!");
            ReadKey();
        }

        private static void RunFourierTransformBenchmark(List<ExecutionResult> results, int size)
        {
            var cpuWatch1 = new Profiler("CPU Single", false);
            var cpuWatch2 = new Profiler("CPU Multi", false);
            var gpuWatch = new Profiler("GPU", true);

            const string name = "Fast Fourier Transform";
            //const int size = 8388608;
            //const int size = 131072;
            //const int size = 1024;
            const bool validate = false;
            var random = new Random();
            
            int[] xs = Generate(size, i => i);
            Vector2[] input = Generate(size, _ => new Vector2((float)random.NextDouble(), 0.0f));
            Vector2[] output1 = null, output2 = null, output3 = null;

            // Fast Fourier Transform
            int numIterations = (int)Math.Log(size, 2.0);

            // CPU 1 core
            {
                Vector2[] inputCopy = input.ToArray();
                PrepareGC();
                WriteLine($"Executing {name} on CPU (single core) ...");
                cpuWatch1.Total.Start();
                int fftSize = 2;
                for (int i = 0; i < numIterations; i++)
                {
                    int fftSizeTemp = fftSize;
                    Vector2[] inputTemp = inputCopy;
                    var query = from x in xs
                        let angle = -2 * (float)Math.PI * (x / (float)fftSizeTemp)
                        let t = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle))
                        let x0 = (int)Math.Floor((float)x / fftSizeTemp) * (fftSizeTemp / 2) + x % (fftSizeTemp / 2)
                        let val0 = inputTemp[x0]
                        let val1 = inputTemp[x0 + size / 2]
                        select new Vector2(
                            val0.X + t.X * val1.X - t.Y * val1.Y,
                            val0.Y + t.Y * val1.X - t.X * val1.Y);

                    output1 = query.ToArray();

                    fftSize *= 2;
                    if (i < numIterations - 1)
                        Swap(ref inputCopy, ref output1);
                }
                cpuWatch1.Total.Stop();
            }

            // CPU multicore
            {
                Vector2[] inputCopy = input.ToArray();
                PrepareGC();
                WriteLine($"Executing {name} on CPU (multicore) ...");
                cpuWatch2.Total.Start();
                int fftSize = 2;
                for (int i = 0; i < numIterations; i++)
                {
                    int fftSizeTemp = fftSize;
                    Vector2[] inputTemp = inputCopy;
                    var query = from x in xs.AsParallel()
                                let angle = -2 * (float)Math.PI * (x / (float)fftSizeTemp)
                                let t = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle))
                                let x0 = (int)Math.Floor((float)x / fftSizeTemp) * (fftSizeTemp / 2) + x % (fftSizeTemp / 2)
                                let val0 = inputTemp[x0]
                                let val1 = inputTemp[x0 + size / 2]
                                select new Vector2(
                                    val0.X + t.X * val1.X - t.Y * val1.Y,
                                    val0.Y + t.Y * val1.X - t.X * val1.Y);

                    output2 = query.ToArray();

                    fftSize *= 2;
                    if (i < numIterations - 1)
                        Swap(ref inputCopy, ref output2);
                }
                cpuWatch2.Total.Stop();
            }

            // GPU
            {
                Vector2[] inputCopy = input.ToArray();
                PrepareGC();
                WriteLine($"Executing {name} on GPU ...");
                gpuWatch.Total.Start();
                int fftSize = 2;
                for (int i = 0; i < numIterations; i++)
                {
                    int fftSizeTemp = fftSize;
                    Vector2[] inputTemp = inputCopy;
                    var query = from x in xs.AsCompute(gpuWatch)
                                let angle = -2 * (float)Math.PI * (x / (float)fftSizeTemp)
                                let t = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle))
                                let x0 = (int)Math.Floor((float)x / fftSizeTemp) * (fftSizeTemp / 2) + x % (fftSizeTemp / 2)
                                let val0 = inputTemp[x0]
                                let val1 = inputTemp[x0 + size / 2]
                                select new Vector2(
                                    val0.X + t.X * val1.X - t.Y * val1.Y,
                                    val0.Y + t.Y * val1.X - t.X * val1.Y);

                    output3 = query.ToArray();

                    fftSize *= 2;
                    if (i < numIterations - 1)
                        Swap(ref inputCopy, ref output3);
                }
                gpuWatch.Total.Stop();
            }

            if (validate)
            {
                for (int i = 0; i < size; i++)
                {
                    if (!Vector2Comparer.Default.Equals(output1[i], output2[i]))
                        throw new Exception($"{nameof(output1)}[{i}] {output1[i]} does not equal {nameof(output2)}[{i}] {output2[i]}");
                    if (!Vector2Comparer.Default.Equals(output1[i], output3[i]))
                        throw new Exception($"{nameof(output1)}[{i}] {output1[i]} does not equal {nameof(output3)}[{i}] {output3[i]}");
                }
            }

            WriteLine();

            results.Add(new ExecutionResult(name, size, cpuWatch1, cpuWatch2, gpuWatch));
        }

        private static void RunBufferBenchmarks(List<ExecutionResult> results)
        {
            Execute(
                "JIT WARM UP",
                1,
                i => i,
                input => input.Select(i => i + 1),
                input => input.Select(i => i + 1),
                input => input.Select(i => i + 1));

            for (int count = 1024; count <= 268_435_456 / 2; count *= 4)
            {
                results.Add(Execute(
                    "Integer Increment",
                    count,
                    i => i,
                    input => input.Select(i => i + 1),
                    input => input.Select(i => i + 1),
                    input => input.Select(i => i + 1)));
            }

            //var scale = Matrix4x4.CreateScale(2.0f);
            //var rotation = Matrix4x4.CreateRotationX((float)Math.PI);
            //var translation = Matrix4x4.CreateTranslation(Vector3.One);
            //results.Add(Execute(
            //    "Matrix Multiplication",
            //    5_000_000,
            //    _ => Matrix4x4.Identity,
            //    input => input.Select(m => m * scale * rotation * translation),
            //    input => input.Select(m => m * scale * rotation * translation),
            //    input => input.Select(m => m * scale * rotation * translation)));

            //results.Add(Execute(
            //    "Vector Dot",
            //    25_000_000,
            //    i => new Vector4(i % 10, i % 10 + 1, i % 10 + 2, i % 10 + 3),
            //    input => input.Zip(input, (v1, v2) => Vector4.Dot(v1, v2)),
            //    input => input.Zip(input, (v1, v2) => Vector4.Dot(v1, v2)),
            //    input => input.Zip(input, (v1, v2) => Vector4.Dot(v1, v2)),
            //    comparer: FloatComparer.Default));
        }

        private static ExecutionResult Execute<TSrc, TDst>(
            string name,
            int count,
            Func<int, TSrc> factory,
            Func<IEnumerable<TSrc>, IEnumerable<TDst>> cpuSingle, 
            Func<ParallelQuery<TSrc>, ParallelQuery<TDst>> cpuMulti,
            Func<VulkanComputeQuery<TSrc>, VulkanComputeQuery<TDst>> gpu, 
            IEqualityComparer<TDst> comparer = null)
        {
            WriteLine($"Setting up {name} ...");

            // Setup profilers.
            var cpuWatchSingle = new Profiler("CPU Single", false);
            var cpuWatchMulti = new Profiler("CPU Multi", false);
            var gpuWatch = new Profiler("GPU", true);

            // Generate test data.
            TSrc[] input = Generate(count, factory);

            WriteLine($"Executing {name} on CPU (single core) ...");
            PrepareGC();
            cpuWatchSingle.Total.Start();
            TDst[] output1 = cpuSingle(input).ToArray();
            cpuWatchSingle.Total.Stop();

            WriteLine($"Executing {name} on CPU (multiple cores) ...");
            PrepareGC();
            cpuWatchMulti.Total.Start();
            TDst[] output2 = cpuMulti(input.AsParallel()).ToArray();
            cpuWatchMulti.Total.Stop();

            WriteLine($"Executing {name} on GPU ...");
            PrepareGC();
            gpuWatch.Total.Start();
            TDst[] output3 = gpu(input.AsCompute(gpuWatch)).ToArray();
            gpuWatch.Total.Stop();

            comparer = comparer ?? EqualityComparer<TDst>.Default;
            for (int i = 0; i < input.Length; i++)
            {
                if (!comparer.Equals(output1[i], output3[i]))
                    throw new Exception($"{nameof(output1)}[{i}] {output1[i]} does not equal {nameof(output3)}[{i}] {output3[i]}");
            }

            WriteLine();
            return new ExecutionResult(name, input.Length, cpuWatchSingle, cpuWatchMulti, gpuWatch);
        }

        private static T[] Generate<T>(int count, Func<int, T> factory) => Enumerable.Range(0, count).Select(factory).ToArray();

        private static void Color(Action<string> write, object value, ConsoleColor color)
        {
            var previousColor = ForegroundColor;
            ForegroundColor = color;
            write(value.ToString());
            ForegroundColor = previousColor;
        }

        private static void Swap<T>(ref T first, ref T second)
        {
            T temp = first;
            first = second;
            second = temp;
        }

        private static void SaveResultsToFile(IEnumerable<ExecutionResult> results, string allResultsPath, string gpuResultsPath)
        {
            const string separator = "\t";
            var groups = results.GroupBy(x => x.Count);

            // LINQ vs PLINQ vs LinqToCompute.
            using (var writer = File.CreateText(allResultsPath))
            {
                foreach (var group in groups)
                {
                    writer.Write(group.Key);
                    writer.Write(separator);

                    foreach (var result in group)
                    {
                        for (var i = 0; i < result.Profilers.Length; i++)
                        {
                            var profiler = result.Profilers[i];
                            writer.Write(profiler.TotalElapsed.TotalSeconds);
                            if (i != result.Profilers.Length - 1)
                                writer.Write(separator);
                        }
                        writer.WriteLine();
                    }
                }
            }

            // LinqToCompute detailed.
            using (var writer = File.CreateText(gpuResultsPath))
            {
                foreach (var group in groups)
                {
                    writer.Write(group.Key);
                    writer.Write(separator);

                    foreach (var result in group)
                    {
                        var gpuProfiler = result.Profilers[result.Profilers.Length - 1];

                        writer.Write(gpuProfiler.SetupQueryElapsed.TotalSeconds);
                        writer.Write(separator);
                        writer.Write(gpuProfiler.SetupDeviceElapsed.TotalSeconds);
                        writer.Write(separator);
                        writer.Write(gpuProfiler.TransferWriteElapsed.TotalSeconds);
                        writer.Write(separator);
                        writer.Write(gpuProfiler.ExecutionElapsed.TotalSeconds);
                        writer.Write(separator);
                        writer.Write(gpuProfiler.TransferReadElapsed.TotalSeconds);
                        writer.Write(separator);

                        writer.WriteLine();
                    }
                }
            }
        }

        private static void PrepareGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private class ExecutionResult
        {
            public string Name { get; }
            public int Count { get; }
            public Profiler[] Profilers { get; }

            public ExecutionResult(string name, int count, params Profiler[] profilers)
            {
                Name = name;
                Count = count;
                Profilers = profilers;
            }

            public void Print()
            {
                int bestTotal = Profilers.MinIndexBy(x => x.TotalElapsed);
                int bestExecution = Profilers.MinIndexBy(x => x.ExecutionElapsed);

                ConsoleColor defaultColor = ForegroundColor;
                const ConsoleColor winColor = ConsoleColor.Green;
                const ConsoleColor loseColor = ConsoleColor.Red;

                WriteLine(Name);
                WriteLine();
                for (int i = 0; i < Profilers.Length; i++)
                {
                    var profiler = Profilers[i];

                    if (profiler == null) continue;

                    Write($"{profiler.Name} Total: "); Color(WriteLine, profiler.TotalElapsed, i == bestTotal ? winColor : loseColor);
                    if (profiler.IsGpu)
                    {
                        Write($"  Setup: "); Color(WriteLine, profiler.SetupElapsed, defaultColor);
                        Write($"   Translate query: "); Color(WriteLine, profiler.SetupQueryElapsed, defaultColor);
                        Write($"   Initialize device: "); Color(WriteLine, profiler.SetupDeviceElapsed, defaultColor);
                        Write($"  Transfer: "); Color(WriteLine, profiler.TransferElapsed, defaultColor);
                        Write($"    Write: "); Color(WriteLine, profiler.TransferWriteElapsed, defaultColor);
                        Write($"    Read: "); Color(WriteLine, profiler.TransferReadElapsed, defaultColor);
                        Write($"  Execution: "); Color(WriteLine, profiler.ExecutionElapsed, i == bestExecution ? winColor : loseColor);
                    }
                    WriteLine();
                }
            }
        }
    }
}
