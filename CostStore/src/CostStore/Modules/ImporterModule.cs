using System.Text.Json.Nodes;
using Amazon.S3;
using Amazon.S3.Model;
using CostStore.Requests;

namespace CostStore;

public class ImporterModule
{
    public static void Import(ImportRequest Request)
    {
        // Get file content
        string content = GetFileContent(CostStore.GetBucketname(), Request.Path);

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

            // Exchange rate
            if (Request.ExchangeRate != null && Request.ExchangeRate != 0)
            {
                c.Amount = c.Total;
                c.Rate = (decimal)Request.ExchangeRate;
                c.Total = (decimal)(c.Total * Request.ExchangeRate);
            }

            // Uplift
            if (Request.Uplift != null && Request.Uplift != 0)
            {
                if (c.Amount == null)
                {
                    c.Amount = c.Total;
                    c.Currency = c.Currency;
                }

                c.Uplift = Request.Uplift;
                c.Total = (decimal)(c.Total * c.Uplift);
            }

            // Optional data

            if (n["Name"] != null)
                c.Name = n["Name"].AsValue().ToString();

            if (n["Currency"] != null)
                c.Currency = n["Currency"].AsValue().ToString();

            if (n["Amount"] != null)
                c.Amount = decimal.Parse(n["Amount"].AsValue().ToString());

            if (n["Total"] != null)
                c.Total = decimal.Parse(n["Total"].AsValue().ToString());

            if (n["Currency"] != null)
                c.Currency = n["Currency"].AsValue().ToString();

            if (n["Uplift"] != null)
                c.Uplift = decimal.Parse(n["Uplift"].AsValue().ToString());

            if (n["UpliftDescription"] != null)
                c.UpliftDescription = n["UpliftDescription"].AsValue().ToString();

            if (n["Rate"] != null)
                c.Rate = decimal.Parse(n["Rate"].AsValue().ToString());

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

}