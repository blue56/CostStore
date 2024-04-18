namespace CostStore.Requests;

public class SetCostCenterRequest
{
    public string PartitionKey { get; set; }
    public string CostId {get; set;}
    public string CostCenterId {get; set;}
}