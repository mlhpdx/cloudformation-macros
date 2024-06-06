using System.Text.Json.Nodes;

namespace Cppl.ForEachMacro;

/// <summary>
/// Provides extension methods for working with JSON documents.
/// </summary>
public static class Extensions
{
    public static string MakeResourceContentSubstitutions(this string text, int i, string v) =>
        text.Replace("%d", $"{i}").Replace("%v", v.Trim());

    public static string MakeResourceNameSubstitutions(this string text, string suffix) =>
        $"{text}{GetSafe(suffix)}";

    private static string GetSafe(string v) =>
        v.Aggregate((text: string.Empty, is_cap: true), (a, c) => c switch
        {
            char when char.IsLetterOrDigit(c) => (a.text + (a.is_cap ? char.ToUpper(c) : c), false),
            _ => (a.text + ' ', true)
        }).text.Replace(" ", string.Empty);

	// extension method for a JsonDocument that will find all of the paths to objects having a property called 
	// "ForEach" within the document.  Uses recursion and a stack of strings to keep track of the paths, and
	// returns a list of strings that are the paths to the objects.
	public static List<string> FindForEachPaths(this JsonNode doc)
	{
		var paths = new List<string>();
		var stack = new Stack<string>();
		FindForEachPaths(doc, stack, paths);
		return paths;
	}

	// recursive method to find all of the paths to objects having a property called "ForEach" within the document.
	private static void FindForEachPaths(JsonNode element, Stack<string> stack, List<string> paths)
	{
		foreach ((var name, var value) in element.AsObject())
		{
			if (name == "ForEach")
			{
				paths.Add(string.Join(".", stack.Reverse()));
			}
			else if (value is JsonObject)
			{
				stack.Push(name);
				FindForEachPaths(value, stack, paths);
				stack.Pop();
			}
			else if (value is JsonArray)
			{
				stack.Push(name);
				var count = value.AsArray().Count;
				for (int index = 0; index < count; index++) {
					var item = value[index];
					if (item is JsonObject)
					{
						stack.Push(index.ToString());
						FindForEachPaths(item, stack, paths);
						stack.Pop();
					}
				}
				stack.Pop();
			}
		}
	}

	// extension method for a list of paths that sorts them by the number of elements in the path.
	public static List<string> SortPathsByDepth(this List<string> paths) =>
		paths.OrderBy(p => p.Split('.').Length).ToList();

	// extension method for a JsonDocument that will print the objects at a list of paths within the document. 
	public static void ExpandTemplatesAtPaths(this JsonNode doc, JsonNode parameters, List<string> paths)
	{
		var n = new JsonObject();
		foreach (var path in paths)
		{
			Console.WriteLine($"Path: {path}");
			var segments = path.Split('.');
			(var parent, var template) = GetTemplateAtPath(doc, segments);
			ExpandTemplate(parameters, segments, parent, template);
		}
	}

	// extension method for a JsonDocument that prints the object at the given path.
	private static (JsonNode parent, JsonNode template) GetTemplateAtPath(JsonNode element, string[] path)
	{
		JsonNode value = element switch {
			JsonObject o => o.TryGetPropertyValue(path[0], out var v) ? v! : throw new Exception("Oops"),
			JsonArray a => a[int.Parse(path[0])]!,
			_ => throw new Exception("Yikes")
		};
		if (path.Length == 1)
		{
			return (element, value);
		} else return GetTemplateAtPath(value, path[1..]);
	}

	private static void ExpandTemplate(JsonNode parameters, string[] path, JsonNode parent, JsonNode template)
	{
		Console.WriteLine($"Kind of Parent: {parent.GetType()}");
		Console.WriteLine(template);

		var settings = template.AsObject().Remove("ForEach", out var set) ? set : throw new Exception("Template node must have ForEach property.");
		var list_name = settings switch
		{
			JsonValue s => s.GetValue<string>(),
			JsonObject s => s["List"]?.GetValue<string>(),
			_ => throw new Exception("Expected a string or object containing a 'List' property (which is a string).")
		};

		var use_index = settings switch
		{
			JsonObject o when o.TryGetPropertyValue("ResourceKeySuffix", out var n) => string.Equals("Index", (string)n!),
			_ => false
		};

		var list = parameters[list_name!]?.AsArray().Select(n => n?.GetValue<string>() ?? throw new Exception("Null value in list.")).ToList()
			?? throw new Exception("Expected parameter to be a list of strings (CommaDelimitedList).");
		var text = template.ToString();

		IEnumerable<string> instances = list.Select((s, i) => text.MakeResourceContentSubstitutions(i, s));
		IEnumerable<JsonNode> nodes = instances.Select(s => JsonNode.Parse(s)!);

		switch (parent)
		{
			case JsonObject o:
				o.Remove(path[^1]);
				foreach (var (node, i) in nodes.Select((n, i) => (n, i)))
				{
					o[path[^1].MakeResourceNameSubstitutions(use_index ? $"{i}" : list[i])] = node;
				}
				break;
			case JsonArray a:
				a.Remove(template);
				foreach (var node in nodes)
				{
					a.Add(node);
				}
				break;
			default:
				throw new Exception("Unexpected parent type.");
		}
	}
}
