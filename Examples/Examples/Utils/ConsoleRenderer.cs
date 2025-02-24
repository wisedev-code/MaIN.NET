using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Examples.Utils;

class ConsoleRenderer
{
    const double FONT_RATIO = 0.5; // Adjust based on your terminal font (0.5 for square pixels)
    const bool USE_DITHERING = true;

    public static void DisplayImage(byte[] imageData, int maxWidth)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using var image = Image.Load<Rgba32>(imageData);
        
        // High-quality resizing with Lanczos3
        var options = new ResizeOptions {
            Size = CalculateSize(image.Width, image.Height, maxWidth),
            Sampler = KnownResamplers.Lanczos3,
            Compand = true
        };

        image.Mutate(x => {
            x.Resize(options);
            if(USE_DITHERING) x.Dither(KnownDitherings.FloydSteinberg);
        });

        RenderWithHalfBlocks(image);
    }

    static Size CalculateSize(int origWidth, int origHeight, int maxWidth)
    {
        double ratio = (double)origWidth / origHeight * FONT_RATIO;
        int width = Math.Min(maxWidth, Console.WindowWidth);
        int height = (int)(width / ratio);
        return new Size(width, height);
    }

    static void RenderWithHalfBlocks(Image<Rgba32> image)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height - 1; y += 2)
            {
                var topRow = accessor.GetRowSpan(y);
                var bottomRow = accessor.GetRowSpan(y + 1);
                
                for (int x = 0; x < topRow.Length; x++)
                {
                    var top = topRow[x];
                    var bottom = bottomRow[x];
                    
                    Console.Write(
                        $"\x1b[38;2;{top.R};{top.G};{top.B}m" +  // Foreground (top)
                        $"\x1b[48;2;{bottom.R};{bottom.G};{bottom.B}m" + // Background (bottom)
                        "▄" + // Lower half block
                        "\x1b[0m"); // Reset
                }
                Console.WriteLine();
            }
        });
    }
}