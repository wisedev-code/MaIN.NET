using SixLabors.ImageSharp;
using StableDiffusion.ML.OnnxRuntime;
using System.Globalization;

public class StableDiffusionService
{
    private readonly StableDiffusionConfig _config;
    private readonly ILogger<StableDiffusionService> _logger;

    public StableDiffusionService(IConfiguration configuration, ILogger<StableDiffusionService> logger)
    {
        _logger = logger;
        var sdConfigSection = configuration.GetSection("StableDiffusionConfig");
        var configModelPath = sdConfigSection["ModelPath"];

        var modelPath = string.IsNullOrEmpty(configModelPath) ?
            throw new ArgumentNullException("Model path is not configured.") :
            configModelPath;

        _config = new StableDiffusionConfig
        {
            NumInferenceSteps = int.Parse(sdConfigSection["NumInferenceSteps"]!),
            GuidanceScale = double.Parse(sdConfigSection["GuidanceScale"]!, CultureInfo.InvariantCulture),
            ExecutionProviderTarget = Enum.Parse<StableDiffusionConfig.ExecutionProvider>(sdConfigSection["ExecutionProviderTarget"]!),
            TextEncoderOnnxPath = Path.Combine(modelPath, "text_encoder", "model.onnx"),
            UnetOnnxPath = Path.Combine(modelPath, "unet", "model.onnx"),
            VaeDecoderOnnxPath = Path.Combine(modelPath, "vae_decoder", "model.onnx"),
            TokenizerOnnxPath = Path.Combine(Directory.GetCurrentDirectory(), "cliptokenizer.onnx"),
            OrtExtensionsPath = Path.Combine(Directory.GetCurrentDirectory(), "ortextensions.dll"),
        };
    }

    public Image GenerateImage(string prompt)
    {
        try
        {
            _logger.LogInformation("Generating image for prompt: {Prompt}", prompt);
            return UNet.Inference(prompt, _config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image for prompt: {Prompt}", prompt);
            throw;
        }
    }
}
