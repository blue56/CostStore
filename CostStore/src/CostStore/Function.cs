using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using CostStore.Requests;

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
    public string FunctionHandler(JsonNode json, ILambdaContext context)
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

        }
        else if (json["Method"].AsValue().ToString() == "Import")
        {
            ImportRequest importRequest
              = JsonSerializer.Deserialize<ImportRequest>(json, options);

            ImporterModule.Import(importRequest);
        }
        else if (json["Method"].AsValue().ToString() == "Export")
        {
            ExportRequest exportRequest
              = JsonSerializer.Deserialize<ExportRequest>(json, options);

            ExportModule.Export(exportRequest);
        }

        return "OK";
    }
}
