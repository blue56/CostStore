namespace CostStore.Requests;

public class AllocateRequest : Request
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}