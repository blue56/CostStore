using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using CostStore.Requests;

namespace CostStore;

public class ImporterModule
{
    public static GenerateUploadUrlResponse GenerateUploadUrl(GenerateUploadUrlRequest Request)
    {
        string bucketName = Request.BucketName;

        // ObjectKey syntax : <partitionkey>/imports/<guid>
        string objectKey = Request.PartitionKey + "/imports/" + Guid.NewGuid().ToString();

        // Specify how long the presigned URL lasts, in hours
        double timeoutDuration = 12;

        // If using the Region us-east-1, and server-side encryption with AWS KMS, you must specify Signature Version 4.
        // Region us-east-1 defaults to Signature Version 2 unless explicitly set to Version 4 as shown below.
        // For more details, see https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingAWSSDK.html#specify-signature-version
        // and https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Amazon/TAWSConfigsS3.html
        AWSConfigsS3.UseSignatureVersion4 = true;

        var region = CostStore.GetRegion();
        IAmazonS3 s3Client = new AmazonS3Client(region);

        string url = GeneratePresignedURL(s3Client, bucketName, objectKey, timeoutDuration);

        GenerateUploadUrlResponse response = new GenerateUploadUrlResponse();
        response.Url = url;
        response.Bucketname = bucketName;
        response.Path = objectKey;

        return response;
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
                Verb = HttpVerb.PUT
            };

