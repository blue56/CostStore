using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace CostStore;

[DynamoDBTable("Cost")]
public class Cost
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

    public decimal? BaseCost
    {
        get; set;
    }

    public string? BaseCostCurrency
    {
        get; set;
    }

    public decimal? Uplift 
    {
        get; set;
    }

    public string? UpliftDescription
    {
        get; set;
    }

    public decimal? ExchangeRate
    {
        get; set;
    }

    public string Name
    {
        get; set;
    }

    public string ResourceId
    {
        get; set;
    }

    public decimal Total
    {
        get; set;
    }

    public string Currency
    {
        get; set;
    }

    public string CostCenterId
    {
        get; set;
    }

    public string Service
    {
        get; set;
    }

    public string AllocationStatus
    {
        get; set;
    }

    public string GetCostId(int Year, int Month)
    {
        return Year + "-" + Month.ToString("D2") + "-" + Service + "-" + ResourceId;
    }

    public void Save()
    {
        DB.GetContext().SaveAsync(this).Wait();
    }
}