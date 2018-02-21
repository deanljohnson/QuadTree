using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace QuadTreeTest
{
    public class Clock
    {
        interface IStopwatch
        {
            bool IsRunning { get; }
            TimeSpan Elapsed { get; }

            void Start();
            void Stop();
            void Reset();
        }

        class TimeWatch : IStopwatch
        {
            private readonly Stopwatch m_Stopwatch = new Stopwatch();

            public TimeSpan Elapsed => m_Stopwatch.Elapsed;

            public bool IsRunning => m_Stopwatch.IsRunning;

            public TimeWatch()
            {
                if (!Stopwatch.IsHighResolution)
                    throw new NotSupportedException("Your hardware doesn't support high resolution counter");

                //prevent the JIT Compiler from optimizing Fkt calls away
                long seed = Environment.TickCount;

                //use the second Core/Processor for the test
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);

                //prevent "Normal" Processes from interrupting Threads
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                //prevent "Normal" Threads from interrupting this thread
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

            public void Start()
            {
                m_Stopwatch.Start();
            }

            public void Stop()
            {
                m_Stopwatch.Stop();
            }

            public void Reset()
            {
                m_Stopwatch.Reset();
            }
        }

        class CpuWatch : IStopwatch
        {
            TimeSpan m_StartTime;
            TimeSpan m_EndTime;

            public TimeSpan Elapsed
            {
                get
                {
                    if (IsRunning)
                        throw new NotImplementedException("Getting elapsed span while watch is running is not implemented");

                    return m_EndTime - m_StartTime;
                }
            }

            public bool IsRunning { get; private set; }

            public void Start()
            {
                m_StartTime = Process.GetCurrentProcess().TotalProcessorTime;
                IsRunning = true;
            }

            public void Stop()
            {
                m_EndTime = Process.GetCurrentProcess().TotalProcessorTime;
                IsRunning = false;
            }

            public void Reset()
            {
                m_StartTime = TimeSpan.Zero;
                m_EndTime = TimeSpan.Zero;
            }
        }

        public static void BenchmarkTime(Action action, int iterations = 10000)
        {
            Benchmark<TimeWatch>(action, iterations);
        }

        static void Benchmark<T>(Action action, int iterations) where T : IStopwatch, new()
        {
            //clean Garbage
            GC.Collect();

            //wait for the finalizer queue to empty
            GC.WaitForPendingFinalizers();

            //clean Garbage
            GC.Collect();

            //warm up
            action();

            var stopwatch = new T();
            var timings = new double[5];
            for (var i = 0; i < timings.Length; i++)
            {
                stopwatch.Reset();
                stopwatch.Start();
                for (var j = 0; j < iterations; j++)
                    action();
                stopwatch.Stop();
                timings[i] = stopwatch.Elapsed.TotalMilliseconds;
                Console.WriteLine(timings[i]);
            }
            Console.WriteLine("normalized mean: " + timings.NormalizedMean());
        }

        public static void BenchmarkCpu(Action action, int iterations = 10000)
        {
            Benchmark<CpuWatch>(action, iterations);
        }
    }

    internal static class DataHelpers
    {
        public static double NormalizedMean(this ICollection<double> values)
        {
            if (values.Count == 0)
                return double.NaN;

            var deviations = values.Deviations().ToArray();
            var meanDeviation = deviations.Sum(t => Math.Abs(t.Item2)) / values.Count;
            return deviations.Where(t => t.Item2 > 0 || Math.Abs(t.Item2) <= meanDeviation).Average(t => t.Item1);
        }

        public static IEnumerable<Tuple<double, double>> Deviations(this ICollection<double> values)
        {
            if (values.Count == 0)
                yield break;

            var avg = values.Average();
            foreach (var d in values)
                yield return Tuple.Create(d, avg - d);
        }
    }
}
