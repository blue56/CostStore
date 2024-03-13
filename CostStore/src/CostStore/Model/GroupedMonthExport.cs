namespace CostStore;

public class GroupedMonthExport
{
    public int Year {get; set;}
    public int Month {get; set;}
    public CostCenterCost[] CostCenters {get; set;}
}