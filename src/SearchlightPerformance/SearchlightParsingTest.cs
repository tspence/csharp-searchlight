using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using perftest;
using Searchlight;

[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
[SimpleJob(RuntimeMoniker.NetCoreApp30)]
[SimpleJob(RuntimeMoniker.CoreRt30)]
[RPlotExporter]
public class SearchlightParsingTest
{
    private SearchlightEngine _engine;
    [GlobalSetup]
    public void SetupEngine()
    {
        _engine = new SearchlightEngine()
            .AddClass(typeof(Employee))
            .AddClass(typeof(Company))
            .AddClass(typeof(Paystub));
    }

    [Benchmark]
    public int RunSimpleSearchlightSuite()
    {
        int totalCount = 0;
        foreach (var filter in new string[]
                 {
                     "Name startswith a and (HireDate between 2020-01-01 and 2021-01-01 and TerminationDate is null)",
                     "Salary gte 20000.56 and JobClass = 'A' and name in ('Bob Smith', 'Sally Jones', 'Charlie Dalton')",
                     "(Address contains 'Washington' or Address contains 'California' or Address contains 'New York') and TerminationDate is not null",
                 })
        {
            foreach (var order in new string[]
                     {
                         "JobClass asc, Name, Salary",
                         "Name asc, HireDate desc",
                     })
            {
                var syntaxTree = _engine.Parse(new FetchRequest()
                    { table = "Employee", filter = filter, order = order });
                if (syntaxTree == null)
                {
                    Console.WriteLine("Something went wrong");
                }
                else
                {
                    totalCount++;
                }
            }
        }

        return totalCount;
    }
}