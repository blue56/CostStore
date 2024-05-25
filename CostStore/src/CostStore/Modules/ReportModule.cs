using Amazon;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using CostStore.Requests;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;

namespace CostStore;

public class ReportModule
{
    public static CreateReportResponse CreateMonthReport(CreateMonthReportRequest Request)
    {
        Report report = new Report();
        report.ReportId = Guid.NewGuid().ToString();
        report.PK = Request.PartitionKey;
        report.CostId = report.GetCostId();
        report.Name = Request.Name;
        report.ReportType = "monthreport";
        report.Period = Request.Month;
        report.Status = "Queued";
        report.Save();

        // Send message to queue
        QueueMessage message = new QueueMessage();
        message.PartitionKey = Request.PartitionKey;
        message.Command = "generatereport";
        message.Parameter = report.ReportId;
        QueueModule.SendMessage(message);

        CreateReportResponse crr = new CreateReportResponse();
        crr.ReportId = report.ReportId;

        return crr;
    }

    public static void ProcessMonthReport(Report Report)
    {
        // Generate and save the report
        var sheets = new Dictionary<string, object>();

        var metadata = CostStore.GetMetadata(Report.PK);

        // Get all cost for month
        var ps = Report.Period.Split("-");
        int year = int.Parse(ps[0]);
        int month = int.Parse(ps[1]);

        var costlist = CostModule.List(Report.PK, year, month);

        var config = new OpenXmlConfiguration
        {
            DynamicColumns = new DynamicExcelColumn[] {
                    new DynamicExcelColumn("Service"){Index=1,Width=10},
                    new DynamicExcelColumn("Name"){Index=1,Width=10},
                    new DynamicExcelColumn("Resource id"){Index=1,Width=15},
                    new DynamicExcelColumn("Amount"){Index=1,Width=10},
                    new DynamicExcelColumn("Currency"){Index=1,Width=10},
                    new DynamicExcelColumn("Total"){Index=1,Width=10},
                    new DynamicExcelColumn("Cost Center"){Index=1,Width=20}
                }
        };

        var stream = new MemoryStream();

        var rowList = new List<Dictionary<string, object>>();

        // Write information sheet
        var rowvalues = new Dictionary<string, object>();
        rowvalues.Add("Year", year);
        rowvalues.Add("Month", month);
        rowvalues.Add("Currency", metadata.Currency);
        rowList.Add(rowvalues);

        //        MiniExcel.SaveAs(stream, rowList, true, "Information");
        sheets.Add("Information", rowList);

        // Write cost sheet

        rowList = new List<Dictionary<string, object>>();

        foreach (var c in costlist)
        {
            rowvalues = new Dictionary<string, object>();

            rowvalues.Add("Service", c.Service);
            rowvalues.Add("Name", c.Name);
            rowvalues.Add("ResourceId", c.ResourceId);
            rowvalues.Add("Amount", c.Amount);
            rowvalues.Add("Currency", c.Currency);
            rowvalues.Add("Exchange rate (" + c.Currency + " / " + metadata.Currency + ")", c.Rate);
            rowvalues.Add("Total", c.Total);
            rowvalues.Add("Cost center", c.CostCenterId);

            rowList.Add(rowvalues);
        }

        sheets.Add("Cost", rowList);

        //        MiniExcel.SaveAs(stream, rowList, true, sheetName);
        MiniExcel.SaveAs(stream, sheets);

        var region = CostStore.GetRegion();
        IAmazonS3 _s3Client = new AmazonS3Client(region);

        var Bucketname = CostStore.GetBucketname();

        string path = Report.PK + "/Reports/" + Report.ReportId;

        SaveFile(_s3Client, Bucketname, path, stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Report.Status = "Done";
        Report.Save();
    }

    public static Report? Get(string PartitionKey, string ReportId)
    {
        var dbc = DB.GetContext();

        string reportid = Report.GetCostId(ReportId);

        return dbc.LoadAsync<Report>(PartitionKey, reportid).Result;
    }

    public static void Process(string PartitionKey, string ReportId)
    {
        // Read report from DB
        var report = Get(PartitionKey, ReportId);

        if (report.ReportType == "monthreport")
        {
            ProcessMonthReport(report);
        }
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

    /*    public static CreateReportResponse Create(CreateReportRequest Request)
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
            message.Parameter = report.ReportId;
            QueueModule.SendMessage(message);

            CreateReportResponse crr = new CreateReportResponse();
            crr.ReportId = report.ReportId;

            return crr;
        }
    */
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
}