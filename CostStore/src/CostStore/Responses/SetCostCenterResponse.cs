namespace CostStore.Responses;

public class SetCostCenterResponse
{
    public string PartitionKey { get; set; }
    public string CostId {get; set;}
    public string CostCenterId {get; set;}
}