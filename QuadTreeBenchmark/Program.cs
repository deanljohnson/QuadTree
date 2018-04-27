using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Running;
using QuadTree;

namespace QuadTreeBenchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
            Type typeToTest = GetTypeToTest();
            List<TypeInfo> benchmarkTypes = Assembly.GetExecutingAssembly()
                .DefinedTypes
                .Where(t => t.ImplementedInterfaces.Contains(typeToTest))
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

        private static Type GetTypeToTest()
        {
            while (true)
            {
                Console.WriteLine("Test Types");
                Console.WriteLine("\t0) QuadTree");
                Console.WriteLine("\t1) BucketGrid");
                Console.Write("Select Type: ");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int inputInt))
                {
                    if (inputInt == 0) return typeof(IBenchmark<QuadTree<TestObject>>);
                    if (inputInt == 1) return typeof(IBenchmark<BucketGrid<TestObject>>);
                }
            }
        }
    }
}
