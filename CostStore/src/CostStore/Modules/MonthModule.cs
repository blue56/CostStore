using CostStore.Requests;
using CostStore.Responses;

namespace CostStore;

public class MonthModule
{
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