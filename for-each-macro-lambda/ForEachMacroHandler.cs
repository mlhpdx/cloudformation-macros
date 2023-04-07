// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Cppl.ForEachMacro;

public static class Extensions {
    public static string MakeSubstitutions(this string text, int i, string v) {
        if (text.Contains("%")) return text.Replace("%d", $"{i}").Replace("%v", v.Trim());
        else return $"{text}{i}";
    }
}

public class Function
{
    static Function() { }
    
    public Function() { /* start X-Ray here */ }

    public async Task<JsonObject> FunctionHandler(JsonObject request, ILambdaContext context)
    {
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

            var resources = fragment!.TryGetPropertyValue("Resources", out var rs) ? (rs as JsonObject)! 
                : throw new InvalidDataException("Request fragment is missing the Resources property."); 

            foreach (var kv in resources!.ToArray()) {
                var name = kv.Key;
                var resource = kv.Value as JsonObject;
                if (resource!.ContainsKey("ForEach")) {
                    // value must be the name of a parameter that is a list of strings
                    resources.Remove(name);
                    resource.Remove("ForEach", out var fe);

                    var text = resource.ToString();

                    var parameter = (string)fe!;
                    var list = template_parameters![parameter]! as JsonArray;
                    foreach((string v, int i) in list!.Select((v, i) => ((string)v!, i))) {
                        var instance = text.MakeSubstitutions(i, v);
                        resources[name.MakeSubstitutions(i, v)] = JsonObject.Parse(instance);
                    }
                }
            }

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
