namespace MaIN.Domain.Models;

public struct Model
{
    public string Name { get; set; }
    public string FileName { get; set; }
    public string DownloadUrl { get; set; }
    public string Description { get; set; }
    public string Path { get; set; }
}

public struct KnownModels
{
    internal static Dictionary<string, Model> Models => new()
    {
        {
            KnownModelNames.Gemma2_2b, new Model()
            {
                Description = string.Empty,
                Name = KnownModelNames.Gemma2_2b,
                FileName = "gemma2-2b-maIN.gguf",
                DownloadUrl = "https://huggingface.co/TheBloke/gemma2-2b-quantized/resolve/main/gemma2-2b-quantized.bin",
            }
        },
        {
            KnownModelNames.Llama3_2_3b, new Model()
            {
                Description = string.Empty,
                Name = KnownModelNames.Llama3_2_3b,
                FileName = "Llama3.2-maIN.gguf",
                DownloadUrl = string.Empty
            }
        },
        {
            KnownModelNames.Llama3_1_8b, new Model()
            {
                Description = string.Empty,
                Name = KnownModelNames.Llama3_1_8b,
                FileName = "Llama3.1-maIN.gguf",
                DownloadUrl = string.Empty
            }
        },
        {
           KnownModelNames.Llava_7b, new Model()
           {
               Description = string.Empty,
               Name = KnownModelNames.Llava_7b,
               FileName = "Llava-maIN.gguf",
               DownloadUrl = string.Empty,
           }
        },
        {
            KnownModelNames.Phi_mini, new Model()
            {
                Description = string.Empty,
                Name = KnownModelNames.Phi_mini,
                FileName = "phi3.5-maIN.gguf",
                DownloadUrl = string.Empty
            }
        },
        {
            KnownModelNames.Qwen2_5_0_5b, new Model()
            {
                Description = string.Empty,
                Name = KnownModelNames.Qwen2_5_0_5b,
                FileName = "Qwen2.5-maIN.gguf",
                DownloadUrl = string.Empty
            }
        }
    };

    public static Model GetEmbeddingModel() =>
        new()
        {
            Name = KnownModelNames.Nomic_Embedding,
            FileName = "nomic-maIN.gguf",
            Description = "Model used to generate embeddings.",
            DownloadUrl = string.Empty,
        };

    public static Model GetModel(string path, string name)
    {
        var isPresent = Models.TryGetValue(name, out var model);
        if (!isPresent)
        {
            //todo support domain specific exceptions
            throw new Exception($"Model {name} is not supported");
        }

        if (File.Exists(Path.Combine(path, model.FileName)))
        {
            return Models[name];  
        }

        throw new Exception($"Model {name} is not downloaded");
    } 
    
    public static Model? GetModelByFileName(string path, string fileName)
    {
        var models = Models.Values.ToList();
        var isPresent = models.Exists(x => x.FileName == fileName);
        if (!isPresent)
        {
            //todo support domain specific exceptions
            Console.WriteLine($"Model {fileName} is not supported");
            return null;
        }

        if (File.Exists(Path.Combine(path, fileName)))
        {
            return models.First(x => x.FileName == fileName);  
        }

        throw new Exception($"Model {fileName} is not downloaded");
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
}