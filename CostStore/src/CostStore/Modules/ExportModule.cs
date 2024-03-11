using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace CostStore;

public class ExportModule
{
    public static void Export(ExportRequest request)
    {
        // Get cost for the month
        var monthcost = CostModule.List(request.PartitionKey,
            request.Year, request.Month);

        MonthExport me = new MonthExport();
        me.Year = request.Year;
        me.Month = request.Month;
        me.Cost = monthcost;

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
