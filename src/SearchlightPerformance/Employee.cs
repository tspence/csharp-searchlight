using System;
using Searchlight;

namespace perftest
{

    [SearchlightModel(DefaultSort = "ID")]
    public class Employee
    {
        [SearchlightField] public int ID { get; set; }
        [SearchlightField] public string Name { get; set; }
        [SearchlightField] public DateTime HireDate { get; set; }
        [SearchlightField] public DateTime? TerminationDate { get; set; }
        [SearchlightField] public float? Salary { get; set; }
        [SearchlightField] public char? JobClass { get; set; }
        [SearchlightField] public string Address { get; set; }
    }
}