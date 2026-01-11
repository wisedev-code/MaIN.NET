using System.Text.Encodings.Web;
using System.Text.Json;

namespace MaIN.Services.Utils;

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

                // Unwrap nested JSON strings up to maxDepth
                if (root.ValueKind == JsonValueKind.String)
                {
                    current = root.GetString()!.Trim();
                    depth++;
                    continue;
                }
                else if (root.ValueKind == JsonValueKind.Object || root.ValueKind == JsonValueKind.Array)
                {

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

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