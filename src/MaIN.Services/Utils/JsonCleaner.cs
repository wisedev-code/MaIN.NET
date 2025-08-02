using MaIN.Domain.Models;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;

public static class JsonCleaner
{
    public static string? CleanAndUnescape(string json, int maxDepth = 5)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        string current = json.Trim();
        int depth = 0;

        while (depth < maxDepth)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(current);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    current = root.GetString()!.Trim();
                    depth++;
                    continue;
                }
                else if (root.ValueKind == JsonValueKind.Object || root.ValueKind == JsonValueKind.Array)
                {
                    // Serialize with relaxed escaping to unescape unicode characters
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    // Note: We serialize the JsonElement directly, which will output real Unicode chars
                    return JsonSerializer.Serialize(root, options);
                }
                else
                {
                    return null;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }
        return null;
    }
}