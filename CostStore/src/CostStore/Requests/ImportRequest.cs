namespace CostStore.Requests;

public class ImportRequest
{
    public string Path {get; set;}

    public string PartitionKey {get; set;}
    
    // Uplift 0 = 0%, 0.05 = 5%, 1 = 100%
    public decimal? Uplift {get; set;}
    public string? UpliftDescription {get; set;}
    
    // ExchangeRate Basecost -> Total rate
    public decimal? ExchangeRate {get; set;}

    public bool? Overwrite {get; set;}
}