namespace CostStore.Requests;

public class CheckRequest
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}