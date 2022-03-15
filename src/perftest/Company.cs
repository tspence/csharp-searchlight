using Searchlight;

namespace perftest;

[SearchlightModel(DefaultSort = "ID")]
public class Company
{
    [SearchlightField]
    public Guid ID { get; set; }
    [SearchlightField]
    public string Name { get; set; }
    [SearchlightField]
    public bool IsPublic { get; set; }
    [SearchlightField]
    public string? StockTickerSymbol { get; set; }
    [SearchlightField]
    public DateTime? WentPublicDate { get; set; } 
    [SearchlightField]
    public decimal? StockPrice { get; set; }
    [SearchlightField]
    public double? Latitude { get; set; }
    [SearchlightField]
    public double? Longitude { get; set; }
}