            urlString = client.GetPreSignedURL(request);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error:'{ex.Message}'");
        }

        return urlString;
    }

    public static CreateImportResponse Create(CreateImportRequest Request)
    {
        Import import = new Import();
        import.ImportId = Guid.NewGuid().ToString();
        import.Type = "CSV";
        import.PK = Request.PartitionKey;
        import.UploadUrl = Request.UploadUrl;
        import.Overwrite = Request.Overwrite;
        import.Name = Request.Name;

        if (Request.ExchangeRate != null)
        {
            import.ExchangeRate = (decimal)Request.ExchangeRate;
        }

        import.CostId = import.GetCostId();

        import.Save();

        // Send message to SQS

        // Create the Amazon SQS client
        var _region = CostStore.GetRegion();

        AmazonSQSConfig config = new AmazonSQSConfig();
        config.RegionEndpoint = _region;

        var sqsClient = new AmazonSQSClient(config);

        string sqsUrl = Environment.GetEnvironmentVariable("QueueUrl");
        string sqsMessage = "";

        sqsMessage = JsonSerializer.Serialize(import);

        SendMessageResponse responseSendMsg =
            sqsClient.SendMessageAsync(sqsUrl, sqsMessage).Result;

        CreateImportResponse response = new CreateImportResponse();
        response.ImportId = import.ImportId;
        return response;
    }

    public static Import[] List(ListImportsRequest Request)
    {
        var context = DB.GetContext();

        // "import-<year>-<month number>"
        string prefix = "import";

        List<object> queryVal = new List<object>();
        queryVal.Add(prefix);

        var cl = context
            .QueryAsync<Import>(Request.PartitionKey,
                Amazon.DynamoDBv2.DocumentModel.QueryOperator.BeginsWith,
                queryVal)
            .GetRemainingAsync().Result;

        return cl.ToArray();
    }

    public static void Import(Import Import)
    {
        string originalPath = new Uri(Import.UploadUrl).AbsolutePath;

        string path = originalPath.TrimStart('/');

        // Get file content
        string content = GetFileContent(CostStore.GetBucketname(), path);

        var metadata = CostStore.GetMetadata(Import.PK);

        JsonNode jn = JsonArray.Parse(content);

        JsonArray ja = (JsonArray)jn;

        foreach (var n in ja)
        {
            int Year = int.Parse(n["Year"].AsValue().ToString());
            int Month = int.Parse(n["Month"].AsValue().ToString());

            // Get month
            var m = MonthModule.Get(Import.PK, Year, Month);

            if (m == null)
            {
                // Create month
                CostMonth newMonth = new CostMonth();
                newMonth.PK = Import.PK;
                newMonth.CostId = MonthModule.GetMonthId(Year, Month);
                newMonth.Year = Year;
                newMonth.Month = Month;
                newMonth.Save();
            }
            else {
                // Check if month is locked
            }

            Cost c = new Cost();

            // Required data
            c.PK = Import.PK;
            c.Service = n["Service"].AsValue().ToString();
            c.ResourceId = n["ResourceId"].AsValue().ToString();
            c.CostId = c.GetCostId(Year, Month);
            c.Amount = decimal.Parse(n["Amount"].AsValue().ToString());
            c.Currency = n["Currency"].AsValue().ToString();

            // Optional data

            if (n["Name"] != null)
                c.Name = n["Name"].AsValue().ToString();

            if (n["UpliftDescription"] != null)
                c.UpliftDescription = n["UpliftDescription"].AsValue().ToString();

            // Uplift
            if (Import.Uplift != null
                && Import.Uplift != 0
                && c.Amount != null
                && c.Amount != 0)
            {
                c.Uplift = Import.Uplift;

                // In original currency
                c.UpliftAmount = (decimal)(c.Amount * c.Uplift);
            }
            else
            {
                c.UpliftAmount = 0;
            }

            // Exchange rate
            if (Import.ExchangeRate != null
                && Import.ExchangeRate != 0
                && c.Amount != null
                && c.Amount != 0)
            {
                c.Rate = (decimal)Import.ExchangeRate;
                //c.Total = (decimal)(c.Total * Import.ExchangeRate);
                c.Total = (decimal)((c.Amount + c.UpliftAmount) * Import.ExchangeRate);
            }
            else if (c.Currency != metadata.Currency)
            {
                // Cost currency not the same as cost store currency
                // and no exchange rate provided
                throw new MissingExchangeRateException();

//                throw new ApplicationException("No exchange rate provided for cost");
            }
            else 
            {
                // Same currency as store
                c.Total = (decimal)(c.Amount + c.UpliftAmount);
            }

            // Check if it exists
            var ec = CostModule.Get(Import.PK, c.GetCostId(Year, Month));

            if (Import.Overwrite == true || ec == null)
            {
                c.Save();
            }
        }
    }

    public static void Import(ImportRequest Request)
    {
        // Get file content
        string content = GetFileContent(CostStore.GetBucketname(), Request.Path);

        var metadata = CostStore.GetMetadata(Request.PartitionKey);

        JsonNode jn = JsonArray.Parse(content);

        JsonArray ja = (JsonArray)jn;

        foreach (var n in ja)
        {
            int Year = int.Parse(n["Year"].AsValue().ToString());
            int Month = int.Parse(n["Month"].AsValue().ToString());

            Cost c = new Cost();

            // Required data
            c.PK = Request.PartitionKey;
            c.Service = n["Service"].AsValue().ToString();
            c.ResourceId = n["ResourceId"].AsValue().ToString();
            c.CostId = c.GetCostId(Year, Month);
            c.Amount = decimal.Parse(n["Amount"].AsValue().ToString());
            c.Currency = n["Currency"].AsValue().ToString();

            // Optional data

            if (n["Name"] != null)
                c.Name = n["Name"].AsValue().ToString();

            if (n["UpliftDescription"] != null)
                c.UpliftDescription = n["UpliftDescription"].AsValue().ToString();

            // Uplift
            if (Request.Uplift != null
                && Request.Uplift != 0
                && c.Amount != null
                && c.Amount != 0)
            {
                c.Uplift = Request.Uplift;

                // In original currency
                c.UpliftAmount = (decimal)(c.Amount * c.Uplift);

                c.Total = (decimal)(c.Amount + c.UpliftAmount);
            }

            // Exchange rate
            if (Request.ExchangeRate != null
                && Request.ExchangeRate != 0
                && c.Amount != null
                && c.Amount != 0)
            {
                c.Rate = (decimal)Request.ExchangeRate;
                c.Total = (decimal)(c.Total * Request.ExchangeRate);
            }
            else if (c.Currency != metadata.Currency)
            {
                // Cost currency not the same as cost store currency
                // and no exchange rate provided
                throw new MissingExchangeRateException();
                //throw new ApplicationException("No exchange rate provided for cost");
            }

            // Check if it exists
            var ec = CostModule.Get(Request.PartitionKey, c.GetCostId(Year, Month));

            if (Request.Overwrite == true || ec == null)
            {
                c.Save();
            }
        }
    }

    public static string GetFileContent(string Bucketname, string Key)
    {
        var region = CostStore.GetRegion();

        var _client = new AmazonS3Client(region);

        var request = new GetObjectRequest();
        request.BucketName = Bucketname;
        request.Key = Key;

        GetObjectResponse response = _client.GetObjectAsync(request).Result;
        StreamReader reader = new StreamReader(response.ResponseStream);
        string content = reader.ReadToEnd();
        return content;
    }

    public static Import? Get(string PartitionKey, string SortKey)
    {
        var dbc = DB.GetContext();
        return dbc.LoadAsync<Import>(PartitionKey, SortKey).Result;
    }

    public static void Process(string PartitionKey, string ImportId)
    {
        var import = Get(PartitionKey, ImportId);

        Process(import);
    }

    public static void Process(Import? import)
    {
        // Fetch import from db
        try
        {
            Import(import);

            import.Status = "Success";
            import.Save();
        }
        catch (MissingExchangeRateException ex)
        {
            import.Status = "Currency does not match.";
            import.Save();            
        }
        catch (Exception ex)
        {
            import.Status = "Failed: " + ex.ToString();
            import.Save();
        }
    }
}

[Serializable]
internal class MissingExchangeRateException : ApplicationException
{
    public MissingExchangeRateException()
    {
    }

    public MissingExchangeRateException(string? message) : base(message)
    {
    }

    public MissingExchangeRateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected MissingExchangeRateException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}