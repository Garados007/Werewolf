using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Tools.LangConfigGenerator;

public class Walker
{
    private static Regex variableDetector = new Regex(
        @"\{([^\}]+)\}",
        RegexOptions.Compiled
    );

    public async Task Walk(string schemaPath, string sourcePath)
    {
        if (!Directory.Exists(Path.GetDirectoryName(schemaPath)))
            throw new FileNotFoundException("directory of schema path not found", schemaPath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("source path not found", sourcePath);
        using var schemaStream = new FileStream(schemaPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var schemaNode = (schemaStream.Length == 0 ? null : JsonNode.Parse(schemaStream)) ?? new JsonObject();
        var sourceElement = await JsonDocument.ParseAsync(sourceStream);
        Walk("", schemaNode.AsObject(), sourceElement.RootElement);
        schemaStream.Position = 0;
        using var writer = new Utf8JsonWriter(schemaStream, new JsonWriterOptions
        {
            Indented = true
        });
        schemaNode.WriteTo(writer);
        await writer.FlushAsync();
        schemaStream.SetLength(schemaStream.Position);
    }

    public void Walk(string path, JsonObject schema, JsonElement node)
    {
        Console.WriteLine($"Check [/{path}]");

        if (!schema.ContainsKey("description"))
            schema["description"] = JsonValue.Create("");

        if (node.ValueKind == JsonValueKind.String)
        {
            CheckVariables(schema, node.GetString()!);
            return;
        }

        if (node.ValueKind == JsonValueKind.Object)
        {
            var nodes = (schema["nodes"] ?? (schema["nodes"] = new JsonObject())).AsObject();

            foreach (var entry in node.EnumerateObject())
            {
                Walk(
                    path.Length == 0 ? entry.Name : $"{path}/{entry.Name}",
                    (nodes[entry.Name] ?? (nodes[entry.Name] = new JsonObject())).AsObject(),
                    entry.Value
                );
            }
        }
    }

    private void CheckVariables(JsonObject schema, string value)
    {
        var list = (schema["variables"] ?? (schema["variables"] = new JsonObject())).AsObject();
        foreach (Match match in variableDetector.Matches(value))
        {
            var name = match.Groups[1].Value;
            if (list.ContainsKey(name))
                continue;
            list[name] = JsonValue.Create("");
        }
        if (list.Count == 0)
            schema.Remove("variables");
    }
}