using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace CostStore;

public class DB
{
    public static DynamoDBContext GetContext()
    {
        var _region = CostStore.GetRegion();

        AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();

        clientConfig.RegionEndpoint = _region;
        AmazonDynamoDBClient client = new AmazonDynamoDBClient(clientConfig);

        DynamoDBContext context = new DynamoDBContext(client);

        return context;
    }

}
