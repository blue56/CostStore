namespace CostStore.Requests;

public class ImportRequest
{
    public string Path {get; set;}
    
    // Uplift 0 = 0%, 1 = 100%
    public decimal? Uplift {get; set;}
    
    // ExchangeRate Basecost -> Total rate
    public decimal? ExchangeRate {get; set;}
}