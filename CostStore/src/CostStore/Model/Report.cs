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

    public string Name { get; set; }
    public string Status {get; set;}
    public string Created { get; set; }
    public string Url { get; set; }
    public string ReportId { get; set; }
    public string ReportType { get; set; }
    public string Period { get; set; }


    public static string GetCostId(string ReportId)
    {
        return "report-" + ReportId;
    }

    public string GetCostId()
    {
        return GetCostId(ReportId);
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