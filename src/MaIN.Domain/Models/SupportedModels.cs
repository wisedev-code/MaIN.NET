namespace MaIN.Domain.Models;

public class Model
{
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public required string DownloadUrl { get; set; }
    public string? AdditionalPrompt { get; set; }
    public string? Description { get; set; }
    public string? Path { get; set; }
    public Func<string, ThinkingState, LLMTokenValue>? ReasonFunction { get; set; }
    public bool HasReasoning() => ReasonFunction is not null;
}

public static class KnownModels
{
    private static List<Model> Models { get; } =
    [
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Gemma2_2b,
            FileName = "Gemma2-2b.gguf",
            DownloadUrl = string.Empty,
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Gemma3_4b,
            FileName = "Gemma3-4b.gguf",
            DownloadUrl = string.Empty,
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Gemma3_12b,
            FileName = "Gemma3-12b.gguf",
            DownloadUrl = string.Empty,
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen3_8b,
            FileName = "Qwen3-8b.gguf",
            DownloadUrl = string.Empty,
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen3_14b,
            FileName = "Qwen3-14b.gguf",
            DownloadUrl = string.Empty,
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.OlympicCoder_7b,
            FileName = "Olympiccoder-7b.gguf",
            DownloadUrl = string.Empty,
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Llama3_2_3b,
            FileName = "Llama3.2-3b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Llama3_1_8b,
            FileName = "Llama3.1-8b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Hermes3_3b,
            FileName = "Hermes3-3b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Hermes3_8b,
            FileName = "Hermes3-8b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Llava_7b,
            FileName = "Llava.gguf",
            DownloadUrl = string.Empty,
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen2_5_0_5b,
            FileName = "Qwen2.5-0.5b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen2_5_coder_3b,
            FileName = "Qwen2.5-coder-3b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen2_5_coder_7b,
            FileName = "Qwen2.5-coder-7b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Qwen2_5_coder_14b,
            FileName = "Qwen2.5-coder-14b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Phi3_5_3b,
            FileName = "phi3.5-3b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Phi4_4b,
            FileName = "phi4-4b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.DeepSeek_R1_8b,
            FileName = "DeepSeekR1-8b.gguf",
            DownloadUrl = string.Empty,
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.DeepSeek_R1_1_5b,
            FileName = "DeepSeekR1-1.5b.gguf",
            DownloadUrl = string.Empty,
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Yi_6b,
            FileName = "Yi-6b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Smollm2_0_1b,
            FileName = "Smollm2-0.1b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Olmo2_7b,
            FileName = "Olmo2-7b.gguf",
            DownloadUrl = string.Empty
        },
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.QwQ_7b,
            FileName = "QwQ-7b.gguf",
            DownloadUrl = string.Empty,
            AdditionalPrompt = "- Output nothing before <think>, enclose all step-by-step reasoning (excluding the final answer) within <think>...</think>, and place the final answer immediately after the closing </think>",
            ReasonFunction = ReasoningFunctions.ProcessQwQ_QwenModToken
        }
    ];

    public static Model GetEmbeddingModel() =>
        new()
        {
            Name = KnownModelNames.Nomic_Embedding,
            FileName = "nomic.gguf",
            Description = "Model used to generate embeddings.",
            DownloadUrl = string.Empty,
        };

    public static Model GetModel(string path, string? name)
    {
        var model = Models.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                               || x.Name.Replace(':', '-').Equals(name,
                                                   StringComparison.InvariantCultureIgnoreCase));
        if (model is null)
        {
            //todo support domain specific exceptions
            throw new Exception($"Model {name} is not supported");
        }

        if (File.Exists(Path.Combine(path, model.FileName)))
        {
            return model;
        }

        throw new Exception($"Model {name} is not downloaded");
    }

    public static Model? GetModelByFileName(string path, string fileName)
    {
        var isPresent = Models.Exists(x => x.FileName == fileName);
        if (!isPresent)
        {
            //todo support domain specific exceptions
            Console.WriteLine($"Model {fileName} is not supported");
            return null;
        }

        if (File.Exists(Path.Combine(path, fileName)))
        {
            return Models.First(x => x.FileName == fileName);
        }

        throw new Exception($"Model {fileName} is not downloaded");
    }

    public static void AddModel(string model, string path)
    {
        Models.Add(new Model()
        {
            Description = string.Empty,
            DownloadUrl = string.Empty,
            Name = model,
            FileName = $"{Path.GetFileName(path)}",
            Path = path
        });
    }

    public static Model GetModel(string modelName)
    {
        var model = Models.FirstOrDefault(x => x.Name.Equals(modelName, StringComparison.InvariantCultureIgnoreCase)
                                               || x.Name.Replace(':', '-').Equals(modelName,
                                                   StringComparison.InvariantCultureIgnoreCase));
        if (model is null)
        {
            //todo support domain specific exceptions
            throw new NotSupportedException($"Model {modelName} is not supported");
        }

        return model;
    }

    public static List<Model> All() => Models;
}

public struct KnownModelNames
{
    public const string Nomic_Embedding = "nomic";
    public const string Gemma2_2b = "gemma2:2b";
    public const string Gemma3_4b = "gemma3:4b";
    public const string Gemma3_12b = "gemma3:12b";
    public const string OlympicCoder_7b = "olympiccoder:7b";
    public const string Llama3_1_8b = "llama3.1:8b";
    public const string Llama3_2_3b = "llama3.2:3b";
    public const string Hermes3_3b = "hermes3:3b";
    public const string Hermes3_8b = "hermes3:8b";
    public const string Llava_7b = "llava:7b";
    public const string Qwen2_5_0_5b = "qwen2.5:0.5b";
    public const string Qwen2_5_coder_3b = "qwen2.5-coder:3b";
    public const string Qwen2_5_coder_7b = "qwen2.5-coder:7b";
    public const string Qwen2_5_coder_14b = "qwen2.5-coder:14b";
    public const string DeepSeek_R1_8b = "deepseekR1:8b";
    public const string DeepSeek_R1_1_5b = "deepseekR1:1.5b";
    public const string QwQ_7b = "qwq:7b";
    public const string Qwen3_8b = "qwen3:8b";
    public const string Qwen3_14b = "qwen3:14b";
    public const string Olmo2_7b = "olmo2:7b";
    public const string Phi3_5_3b = "phi3.5:3b";
    public const string Phi4_4b = "phi4:4b";
    public const string Smollm2_0_1b = "smollm2:0.1b";
    public const string Yi_6b = "yi:6b";

}