namespace CostStore.Requests;

public class CreateMonthReportRequest
{
    public string PartitionKey { get; set; }
    public string Name { get; set; }
    public string Month { get; set; }
}