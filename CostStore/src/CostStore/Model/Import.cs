using Amazon.DynamoDBv2.DataModel;

namespace CostStore;

[DynamoDBTable("Cost")]
public class Import
{
    [DynamoDBHashKey] //Partition key
    public string PK
    {
        get; set;
    }

    [DynamoDBRangeKey] //Sort key
    public string CostId
    {
        get; set;
    }

    public string ImportId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public string UploadUrl { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal? Uplift { get; set; }
    public bool? Overwrite { get; set; }
    public string Created { get; set; }

    public string GetCostId()
    {
        return "import-" + ImportId;
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(Created))
        {
            Created = DateTime.Now.ToString("s");
        }

        DB.GetContext().SaveAsync(this).Wait();
    }
}