using CostStore.Requests;
using CostStore.Responses;

namespace CostStore;

public class MonthModule
{
    public static string GetMonthId(int Year, int Month)
    {
        // "month-<year>-<month number>

        return "month-" + Year + "-" + Month.ToString("D2");
    }

    public static CostMonth? Get(string PartitionKey, int Year, int Month)
    {
        var dbc = DB.GetContext();

        string monthid = GetMonthId(Year, Month);

        return dbc.LoadAsync<CostMonth>(PartitionKey, monthid).Result;
    }

    public static CostMonth[] List(string PartitionKey)
    {
        var context = DB.GetContext();

        // "month-<year>-<month number>"
        string prefix = "month";

        List<object> queryVal = new List<object>();
        queryVal.Add(prefix);

        var cl = context
            .QueryAsync<CostMonth>(PartitionKey,
                Amazon.DynamoDBv2.DocumentModel.QueryOperator.BeginsWith,
                queryVal)
            .GetRemainingAsync().Result;

        return cl.ToArray();
    }
}