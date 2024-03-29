using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace CostStore;

public class ExportModule
{
    public static void GroupedExport(GroupedExportRequest request)
    {
        // Get cost for the month
        var monthcost = CostModule.List(request.PartitionKey,
            request.Year, request.Month);

        var grouping = from p in monthcost
                       group p by p.CostCenterId into g
                       select new { CostCenterId = g.Key, Costlist = g.ToList() };

        // Get metadata for cost store
        Metadata metadata = CostStore.GetMetadata(request.PartitionKey);

        GroupedMonthExport me = new GroupedMonthExport();
        me.Year = request.Year;
        me.Month = request.Month;
        me.Currency = metadata.Currency;

        List<CostCenterCost> cccList = new List<CostCenterCost>();

        foreach (var g in grouping)
        {
            CostCenterCost ccc = new CostCenterCost();

            if (g.CostCenterId == null)
            {
                ccc.CostCenterId = "Unassigned";
                ccc.Cost = g.Costlist.ToArray();
            }
            else
            {
                ccc.CostCenterId = g.CostCenterId;
                ccc.Cost = g.Costlist.ToArray();
            }

            cccList.Add(ccc);
        }

        me.CostCenters = cccList.ToArray();

        // Create an S3 client
        var _s3Client = new AmazonS3Client(CostStore.GetRegion());

        // Make distribution report
        var costjson = JsonSerializer.Serialize(me);

        byte[] byteArray = Encoding.ASCII.GetBytes(costjson);
        MemoryStream stream = new MemoryStream(byteArray);

        SaveFile(_s3Client, CostStore.GetBucketname(), request.Path,
                stream, "application/json");
    }

    public static void Export(ExportRequest request)
    {
        // Get cost for the month
        Cost[] monthcost = null;

        if (request.Service == null)
        {
            monthcost = CostModule.List(request.PartitionKey,
                request.Year, request.Month);
        }
        else
        {
            monthcost = CostModule.ListFilterService(request.PartitionKey,
                request.Year, request.Month, request.Service);
        }

        // Get metadata for cost store
        Metadata metadata = CostStore.GetMetadata(request.PartitionKey);

        MonthExport me = new MonthExport();
        me.Year = request.Year;
        me.Month = request.Month;
        me.Cost = monthcost;
        me.Currency = metadata.Currency;

        // Create an S3 client
        var _s3Client = new AmazonS3Client(CostStore.GetRegion());

        // Make distribution report
        var costjson = JsonSerializer.Serialize(me);

        byte[] byteArray = Encoding.ASCII.GetBytes(costjson);
        MemoryStream stream = new MemoryStream(byteArray);

        SaveFile(_s3Client, CostStore.GetBucketname(), request.Path,
                stream, "application/json");
    }

    public static void SaveFile(IAmazonS3 _s3Client, string Bucketname,
        string S3Path, Stream Stream, string ContentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = Bucketname,
            Key = S3Path,
            ContentType = ContentType,
            InputStream = Stream
        };

        _s3Client.PutObjectAsync(putRequest).Wait();
    }

}

