

namespace Examples.Utils;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public static class ImagePreview
{
    public static void ShowImage(byte[]? imageData, string extension = "png")
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

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", tempFile); // Opens with default viewer on Linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", "-a Preview " + tempFile);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open image: {ex.Message}");
        }
    }
}

