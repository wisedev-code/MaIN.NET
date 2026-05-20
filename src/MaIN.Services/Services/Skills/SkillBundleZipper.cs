using System.IO.Compression;

namespace MaIN.Services.Services.Skills;

/// <summary>
/// Shared logic for packaging a skill bundle into a temporary zip suitable for upload.
/// Used by all provider uploaders so multipart payload shape stays consistent.
/// </summary>
internal static class SkillBundleZipper
{
    // Names that should never end up in a uploaded skill bundle, regardless of how the user
    // organises their skills/ directory. Mostly VCS, IDE, OS, and build artefacts that would
    // either leak source-control data or just inflate the payload.
    private static readonly HashSet<string> IgnoredEntries = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg",
        ".DS_Store", "Thumbs.db",
        ".vs", ".vscode", ".idea",
        "node_modules",
        "bin", "obj",
        "__pycache__"
    };

    /// <summary>
    /// Zips an entire directory (preserves the bundle folder as the top-level entry so server-side
    /// paths look like "skill-name/SKILL.md", matching the per-file upload convention).
    /// Common VCS/IDE/build artefacts are excluded.
    /// </summary>
    public static string ZipDirectory(string bundlePath)
    {
        var tempZip = NewTempZipPath();
        var rootName = Path.GetFileName(bundlePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        using var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create);
        foreach (var file in Directory.EnumerateFiles(bundlePath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(bundlePath, file);
            if (IsIgnored(relative)) continue;

            var entryPath = string.IsNullOrEmpty(rootName)
                ? relative.Replace('\\', '/')
                : $"{rootName}/{relative.Replace('\\', '/')}";

            zip.CreateEntryFromFile(file, entryPath, CompressionLevel.Optimal);
        }
        return tempZip;
    }

    private static bool IsIgnored(string relativePath)
    {
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (IgnoredEntries.Contains(segment)) return true;
        }
        return false;
    }

    /// <summary>
    /// Zips a single SKILL.md file under the canonical "{skillName}/SKILL.md" entry path.
    /// Used when the bundle is a lone .md file rather than a folder.
    /// </summary>
    public static string ZipSingleFile(string skillName, string filePath)
    {
        var tempZip = NewTempZipPath();
        using var zip = ZipFile.Open(tempZip, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(filePath, $"{skillName}/SKILL.md", CompressionLevel.Optimal);
        return tempZip;
    }

    public static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }

    private static string NewTempZipPath() =>
        Path.Combine(Path.GetTempPath(), $"main-skill-{Guid.NewGuid():N}.zip");
}
