namespace CostStore.Requests;

public class CreateImportRequest
{
    public string PartitionKey { get; set; }
    public string Type { get; set; }
    public string UploadUrl { get; set; }
    public decimal? ExchangeRate { get; set; }
    public bool Overwrite { get; set; }
    public string Name { get; set; }
}