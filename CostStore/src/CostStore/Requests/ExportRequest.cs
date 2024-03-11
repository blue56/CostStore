namespace CostStore;

public class ExportRequest
{
    public string PartitionKey { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public string Path { get; set; }
}