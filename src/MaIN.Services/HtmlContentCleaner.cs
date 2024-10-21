using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MaIN.Services;

public static class HtmlContentCleaner
{
    public static string CleanHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script tags
        var scriptNodes = doc.DocumentNode.SelectNodes("//script");
        if (scriptNodes != null)
        {
            foreach (var node in scriptNodes)
            {
                node.Remove();
            }
        }

        // Remove style tags
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            foreach (var node in styleNodes)
            {
                node.Remove();
            }
        }

        // Remove common non-content elements
        var nonContentSelectors = new[] { "//header", "//nav", "//footer", "//aside", "//form" };
        foreach (var selector in nonContentSelectors)
        {
            var nodes = doc.DocumentNode.SelectNodes(selector);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Remove();
                }
            }
        }

        // Remove comments
        var commentNodes = doc.DocumentNode.SelectNodes("//comment()");
        if (commentNodes != null)
        {
            foreach (var node in commentNodes)
            {
                node.Remove();
            }
        }

        // Remove empty elements
        RemoveEmptyElements(doc.DocumentNode);

        // Remove all attributes except for 'href' and 'src'
        RemoveAttributes(doc.DocumentNode);

        // Get the cleaned HTML
        string cleanedHtml = doc.DocumentNode.InnerHtml;

        // Remove extra whitespace
        cleanedHtml = Regex.Replace(cleanedHtml, @"\s+", " ");
        cleanedHtml = Regex.Replace(cleanedHtml, @">\s+<", "><");

        return cleanedHtml.Trim();
    }

    private static void RemoveEmptyElements(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Element && string.IsNullOrWhiteSpace(node.InnerHtml))
        {
            node.Remove();
            return;
        }

        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
            RemoveEmptyElements(node.ChildNodes[i]);
        }
    }

    private static void RemoveAttributes(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Element)
        {
            var attributesToKeep = new[] { "href", "src" };
            var attributes = node.Attributes.ToList();
            foreach (var attr in attributes)
            {
                if (!attributesToKeep.Contains(attr.Name.ToLower()))
                {
                    node.Attributes.Remove(attr);
                }
            }
        }

        foreach (var childNode in node.ChildNodes)
        {
            RemoveAttributes(childNode);
        }
    }
}