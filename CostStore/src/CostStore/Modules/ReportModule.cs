using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CostStore.Requests;

namespace CostStore;

public class ReportModule
{
    public static void Process(string PartitionKey, string ReportId)
    {
        // Generate and save the report
    }

    public static CreateReportResponse Create(CreateReportRequest Request)
    {
        Report report = new Report();
        report.ReportId = Guid.NewGuid().ToString();
        report.PK = Request.PartitionKey;
        report.CostId = report.GetCostId();
        report.Name = Request.Name;
        report.Save();

        // Send message to queue
        QueueMessage message = new QueueMessage();
        message.PartitionKey = Request.PartitionKey;
        message.Command = "createreport";
        message.Parameter = report.CostId;
        QueueModule.SendMessage(message);

        CreateReportResponse crr = new CreateReportResponse();
        crr.ReportId = report.ReportId;

        return crr;
    }

    public static Report[] List(string PartitionKey)
    {
        var context = DB.GetContext();

        // "report-<created>-<month number>"
        string prefix = "report-";

        List<object> queryVal = new List<object>();
        queryVal.Add(prefix);

        var cl = context
            .QueryAsync<Report>(PartitionKey,
                Amazon.DynamoDBv2.DocumentModel.QueryOperator.BeginsWith,
                queryVal)
            .GetRemainingAsync().Result;

        string bucketName = CostStore.GetBucketname();

        // Specify how long the presigned URL lasts, in hours
        double timeoutDuration = 12;

        // If using the Region us-east-1, and server-side encryption with AWS KMS, you must specify Signature Version 4.
        // Region us-east-1 defaults to Signature Version 2 unless explicitly set to Version 4 as shown below.
        // For more details, see https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingAWSSDK.html#specify-signature-version
        // and https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Amazon/TAWSConfigsS3.html
        AWSConfigsS3.UseSignatureVersion4 = true;

        var region = CostStore.GetRegion();
        IAmazonS3 s3Client = new AmazonS3Client(region);

        foreach (var item in cl)
        {
            // ObjectKey syntax : <partitionkey>/imports/<guid>
            string objectKey = PartitionKey + "/Reports/" + item.ReportId;

            // Generate presigned url
            string url = GeneratePresignedURL(s3Client, bucketName, objectKey, timeoutDuration);

            item.Url = url;
            // item.Url = "https://file-examples.com/wp-content/storage/2017/02/file_example_XLS_50.xls";
        }

        return cl.ToArray();
    }

    // https://docs.aws.amazon.com/AmazonS3/latest/userguide/example_s3_Scenario_PresignedUrl_section.html

    /// <summary>
    /// Generate a presigned URL that can be used to access the file named
    /// in the objectKey parameter for the amount of time specified in the
    /// duration parameter.
    /// </summary>
    /// <param name="client">An initialized S3 client object used to call
    /// the GetPresignedUrl method.</param>
    /// <param name="bucketName">The name of the S3 bucket containing the
    /// object for which to create the presigned URL.</param>
    /// <param name="objectKey">The name of the object to access with the
    /// presigned URL.</param>
    /// <param name="duration">The length of time for which the presigned
    /// URL will be valid.</param>
    /// <returns>A string representing the generated presigned URL.</returns>
    public static string GeneratePresignedURL(IAmazonS3 client, string bucketName, string objectKey, double duration)
    {
        string urlString = string.Empty;
        try
        {
            var request = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddHours(duration),
                Verb = HttpVerb.GET
            };

            urlString = client.GetPreSignedURL(request);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error:'{ex.Message}'");
        }

        return urlString;
    }


    public static string GenerateDownloadUrl(GenerateUploadUrlRequest? request)
    {
        // TODO

        return "https://file-examples.com/wp-content/storage/2017/02/file_example_XLS_50.xls";
    }
}