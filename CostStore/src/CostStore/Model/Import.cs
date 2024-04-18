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
    public int Year { get; set; }
    public int Month { get; set; }
    public string Type { get; set; }

    public string GetCostId()
    {
        return "import-" + Year + "-" + Month.ToString("D2") + "-" + ImportId;
    }

    public void Save()
    {
        DB.GetContext().SaveAsync(this).Wait();
    }
}