using System.Text.RegularExpressions;

namespace MainFE;

public static class ExtensionMethods
{
    public static string PrepareMd(this string content)
    {
        // Bold text correction: ** text** -> **text**
        string patternBold = @"\*\*\s*(.*?)\s*\*\*";
        content = Regex.Replace(content, patternBold, @"**$1**");

        // Italic text correction: * text* -> *text*
        string patternItalic = @"\*\s*(.*?)\s*\*";
        content = Regex.Replace(content, patternItalic, @"*$1*");

        // Bold and Italic text correction: *** text*** -> ***text***
        string patternBoldItalic = @"\*\*\*\s*(.*?)\s*\*\*\*";
        content = Regex.Replace(content, patternBoldItalic, @"***$1***");

        // Inline code correction: ` code ` -> `code`
        string patternCode = @"`\s*(.*?)\s*`";
        content = Regex.Replace(content, patternCode, @"`$1`");

        // Heading correction: # Heading -> #Heading
        for (int i = 1; i <= 6; i++)
        {
            string patternHeading = $@"^(\s*#{i}\s+)\s*(.*?)\s*$";
            string replacementHeading = $@"#{i} $2";
            content = Regex.Replace(content, patternHeading, replacementHeading, RegexOptions.Multiline);
        }

        // Link correction: [ text](url) -> [text](url)
        string patternLink = @"\[\s*(.*?)\s*\]\(\s*(.*?)\s*\)";
        content = Regex.Replace(content, patternLink, @"[$1]($2)");

        // Image correction: ![ alt text](url) -> ![alt text](url)
        string patternImage = @"!\[\s*(.*?)\s*\]\(\s*(.*?)\s*\)";
        content = Regex.Replace(content, patternImage, @"![$1]($2)");

        return content;
    }
    
    public static string GetApiUrl()
    {
        return Environment.GetEnvironmentVariable("API_URL") ?? throw new InvalidOperationException("API_URL environment variable is not set");
    }

}