namespace MaIN.Infrastructure.Models;

public readonly record struct DownloadProgress(
    long BytesRead,
    long? TotalBytes,
    double BytesPerSecond)
{
    public double? Percentage => TotalBytes is > 0
        ? (double)BytesRead / TotalBytes.Value * 100
        : null;

    public TimeSpan? Eta => TotalBytes.HasValue && BytesPerSecond > 0
        ? TimeSpan.FromSeconds((TotalBytes.Value - BytesRead) / BytesPerSecond)
        : null;
}
