namespace MaIN.Domain.Models;

public class Model
{
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public string? DownloadUrl { get; set; }
    public string? Description { get; set; }
    public string? Path { get; set; }
}

public static class KnownModels
{
    private static List<Model> Models { get; } = 
    [
        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Gemma2_2b,
            FileName = "gemma2-2b.gguf",
            DownloadUrl = "https://huggingface.co/TheBloke/gemma2-2b-quantized/resolve/main/gemma2-2b-quantized.bin",
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
            Name = KnownModelNames.Llava_7b,
            FileName = "Llava.gguf",
            DownloadUrl = string.Empty,
        },

        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Phi_mini,
            FileName = "phi3.5-3b.gguf",
            DownloadUrl = string.Empty
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
            Name = KnownModelNames.DeepSeek_R1_8b,
            FileName = "DeepSeekR1-8b.gguf",
            DownloadUrl = string.Empty
        },

        new Model()
        {
            Description = string.Empty,
            Name = KnownModelNames.Fox_1_6b,
            FileName = "Fox-1.6b.gguf",
            DownloadUrl = string.Empty
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
}

public struct KnownModelNames
{
    public const string Nomic_Embedding = "nomic";
    public const string Gemma2_2b = "gemma2:2b";
    public const string Llama3_1_8b = "llama3.1:8b";
    public const string Llama3_2_3b = "llama3.2:3b";
    public const string Phi_mini = "phi3:mini";
    public const string Llava_7b = "llava:7b";
    public const string Qwen2_5_0_5b = "qwen2.5:0.5b";
    public const string Qwen2_5_coder_3b = "qwen2.5-coder:3b";
    public const string Qwen2_5_coder_7b = "qwen2.5-coder:7b";
    public const string Qwen2_5_coder_14b = "qwen2.5-coder:14b";
    public const string DeepSeek_R1_8b = "deepseekR1-8b";
    public const string Fox_1_6b = "fox:1.6b";
}