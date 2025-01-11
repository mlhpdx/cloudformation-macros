// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Cppl.ForEachMacro;



public class Function
{
    static Function() { }
    
    public Function() { /* start X-Ray here */ }

    public async Task<JsonObject> FunctionHandler(JsonObject request, ILambdaContext context)
    {
        await Console.Out.WriteLineAsync($"\nRequest {request?.ToString() ?? string.Empty}");

        var request_id = request?.TryGetPropertyValue("requestId", out var r) == true ? (string?)(r as JsonValue)
            : throw new InvalidDataException("Request is missing requestId.");

        try {
            var region = request.TryGetPropertyValue("region", out var d) ? d as JsonValue 
                : throw new InvalidDataException("Request is missing region.");

            var account = request.TryGetPropertyValue("accountId", out var b) == true ? b as JsonValue 
                : throw new InvalidDataException("Request is missing account.");
                            
            // we're going to mutate `fragment` and use it in the response, so disconnect it from the request
            var fragment = request.Remove("fragment", out var f) ? f as JsonObject
                : throw new InvalidDataException("Request is missing fragment.");

            var template_parameters = request.TryGetPropertyValue("templateParameterValues", out var tp) ? tp as JsonObject
                : throw new InvalidDataException("Request is missing templateParameterValues.");

            var macro_parameters = request.TryGetPropertyValue("params", out var mp) ? mp as JsonObject
                : throw new InvalidDataException("Request is missing params (macro parameters).");

            var transform_id = request?.TryGetPropertyValue("transformId", out var t) == true ? (string?)(t as JsonValue)
                : throw new InvalidDataException("Request is missing transformId.");
            
            await Console.Out.WriteLineAsync($"\nRequest ID {request_id}\nTransform ID: {transform_id}");
            await Console.Out.WriteLineAsync($"\nFragment {fragment?.ToString() ?? string.Empty}\nTemplate Parameters: {template_parameters?.ToString() ?? string.Empty}");

            var paths = fragment!.FindForEachPaths().SortPathsByDepth();
            await Console.Out.WriteLineAsync($"\nPaths:\n  {string.Join("\n  ", paths)}");

            fragment!.ExpandTemplatesAtPaths(template_parameters!, paths);

            return new() {
                [ "requestId" ] = $"{request_id}",
                [ "status"] = "SUCCESS",
                [ "fragment" ] = fragment
            };
        } catch (Exception e) {
            await Console.Out.WriteLineAsync($"Runtime Error: {e}");
            return new() {
                [ "requestId" ] = $"{request_id}",
                [ "status"] = "FAILURE"
            };
        }
    }
}
