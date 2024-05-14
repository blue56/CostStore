namespace CostStore;

public class QueueMessage
{
    public string PartitionKey {get; set;}
    public string Command {get; set;}
    public string Parameter {get; set;}
}