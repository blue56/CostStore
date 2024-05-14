using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using CostStore;

[DynamoDBTable("Cost")]
public class Report
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

    public string GetCostId()
    {
        return "report-" + ReportId;
    }


    public string Name { get; set; }
    public string Created { get; set; }
    public string Url { get; set; }
    public string ReportId { get; set; }

    public void Save()
    {
        if (string.IsNullOrEmpty(Created))
        {
            Created = DateTime.Now.ToString("s");
        }

        DB.GetContext().SaveAsync(this).Wait();
    }
}