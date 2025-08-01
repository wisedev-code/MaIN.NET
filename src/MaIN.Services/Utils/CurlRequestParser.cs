using MaIN.Domain.Entities.Agents.AgentSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaIN.Services.Utils;
public static class CurlRequestParser
{
    public static void PopulateRequestFromCurl(HttpRequestMessage request, string curlRaw)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(curlRaw))
            throw new ArgumentException("curl string is empty");

        // Normalize multi-line curl: usuń backslash+newline i nadmiarowe spacje
        string curl = Regex.Replace(curlRaw, @"\\\s*\n", " ");
        curl = Regex.Replace(curl, @"\s{2,}", " ").Trim();

        // Metoda HTTP - sprawdzaj -X i --request
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
                request.Method ??= HttpMethod.Get; // domyślnie GET
            }
        }

        // URL - najpierw --url, potem pierwszy URL po curl
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
                throw new InvalidOperationException("No URL found in curl string and RequestUri is null.");
            }
        }

        // Nagłówki - obsługa -H i --header, w pojedynczych lub podwójnych cudzysłowach
        var headerPattern = @"(?:-H|--header)\s+['""]?([^:'""]+):\s*([^'""]+)['""]?";
        foreach (Match headerMatch in Regex.Matches(curl, headerPattern, RegexOptions.IgnoreCase))
        {
            string key = headerMatch.Groups[1].Value.Trim();
            string value = headerMatch.Groups[2].Value.Trim();

            // DODANA ZMIANA: Sprawdź, czy nagłówek Authorization już istnieje
            if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && request.Headers.Contains("Authorization"))
            {
                // Jeśli nagłówek Authorization już istnieje, nie podmieniaj go
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                if (request.Content == null)
                    request.Content = new StringContent(""); // dummy content for headers

                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        // Dane (payload) - może być wiele -d lub --data
        //var dataMatches = Regex.Matches(curl, @"(?:-d|--data(?:-raw)?)\s+(['""])(.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var processedCurl = PreprocessCurlString(curl);
        var dataMatches = Regex.Matches(processedCurl, @"(?:-d|--data(?:-raw)?)\s+(['""])((?:\\.|(?!\1).)*)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);


        if (request.Method == HttpMethod.Get)
        {
            // GET - dodaj dane jako query string
            var queryParams = new List<string>();
            foreach (Match m in dataMatches)
            {
                var data = m.Groups[2].Value.Trim();
                if (data.StartsWith("@"))
                    throw new NotSupportedException("Nie obsługiwany -d z plikiem przy GET.");

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
            // POST, PUT itp. - ustaw body z pierwszego -d
            if (dataMatches.Count > 0)
            {
                var rawData = dataMatches[0].Groups[2].Value;

                rawData = rawData.Replace("__SINGLE_QUOTE__", "'");
                rawData = rawData.Replace("__DOUBLE_QUOTE__", "\"");
                rawData = rawData.Replace("__BACKSLASH__", "\\");

                if (rawData.StartsWith("@"))
                    throw new NotSupportedException("Obsługa -d z plikiem nie jest zaimplementowana.");

                request.Content = new StringContent(rawData, Encoding.UTF8, "application/json");
            }
        }
    }


    public static string PreprocessCurlString(string curlRaw)
    {
        if (string.IsNullOrEmpty(curlRaw))
            return curlRaw;

        string result = curlRaw;

        // 1. Replace shell escaped single quotes inside single-quoted strings: '\'' -> __SINGLE_QUOTE__
        // This is the main culprit breaking regex
        result = Regex.Replace(result, @"'\\''", "__SINGLE_QUOTE__");

        // 2. Optionally, handle escaped backslashes or quotes inside double quotes:
        // Replace \" with __DOUBLE_QUOTE__ and \\ with __BACKSLASH__ if needed
        // (You can add this if you see your JSON payload uses double quotes with escapes)
        result = result.Replace("\\\"", "__DOUBLE_QUOTE__");
        result = result.Replace("\\\\", "__BACKSLASH__");

        // 3. Normalize multi-line curl command: remove backslash+newline (already done in your code)
        result = Regex.Replace(result, @"\\\s*\n", " ");

        // 4. Replace multiple spaces by single space
        result = Regex.Replace(result, @"\s{2,}", " ").Trim();

        return result;
    }
}