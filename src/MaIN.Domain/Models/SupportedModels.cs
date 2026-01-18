using MaIN.Domain.Exceptions.Models;

namespace MaIN.Domain.Models;

public class Model
{
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public string? MMProject { get; set; }
    public string? DownloadUrl { get; set; }
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
       new()
        {
            Description = "Compact 2B model for text generation, summarization, and simple Q&A",
            Name = KnownModelNames.Gemma2_2b,
            FileName = "Gemma2-2b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/gemma2_2b/resolve/main/gemma2-2b-maIN.gguf?download=true",
        },
        new()
        {
            Description = "Balanced 4B model for writing, analysis, and mathematical reasoning",
            Name = KnownModelNames.Gemma3_4b,
            FileName = "Gemma3-4b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Gemma3-4b/resolve/main/gemma3-4b.gguf?download=true",
        },
        new()
        {
            Description = "Large 12B model for complex analysis, research, and creative writing",
            Name = KnownModelNames.Gemma3_12b,
            FileName = "Gemma3-12b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Gemma3-12b/resolve/main/gemma3-12b.gguf?download=true",
        },
        new()
        {
            Description = "Large 12B model for complex analysis, research, and creative writing",
            Name = KnownModelNames.Gemma3_12b,
            FileName = "Gemma3-12b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Gemma3-12b/resolve/main/gemma3-12b.gguf?download=true",
        },
        new()
        {
            Description = "Compact 4B model optimized for efficient reasoning and general-purpose tasks",
            Name = KnownModelNames.Gemma3n_e4b,
            FileName = "Gemma3n-e4b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Gemma-3n-e4b/resolve/main/gemma-3n-e4b.gguf?download=true",
        },
        new()
        {
            Description = "Lightweight modern 1.2B model for fast inference and resource-constrained environments",
            Name = KnownModelNames.LFM2_1_2b,
            FileName = "lfm2-1.2b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Lfm2-1.2b/resolve/main/lfm2-1.2b.gguf?download=true",
        },
        new()
        {
            Description = "Mid-size 8B model balancing performance and efficiency for diverse applications",
            Name = KnownModelNames.Minicpm4_8b,
            FileName = "Minicpm4-8b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Minicpm4-8b/resolve/main/MiniCPM4-8b.gguf?download=true",
        },
        new()
        {
            Description = "Large 24B model offering advanced reasoning and comprehensive knowledge for complex tasks",
            Name = KnownModelNames.Mistral_3_2_24b,
            FileName = "Mistral3.2-24b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Mistral3.2-24b/resolve/main/Mistral3.2-24b.gguf?download=true",
        },
        new()
        {
            Description = "Specialized 4B model optimized for web development and code generation tasks",
            Name = KnownModelNames.Webgen_4b,
            FileName = "webgen-4b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/webgen-4b/resolve/main/Webgen-4b.gguf?download=true",
        },
        new()
        {
            Description = "Large 11B Polish language model with strong multilingual capabilities and reasoning",
            Name = KnownModelNames.Bielik_2_5_11b,
            FileName = "Bielik2.5-11b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Bielik2.5-11b/resolve/main/Bielik2.5-11b.gguf?download=true",
        },
        new()
        {
            Description = "Fast 8B model for multilingual tasks, translation, and logical reasoning",
            Name = KnownModelNames.Qwen3_8b,
            FileName = "Qwen3-8b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen3-8b/resolve/main/Qwen3-8b.gguf?download=true",
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new()
        {
            Description = "Advanced 14B model for complex reasoning, research, and document analysis",
            Name = KnownModelNames.Qwen3_14b,
            FileName = "Qwen3-14b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen3-14b/resolve/main/Qwen3-14b.gguf?download=true",
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new()
        {
            Description = "Specialized 7B model for algorithms, data structures, and contest programming",
            Name = KnownModelNames.OlympicCoder_7b,
            FileName = "Olympiccoder-7b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/OlympicCoder-7b/resolve/main/OlympicCoder-7b.gguf?download=true",
        },
        new()
        {
            Description = "Lightweight 3B model for chatbots, content creation, and basic coding",
            Name = KnownModelNames.Llama3_2_3b,
            FileName = "Llama3.2-3b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Llama3.2_3b/resolve/main/Llama3.2-maIN.gguf?download=true"
        },
        new()
        {
            Description = "Versatile 8B model for writing, coding, math, and general assistance",
            Name = KnownModelNames.Llama3_1_8b,
            FileName = "Llama3.1-8b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Llama3.1_8b/resolve/main/Llama3.1-maIN.gguf?download=true"
        },
        new()
        {
            Description = "Efficient 3B model for dialogue, roleplay, and conversational AI",
            Name = KnownModelNames.Hermes3_3b,
            FileName = "Hermes3-3b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Hermes3-3b/resolve/main/hermes3-3b.gguf?download=true"
        },
        new()
        {
            Description = "Enhanced 8B model for complex dialogue, storytelling, and advice",
            Name = KnownModelNames.Hermes3_8b,
            FileName = "Hermes3-8b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Hermes3_8b/resolve/main/hermes3-8b.gguf?download=true"
        },
        new()
        {
            Description = "Vision-language model for image analysis, OCR, and visual Q&A",
            Name = KnownModelNames.Llava_7b,
            FileName = "Llava.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Llava/resolve/main/Llava-maIN.gguf?download=true",
        },
        new()
        {
            Description = "Ultra-lightweight 0.5B model for simple text completion and basic tasks",
            Name = KnownModelNames.Qwen2_5_0_5b,
            FileName = "Qwen2.5-0.5b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen2.5/resolve/main/Qwen2.5-maIN.gguf?download=true"
        },
        new()
        {
            Description = "Compact 3B model for Python, JavaScript, bug fixing, and code review",
            Name = KnownModelNames.Qwen2_5_coder_3b,
            FileName = "Qwen2.5-coder-3b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen2.5-Coder-3b/resolve/main/Qwen2.5-coder-3b.gguf?download=true"
        },
        new()
        {
            Description = "Advanced 7B model for full-stack development, API design, and testing",
            Name = KnownModelNames.Qwen2_5_coder_7b,
            FileName = "Qwen2.5-coder-7b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen2.5-Coder-7b/resolve/main/Qwen2.5-coder-7b.gguf?download=true"
        },
        new()
        {
            Description = "Professional 14B model for system design, architecture, and code refactoring",
            Name = KnownModelNames.Qwen2_5_coder_14b,
            FileName = "Qwen2.5-coder-14b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Qwen2.5-Coder-14b/resolve/main/Qwen2.5-coder-14b.gguf?download=true"
        },
        new()
        {
            Description = "Efficient 3B model for mobile apps, IoT devices, and edge computing",
            Name = KnownModelNames.Phi3_5_3b,
            FileName = "phi3.5-3b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/phi3.5-3b/resolve/main/phi3.5-3b.gguf?download=true"
        },
        new()
        {
            Description = "Latest 4B model for factual Q&A, safety-focused applications, and education",
            Name = KnownModelNames.Phi4_4b,
            FileName = "phi4-4b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Phi4-4b/resolve/main/phi4-4b.gguf?download=true"
        },
        new()
        {
            Description = "Advanced 8B model for math proofs, scientific reasoning, and logical puzzles",
            Name = KnownModelNames.DeepSeek_R1_8b,
            FileName = "DeepSeekR1-8b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/DeepseekR1-8b/resolve/main/DeepSeekR1-8b-maIN.gguf?download=true",
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new()
        {
            Description = "Compact 1.5B model for basic logic, simple math, and chain-of-thought tasks",
            Name = KnownModelNames.DeepSeek_R1_1_5b,
            FileName = "DeepSeekR1-1.5b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/DeepseekR1-1.5b/resolve/main/DeepSeekR1-1.5b.gguf?download=true",
            ReasonFunction = ReasoningFunctions.ProcessDeepSeekToken
        },
        new()
        {
            Description = "Bilingual 6B model for Chinese-English translation and cultural content",
            Name = KnownModelNames.Yi_6b,
            FileName = "Yi-6b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/yi-6b/resolve/main/yi-6b.gguf?download=true"
        },
        new()
        {
            Description = "Tiny 0.1B model for keyword extraction, simple classification, and demos",
            Name = KnownModelNames.Smollm2_0_1b,
            FileName = "Smollm2-0.1b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Smollm2-0.1b/resolve/main/smollm2-0.1b.gguf?download=true"
        },
        new()
        {
            Description = "Open-source 7B model for research, benchmarking, and academic studies",
            Name = KnownModelNames.Olmo2_7b,
            FileName = "Olmo2-7b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/Olmo2-7b/resolve/main/olmo2-7b.gguf?download=true"
        },
        new()
        {
            Description = "Reasoning-focused 7B model for step-by-step problem solving and analysis",
            Name = KnownModelNames.QwQ_7b,
            FileName = "QwQ-7b.gguf",
            DownloadUrl = "https://huggingface.co/Inza124/QwQ-7b/resolve/main/qwq-7b.gguf?download=true",
            AdditionalPrompt = "- Output nothing before <think>, enclose all step-by-step reasoning (excluding the final answer) within <think>...</think>, and place the final answer immediately after the closing </think>",
            ReasonFunction = ReasoningFunctions.ProcessQwQ_QwenModToken
        },
        new()
        {
            Description = "Frontier TTS model for its size of 82 million parameters (text in/audio out).",
            Name = KnownModelNames.Kokoro_82m,
            FileName = "kokoro.onnx",
            DownloadUrl = "https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/kokoro.onnx"
        }
    ];

    public static Model GetEmbeddingModel() =>
        new()
        {
            Name = KnownModelNames.Nomic_Embedding,
            FileName = "nomicv2.gguf",
            Description = "Model used to generate embeddings.",
            DownloadUrl = "https://huggingface.co/Inza124/Nomic/resolve/main/nomicv2.gguf?download=true",
        };

    public static bool IsModelSupported(string name) =>
        Models.Any(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                        || x.Name.Replace(':', '-').Equals(name,
                            StringComparison.InvariantCultureIgnoreCase));
    
    public static Model GetModel(string path, string? name)
    {
        var model = Models.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                               || x.Name.Replace(':', '-').Equals(name,
                                                   StringComparison.InvariantCultureIgnoreCase));
        if (model is null)
        {
            throw new ModelNotSupportedException(name);
        }

        if (File.Exists(Path.Combine(path, model.FileName)))
        {
            return model;
        }

        throw new ModelNotDownloadedException(name);
    }

    public static Model? GetModelByFileName(string path, string fileName)
    {
        var isPresent = Models.Exists(x => x.FileName == fileName);
        if (!isPresent)
        {
            Console.WriteLine($"{new ModelNotSupportedException(fileName).PublicErrorMessage}");
            return null;
        }

        if (File.Exists(Path.Combine(path, fileName)))
        {
            return Models.First(x => x.FileName == fileName);
        }

        throw new ModelNotDownloadedException(fileName);
    }

    public static void AddModel(string model, string path, string? mmProject = null)
    {
        Models.Add(new Model()
        {
            Description = string.Empty,
            DownloadUrl = string.Empty,
            MMProject = mmProject,
            Name = model,
            FileName = Path.GetFileName(path),
            Path = Path.GetDirectoryName(path)
        });
    }

    public static Model GetModel(string modelName)
    {
        var model = Models.FirstOrDefault(x => x.Name.Equals(modelName, StringComparison.InvariantCultureIgnoreCase)
                                               || x.Name.Replace(':', '-').Equals(modelName,
                                                   StringComparison.InvariantCultureIgnoreCase));
        if (model is null)
        {
            throw new ModelNotSupportedException(modelName);
        }

        return model;
    }

    public static List<Model> All() => Models;
}

public struct KnownModelNames
{
    public const string Nomic_Embedding = "nomicv2";
    public const string Gemma2_2b = "gemma2:2b";
    public const string Gemma3_4b = "gemma3:4b";
    public const string Gemma3_12b = "gemma3:12b";
    public const string Gemma3n_e4b = "gemma3n:e4b";
    public const string OlympicCoder_7b = "olympiccoder:7b";
    public const string Minicpm4_8b = "minicpm4:8b";
    public const string LFM2_1_2b = "lfm2:1.2b";
    public const string Webgen_4b = "webgen:4b";
    public const string Mistral_3_2_24b = "mistral:3.2:24b";
    public const string Bielik_2_5_11b = "bielik:2.5:11b";
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

    public const string Kokoro_82m = "kokoro:82m";
}