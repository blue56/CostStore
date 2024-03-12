namespace CostStore;

public class GroupedMonthExport
{
    public int Year {get; set;}
    public int Month {get; set;}
    public Dictionary<string,Cost[]> CostCenters {get; set;}
}