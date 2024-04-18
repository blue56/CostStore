using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using CostStore.Requests;
using CostStore.Responses;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CostStore;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public JsonNode FunctionHandler(JsonNode json, ILambdaContext context)
    {
        CostStore.Initialize();

        JsonObject jo = (JsonObject)json;

        var options = new JsonSerializerOptions
        {
            Converters =
                {
                        new JsonStringEnumConverter()
                },
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // this won't work
            PropertyNameCaseInsensitive = true
        };

        if (json["Method"].AsValue().ToString() == "Allocate")
        {
            AllocateRequest allocateRequest
              = JsonSerializer.Deserialize<AllocateRequest>(json, options);

            CostModule.Allocate(allocateRequest);

            return JsonNode.Parse("\"OK\"");
        }
        if (json["Method"].AsValue().ToString() == "MonthCost")
        {
            MonthCostRequest request
              = JsonSerializer.Deserialize<MonthCostRequest>(json, options);

            var response = CostModule.GetMonthCost(request);

            string jsonString = JsonSerializer.Serialize(response);

            return JsonNode.Parse(jsonString);
        }
        if (json["Method"].AsValue().ToString() == "SetCostCenter")
        {
            SetCostCenterRequest request
              = JsonSerializer.Deserialize<SetCostCenterRequest>(json, options);

            var response = CostModule.SetCostCenter(request);

            string jsonString = JsonSerializer.Serialize(response);

            return JsonNode.Parse(jsonString);
        }
        if (json["Method"].AsValue().ToString() == "CreateImport")
        {
            CreateImportRequest request
              = JsonSerializer.Deserialize<CreateImportRequest>(json, options);

            //var response = CostModule.SetCostCenter(request);
            var response = ImporterModule.Create(request);

            string jsonString = JsonSerializer.Serialize(response);

            return JsonNode.Parse(jsonString);
        }
        if (json["Method"].AsValue().ToString() == "ListImports")
        {
            ListImportsRequest request
              = JsonSerializer.Deserialize<ListImportsRequest>(json, options);

            //var response = CostModule.SetCostCenter(request);
            var response = ImporterModule.List(request);

            string jsonString = JsonSerializer.Serialize(response);

            return JsonNode.Parse(jsonString);
        }
        else if (json["Method"].AsValue().ToString() == "Import")
        {
            ImportRequest importRequest
              = JsonSerializer.Deserialize<ImportRequest>(json, options);

            ImporterModule.Import(importRequest);

            return JsonNode.Parse("\"OK\"");
        }
        else if (json["Method"].AsValue().ToString() == "Export")
        {
            ExportRequest exportRequest
              = JsonSerializer.Deserialize<ExportRequest>(json, options);

            ExportModule.Export(exportRequest);

            return JsonNode.Parse("\"OK\"");
        }
        else if (json["Method"].AsValue().ToString() == "GroupedExport")
        {
            GroupedExportRequest exportRequest
              = JsonSerializer.Deserialize<GroupedExportRequest>(json, options);

            ExportModule.GroupedExport(exportRequest);

            return JsonNode.Parse("\"OK\"");
        }
        else if (json["Method"].AsValue().ToString() == "Check")
        {
            CheckRequest checkRequest
              = JsonSerializer.Deserialize<CheckRequest>(json, options);

            CheckResponse response = CostModule.Check(checkRequest);

            string jsonString = JsonSerializer.Serialize(response);

            return JsonNode.Parse(jsonString);
        }
        else if (json["Method"].AsValue().ToString() == "Months")
        {
            string pk = json["PartitionKey"].AsValue().ToString();
            var months = MonthModule.List(pk);

            string jsonString = JsonSerializer.Serialize(months);

            return JsonNode.Parse(jsonString);
        }
        else {
            return JsonNode.Parse("\"Unkown operation\"");
        }
    }
}
