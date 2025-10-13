using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using SixLabors.ImageSharp;
using StableDiffusion.ML.OnnxRuntime;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace MaIN.Services.Services.ImageGenServices;

public class ImageGenONNXService(MaINSettings settings) : IImageGenService
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    public Task<ChatResult?> Send(Chat chat)
    {
        var matrixCheck = Matrix<double>.Build.Dense(1, 1); // Ensures MathNet.Numerics is referenced
        StableDiffusionConfig config = GetStableDiffusionConfig();

        string prompt = chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");

        try
        {
            Image image = GenerateImage(prompt, config);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            byte[] imageBytes = ms.ToArray();
            return Task.FromResult<ChatResult?>(CreateChatResult(imageBytes));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error generating image with ONNX", ex);
        }
    }

    private StableDiffusionConfig GetStableDiffusionConfig()
    {
        ImageGenSettings imgSettings = new()
        {
            ExecutionProviderTarget = "Cpu",
            NumInferenceSteps = 8,
            GuidanceScale = 7.5,
            ModelPath = "C:\\Users\\zlote\\Desktop\\MAIN\\ImageGeneration\\ImageGeneration\\OnnxModels\\CompVis"
        };
        //ImageGenSettings imgSettings = _settings.ImageGenSettings
        //    ?? throw new InvalidOperationException("Image generation settings are not configured.");

        return new StableDiffusionConfig
        {
            NumInferenceSteps = imgSettings.NumInferenceSteps,
            GuidanceScale = imgSettings.GuidanceScale,
            ExecutionProviderTarget = Enum.Parse<StableDiffusionConfig.ExecutionProvider>(imgSettings.ExecutionProviderTarget),
            TextEncoderOnnxPath = Path.Combine(imgSettings.ModelPath, "text_encoder", "model.onnx"),
            UnetOnnxPath = Path.Combine(imgSettings.ModelPath, "unet", "model.onnx"),
            VaeDecoderOnnxPath = Path.Combine(imgSettings.ModelPath, "vae_decoder", "model.onnx"),
            TokenizerOnnxPath = Path.Combine(imgSettings.ModelPath, "cliptokenizer.onnx"),
            OrtExtensionsPath = Path.Combine(imgSettings.ModelPath, "ortextensions.dll"),
        };
    }

    private Image GenerateImage(string prompt, StableDiffusionConfig config)
    {
        try
        {
            return UNet.Inference(prompt, config);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private ChatResult CreateChatResult(byte[] imageBytes)
    {
        return new ChatResult
        {
            Done = true,
            Message = new Message
            {
                Content = ServiceConstants.Messages.GeneratedImageContent,
                Role = ServiceConstants.Roles.Assistant,
                Image = imageBytes,
                Type = MessageType.Image
            },
            Model = "CompVis", //Path.GetFileName(_settings.ImageGenSettings!.ModelPath),
            CreatedAt = DateTime.UtcNow
        };
    }
}
