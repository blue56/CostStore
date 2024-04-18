namespace CostStore.Requests;

public class ListImportsRequest
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Type { get; set; }
}