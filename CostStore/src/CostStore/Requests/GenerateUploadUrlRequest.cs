namespace CostStore.Requests;

public class GenerateUploadUrlRequest
{
    public string PartitionKey { get; set; }
    public string BucketName {get; set;}
}