using Microsoft.KernelMemory.DataFormats;
using Tesseract;

namespace MaIN.Services.Services;

public class OcrWrapper : IOcrEngine
{
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = new CancellationToken())
    {
        if (!imageContent.CanRead)
            throw new ArgumentException("Stream is not readable.");

        byte[] imageBytes;
        using (var memoryStream = new MemoryStream())
        {
            await imageContent.CopyToAsync(memoryStream, cancellationToken);
            imageBytes = memoryStream.ToArray();
        }
        
        using var engine = new TesseractEngine(
            Path.Combine(AppContext.BaseDirectory, "tessdata"),
            "eng",
            EngineMode.TesseractAndLstm);
        
        using var img = Pix.LoadFromMemory(imageBytes);
        using var page = engine.Process(img);

        return page.GetText();
    }
    
}