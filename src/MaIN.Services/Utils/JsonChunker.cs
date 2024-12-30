using System.Text.Json;

namespace MaIN.Services.Utils;

public class JsonChunker(
    int maxTokens = 10000,
    int chunkOverlap = 200,
    int estimatedCharsPerToken = 4)
{
    private readonly int _chunkOverlap = chunkOverlap;

    private int MaxCharsPerChunk => maxTokens * estimatedCharsPerToken;

    public IEnumerable<string> ChunkJson(string jsonString)
    {
        var document = JsonDocument.Parse(jsonString);
        var root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Array => ChunkArray(root),
            JsonValueKind.Object => ChunkObject(root),
            _ => throw new ArgumentException("Input must be a JSON object or array")
        };
    }

    private IEnumerable<string> ChunkArray(JsonElement array)
    {
        var currentChunk = new List<JsonElement>();
        var currentSize = 0;

        foreach (var element in array.EnumerateArray())
        {
            var elementJson = element.GetRawText();
            var elementSize = elementJson.Length;

            // If single element is larger than max size, handle recursively
            if (elementSize > MaxCharsPerChunk)
            {
                if (element.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    foreach (var subChunk in ChunkElement(element))
                    {
                        yield return subChunk;
                    }
                    continue;
                }
            }

            // If adding this element would exceed max size, yield current chunk
            if (currentSize + elementSize > MaxCharsPerChunk && currentChunk.Any())
            {
                yield return JsonSerializer.Serialize(currentChunk);
                currentChunk.Clear();
                currentSize = 0;
            }

            currentChunk.Add(element);
            currentSize += elementSize;
        }

        if (currentChunk.Any())
        {
            yield return JsonSerializer.Serialize(currentChunk);
        }
    }

    private IEnumerable<string> ChunkObject(JsonElement obj)
    {
        var currentChunk = new Dictionary<string, JsonElement>();
        var currentSize = 0;

        foreach (var property in obj.EnumerateObject())
        {
            var propertyJson = JsonSerializer.Serialize(
                new Dictionary<string, JsonElement> { { property.Name, property.Value } }
            );
            var propertySize = propertyJson.Length;

            // If single property is larger than max size, handle recursively
            if (propertySize > MaxCharsPerChunk)
            {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    foreach (var subChunk in ChunkElement(property.Value))
                    {
                        yield return JsonSerializer.Serialize(
                            new Dictionary<string, JsonElement> { { property.Name, JsonDocument.Parse(subChunk).RootElement } }
                        );
                    }
                    continue;
                }
            }

            // If adding this property would exceed max size, yield current chunk
            if (currentSize + propertySize > MaxCharsPerChunk && currentChunk.Any())
            {
                yield return JsonSerializer.Serialize(currentChunk);
                currentChunk.Clear();
                currentSize = 0;
            }

            currentChunk[property.Name] = property.Value;
            currentSize += propertySize;
        }

        if (currentChunk.Any())
        {
            yield return JsonSerializer.Serialize(currentChunk);
        }
    }

    private IEnumerable<string> ChunkElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Array => ChunkArray(element),
            JsonValueKind.Object => ChunkObject(element),
            _ => new[] { element.GetRawText() }
        };
    }
}