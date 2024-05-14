namespace CostStore.Requests;

public class CreateReportRequest
{
    public string PartitionKey { get; set; }
    public string Name { get; set; }
}