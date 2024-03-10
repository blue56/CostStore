namespace CostStore.Requests;

public class ImportRequest : Request
{
    public string Path {get; set;}

    public string PartitionKey {get; set;}
    
    // Uplift 0 = 0%, 1 = 100%
    public decimal? Uplift {get; set;}
    
    // ExchangeRate Basecost -> Total rate
    public decimal? ExchangeRate {get; set;}
}