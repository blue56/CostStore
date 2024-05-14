using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace CostStore;

public class QueueModule
{
    public static void Process(QueueMessage Message)
    {
        if (Message != null)
        {
            if (Message.Command == "createreport")
            {
                ReportModule.Process(Message.PartitionKey, Message.Parameter);
            }
            else if (Message.Command == "import")
            {
                ImporterModule.Process(Message.PartitionKey, Message.Parameter);
            }
        }
    }

    public static void SendMessage(QueueMessage Message)
    {
        // Create the Amazon SQS client
        var _region = CostStore.GetRegion();

        AmazonSQSConfig config = new AmazonSQSConfig();
        config.RegionEndpoint = _region;

        var sqsClient = new AmazonSQSClient(config);

        string sqsUrl = Environment.GetEnvironmentVariable("QueueUrl");
        string sqsMessage = "";

        sqsMessage = JsonSerializer.Serialize(Message);

        SendMessageResponse responseSendMsg =
            sqsClient.SendMessageAsync(sqsUrl, sqsMessage).Result;
    }
}