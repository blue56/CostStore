using CostStore.Requests;

namespace CostStore;

public class CostModule
{
    public static Cost[] List(string PartitionKey, int Year,
        int Month, string CostCenterId)
    {
        var cl = List(PartitionKey, Year, Month);

        var tt = cl.Where(x => x.CostCenterId == CostCenterId);

        return tt.ToArray();
    }

    public static Cost? Get(string PartitionKey, string CostId)
    {
        var dbc = DB.GetContext();
        return dbc.LoadAsync<Cost>(PartitionKey, CostId).Result;
    }

    public static void SetCostCenter(string PartitionKey, string CostId,
        string CostCenterId)
    {
        var c = CostModule.Get(PartitionKey, CostId);
        c.CostCenterId = CostCenterId;
        c.AllocationStatus = "Manual";
        c.Save();
    }

    public static void Allocate(string PartitionKey, AllocateRequest AllocateRequest)
    {
        int Year = AllocateRequest.Year;
        int Month = AllocateRequest.Month;

        //
        var cl = CostModule.List(
            PartitionKey, 
            AllocateRequest.Year, 
            AllocateRequest.Month);

        // Get cost from priveous month
        int pyear = Year;
        int pmonth = Month;

        if (Month == 1)
        {
            pmonth = 12;
            pyear = Year - 1;
        }
        else {
            pmonth = pmonth -1;
        }

        var pcl = CostModule.List(PartitionKey, pyear, pmonth);

        foreach (var c in cl)
        {
            if (string.IsNullOrEmpty(c.CostCenterId))
            {
                // Try to find simelar cost
                var pc = pcl.Where(x => x.PartitionKey == c.PartitionKey
                && x.ResourceId == c.ResourceId
                && x.Service == c.Service).FirstOrDefault();

                if (pc != null)
                {
                    c.CostCenterId = pc.CostCenterId;
                    c.AllocationStatus = "Continued";
                    c.Save();
                }
            }
        }
    }

    public static Cost[] List(string PartitionKey, int Year,
        int Month)
    {
        var context = DB.GetContext();

        string prefix = Year + "-" + Month.ToString("D2"); 

        List<object> queryVal = new List<object>();
        //queryVal.Add(Year + "-" + Month.ToString("D2"));
        //queryVal.Add("2024-01-3409b748-4eb8-4b7b-bc85-17902887e2c7");
        //queryVal.Add("2024-01");
        queryVal.Add(prefix);

        var cl = context
            .QueryAsync<Cost>(PartitionKey,
                Amazon.DynamoDBv2.DocumentModel.QueryOperator.BeginsWith,
                queryVal)
            .GetRemainingAsync().Result;

        return cl.ToArray();
    }
}