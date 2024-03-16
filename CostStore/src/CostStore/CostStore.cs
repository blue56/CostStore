
using Amazon;

namespace CostStore;

public class CostStore
{
    private static RegionEndpoint _region;
    private static string _bucketname;

    public static void Initialize()
    {
        // Get from environment variable

        string Region = Environment.GetEnvironmentVariable("Region");
        _region = RegionEndpoint.GetBySystemName(Region);

        //
        _bucketname = Environment.GetEnvironmentVariable("Bucketname");

    }

    public static string GetBucketname()
    {
        return _bucketname;
    }

    public static RegionEndpoint GetRegion()
    {
        return _region;
    }

    public static Metadata GetMetadata(string PartitionKey) 
    {
        var dbc = DB.GetContext();
        return dbc.LoadAsync<Metadata>(PartitionKey, "metadata").Result;
    }
}