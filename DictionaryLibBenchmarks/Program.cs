using BenchmarkDotNet.Running;
using System;

namespace DictionaryLibBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run the benchmark
            var summary = BenchmarkRunner.Run<DictionaryLibWordLookupBenchmark>();
            
            Console.WriteLine("Benchmark completed. Results saved to BenchmarkDotNet.Artifacts folder.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}