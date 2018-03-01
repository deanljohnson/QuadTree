using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace QuadTreeBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            List<TypeInfo> benchmarkTypes = Assembly.GetExecutingAssembly()
                .DefinedTypes
                .Where(t => t.ImplementedInterfaces.Contains(typeof(IBenchmark)))
                .ToList();

            List<IBenchmark> benchmarks = benchmarkTypes
                .Select(bt => (IBenchmark) Activator.CreateInstance(bt))
                .ToList();

            while (true)
            {
                Console.WriteLine("Test Options");
                for (int i = 0; i < benchmarks.Count; i++)
                {
                    Console.WriteLine($"\t{i}) {benchmarks[i].Name}");
                }
                Console.Write("Select Benchmark: ");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int benchmarkIndex))
                {
                    var summary = BenchmarkRunner.Run(benchmarks[benchmarkIndex].GetType());
                }
                else
                {
                    return;
                }
            }
        }
    }
}
