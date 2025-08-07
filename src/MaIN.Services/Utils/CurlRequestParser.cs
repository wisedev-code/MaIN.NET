using System.Text;
using System.Text.RegularExpressions;

namespace MaIN.Services.Utils;
public static class CurlRequestParser
{
    public static void PopulateRequestFromCurl(HttpRequestMessage request, string curlRaw)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(curlRaw))
            throw new ArgumentException("The curl string cannot be null or empty", nameof(curlRaw));

        string curl = Regex.Replace(curlRaw, @"\\\s*\n", " ");
        curl = Regex.Replace(curl, @"\s{2,}", " ").Trim();

        if (request.Method == null || request.Method == HttpMethod.Get)
        {
            var methodMatch = Regex.Match(curl, @"(?:-X|--request)\s+(\w+)", RegexOptions.IgnoreCase);
            if (methodMatch.Success)
            {
                request.Method = new HttpMethod(methodMatch.Groups[1].Value.ToUpperInvariant());
            }
            else if (curl.Contains("--get"))
            {
                request.Method = HttpMethod.Get;
            }
            else
            {
                request.Method ??= HttpMethod.Get;
            }
        }

        if (request.RequestUri == null)
        {
            var urlMatch = Regex.Match(curl, @"(?:--url\s+|curl\s+)(['""]?)(https?://[^\s'""]+)\1", RegexOptions.IgnoreCase);
            if (!urlMatch.Success)
            {
                urlMatch = Regex.Match(curl, @"(['""])(https?://[^\s'""]+)\1", RegexOptions.IgnoreCase);
            }
            if (urlMatch.Success)
            {
                request.RequestUri = new Uri(urlMatch.Groups[2].Value);
            }
            else
            {
                throw new InvalidOperationException("No URL was found in the curl string, and the request URI is null");
            }
        }

        var headerPattern = @"(?:-H|--header)\s+['""]?([^:]+):\s*(.+?)['""]?(?=\s+-|$)";
        foreach (Match headerMatch in Regex.Matches(curl, headerPattern, RegexOptions.IgnoreCase))
        {
            string key = headerMatch.Groups[1].Value.Trim();
            string value = headerMatch.Groups[2].Value.Trim();

            if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && request.Headers.Contains("Authorization"))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                if (request.Content == null)
                    request.Content = new StringContent(""); 

                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        var processedCurl = PreprocessCurlString(curl);
        var dataMatches = Regex.Matches(processedCurl, @"(?:-d|--data(?:-raw)?)\s+(['""])((?:\\.|(?!\1).)*)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);


        if (request.Method == HttpMethod.Get)
        {
            var queryParams = new List<string>();
            foreach (Match m in dataMatches)
            {
                var data = m.Groups[2].Value.Trim();
                if (data.StartsWith("@"))
                    throw new NotSupportedException("Unsupported -d with file for GET");

                queryParams.Add(data);
            }

            if (queryParams.Count > 0)
            {
                var builder = new UriBuilder(request.RequestUri);
                var existingQuery = builder.Query;
                string newQuery = existingQuery.Length > 1 ? existingQuery.Substring(1) + "&" + string.Join("&", queryParams) : string.Join("&", queryParams);
                builder.Query = newQuery;
                request.RequestUri = builder.Uri;
            }
        }
        else
        {
            if (dataMatches.Count > 0)
            {
                var rawData = dataMatches[0].Groups[2].Value;

                // Restore original characters from placeholders (used during preprocessing)

                rawData = rawData.Replace("__SINGLE_QUOTE__", "'");
                rawData = rawData.Replace("__DOUBLE_QUOTE__", "\"");
                rawData = rawData.Replace("__BACKSLASH__", "\\");

                // curl -d starting with '@' indicates a file; not supported here
                if (rawData.StartsWith("@"))
                    throw new NotSupportedException("Handling -d with file is not implemented.");

                request.Content = new StringContent(rawData, Encoding.UTF8, "application/json");
            }
        }
    }


    private static string PreprocessCurlString(string curlRaw)
    {
        if (string.IsNullOrEmpty(curlRaw))
            return curlRaw;

        string result = curlRaw;
        result = Regex.Replace(result, @"'\\''", "__SINGLE_QUOTE__");

        result = result.Replace("\\\"", "__DOUBLE_QUOTE__");
        result = result.Replace("\\\\", "__BACKSLASH__");

        result = Regex.Replace(result, @"\\\s*\n", " ");

        result = Regex.Replace(result, @"\s{2,}", " ").Trim();

        return result;
    }
}