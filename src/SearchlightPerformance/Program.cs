using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace perftest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ManualConfig()
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddLogger(new ConsoleLogger());
            var summary = BenchmarkRunner.Run<SearchlightParsingTest>(config);
            Console.WriteLine($"Summary: {summary}");
        }
    }
}