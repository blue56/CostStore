using CostStore.Requests;
using CostStore.Responses;

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

    public static SetCostCenterResponse SetCostCenter(SetCostCenterRequest request)
    {
        var c = CostModule.Get(request.PartitionKey, request.CostId);
        c.CostCenterId = request.CostCenterId;
        c.AllocationStatus = "Manual";
        c.Save();

        SetCostCenterResponse response = new SetCostCenterResponse();
        response.CostCenterId = request.CostCenterId;
        response.CostId = request.CostId;
        response.PartitionKey = request.PartitionKey;

        return response;
    }

    public static MonthCostResponse GetMonthCost(MonthCostRequest request)
    {
        MonthCostResponse response = new MonthCostResponse();

        var month = MonthModule.Get(request.PartitionKey, request.Year, request.Month);

        var cl = CostModule.List(
            request.PartitionKey, 
            request.Year, 
            request.Month);

        response.Cost = cl;
        response.Year = request.Year;
        response.Month = request.Month;
        response.Name = month.Name;
        response.PartitionKey = request.PartitionKey;

        return response;
    }

    public static CheckResponse Check(CheckRequest checkRequest)
    {
        int Year = checkRequest.Year;
        int Month = checkRequest.Month;

        //
        var cl = CostModule.List(
            checkRequest.PartitionKey, 
            checkRequest.Year, 
            checkRequest.Month);

        var ac = cl.Where(x => x.CostCenterId == null).Count();

        CheckResponse checkResponse = new CheckResponse();
        checkResponse.Year = checkRequest.Year;
        checkResponse.Month = checkRequest.Month;
        checkResponse.PartitionKey = checkRequest.PartitionKey;

        // ac holds the number of cost object where costcenterid not set
        checkResponse.IsCostAllocated = (ac == 0);

        return checkResponse;
    }

    public static void Allocate(AllocateRequest AllocateRequest)
    {
        int Year = AllocateRequest.Year;
        int Month = AllocateRequest.Month;

        //
        var cl = CostModule.List(
            AllocateRequest.PartitionKey, 
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

        var pcl = CostModule.List(AllocateRequest.PartitionKey, pyear, pmonth);

        foreach (var c in cl)
        {
            if (string.IsNullOrEmpty(c.CostCenterId))
            {
                // Try to find simelar cost
                var pc = pcl.Where(x => x.PK == c.PK
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

    public static Cost[] ListFilterService(string PartitionKey, int Year,
        int Month, string Service)
    {
        var cl = List(PartitionKey, Year, Month);

        var tt = cl.Where(x => x.Service == Service);

        return tt.ToArray();
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