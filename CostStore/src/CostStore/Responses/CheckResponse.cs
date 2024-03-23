namespace CostStore.Responses;

public class CheckResponse
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsCostAllocated {get; set;}
}