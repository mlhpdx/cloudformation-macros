// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;
using Cppl.Utilities.AWS;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Cppl.ForEachMacro;

public static class Extensions {
    public static JsonArray ToJsonArray(this IEnumerable<MimeKit.InternetAddress> addresses) {
        return addresses.Select(a => a.ToString()).ToJsonArray();
    }

    public static JsonArray ToJsonArray(this IEnumerable<string> strings) {
        return new JsonArray(strings.Select(s => JsonValue.Create(s)).ToArray());
    }
}

public class Function
{
    static Function() { }
    
    public Function() : this() { /* start X-Ray here */ }

    public async Task<JsonObject> FunctionHandler(JsonObject request, ILambdaContext context)
    {
        try {
            var region = request.TryGetPropertyValue("region", out var d) ? d as JsonValue 
                : throw new InvalidDataException("Request is missing region.");

            var account = request.TryGetPropertyValue("account", out var b) == true ? b as JsonValue 
                : throw new InvalidDataException("Request is missing account.");
                            
            var fragment = request.TryGetPropertyValue("fragment", out var o) == true ? o as JsonObject 
                : throw new InvalidDataException("Request is missing fragment.");

            var template_parameters = request.TryGetPropertyValue("templateParameterValues", out var p) ? p as JsonObject
                : throw new InvalidDataException("Request is missing templateParameterValues.");

            var macro_parameters = request.TryGetPropertyValue("params", out var p) ? p as JsonObject
                : throw new InvalidDataException("Request is missing params (macro parameters).");

            var transform_id = bucket?.TryGetPropertyValue("transformId", out var t) == true ? (string?)(t as JsonValue)
                : throw new InvalidDataException("Request is missing transformId.");
            
            var request_id = bucket?.TryGetPropertyValue("requestId", out var r) == true ? (string?)(r as JsonValue)
                : throw new InvalidDataException("Request is missing requestId.");

            await Console.Out.WriteLineAsync($"\nRequest ID {request_id}\nTransform ID: {transform_id}");

            return new() {
                [ "requestId" ] = $"{request_id}",
                [ "status"] = "SUCCESS",
                [ "fragment" ] = fragment
            };
        } catch (Exception e) {
            await Console.Out.WriteLineAsync($"Runtime Error: {e.ToString()}");
            return new() {
                [ "requestId" ] = $"{request_id}",
                [ "status"] = "FAILURE"
            };
        }
    }
}
