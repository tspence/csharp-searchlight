using System.Diagnostics;
using System.Reflection;
using perftest;
using Searchlight;

public class Program
{
    private const int ITERATIONS = 10_000;
    
    public static void Main()
    {
        var engine = new SearchlightEngine()
            .AddClass(typeof(Employee))
            .AddClass(typeof(Company))
            .AddClass(typeof(Paystub));
        
        var sw = new Stopwatch();
        sw.Start();
        long parsings = 0;

        for (var i = 0; i < ITERATIONS; i++)
        {
            parsings += RunSimpleSearchlightSuite(engine);
        }

        var totalTime = sw.Elapsed;
        var timePerParse = totalTime / parsings;
        Console.WriteLine($"Completed {parsings} searchlight Parses in {totalTime} (or {timePerParse} each)");
    }

    private static int RunSimpleSearchlightSuite(SearchlightEngine engine)
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
                var syntaxTree = engine.Parse(new FetchRequest()
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