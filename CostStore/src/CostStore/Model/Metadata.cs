using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using CostStore;

[DynamoDBTable("Cost")]
public class Metadata
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

    // Cost store currency
    public string Currency {get; set;}

    public void Save()
    {
        DB.GetContext().SaveAsync(this).Wait();
    }
}