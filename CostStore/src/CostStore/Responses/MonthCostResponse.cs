namespace CostStore.Responses;

public class MonthCostResponse
{
    public string PartitionKey { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Cost[] Cost { get; set; }
}