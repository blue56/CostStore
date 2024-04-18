namespace CostStore.Requests;

public class CreateImportRequest
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Type { get; set; }
}