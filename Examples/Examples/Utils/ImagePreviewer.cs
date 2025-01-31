using System.Diagnostics;

namespace Examples.Utils;

class ImagePreviewer
{
    public static void ShowImage(byte[] imageData, string extension = "png")
    {
        // Validate extension
        if (string.IsNullOrWhiteSpace(extension) || extension.Contains("."))
            throw new ArgumentException("Invalid file extension");

        // Create temp file with proper extension
        string tempFile = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.{extension}"
        );

        File.WriteAllBytes(tempFile, imageData);
        
        Process.Start(new ProcessStartInfo
        {
            FileName = tempFile,
            UseShellExecute = true
        });
    }
}