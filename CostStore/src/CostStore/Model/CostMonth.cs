using Amazon.DynamoDBv2.DataModel;
using CostStore;

[DynamoDBTable("Cost")]
public class CostMonth
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

    public int Month { get; set; }
    public int Year { get; set; }
    public string Name { get; set; }

    public string GetCostId(int Year, int Month)
    {
        return Year + "-" + Month.ToString("D2");
    }

    public void Save()
    {
        DB.GetContext().SaveAsync(this).Wait();
    }
}