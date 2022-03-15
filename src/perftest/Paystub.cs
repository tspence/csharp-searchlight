using Searchlight;

namespace perftest;

[SearchlightModel(DefaultSort = "PaystubId")]
public class Paystub
{
    [SearchlightField]
    public Guid PaystubId { get; set; }
    [SearchlightField]
    public int EmployeeId { get; set; }
    [SearchlightField]
    public Guid CompanyId { get; set; }
    [SearchlightField]
    public DateTime PayDate { get; set; }
    [SearchlightField]
    public decimal Amount { get; set; }
}