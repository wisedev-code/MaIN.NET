using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace MaIN.Services.Services.LLMService.Memory;

public static class DocumentProcessor
{
    public static string ProcessDocument(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        return extension switch
        {
            ".pdf" => ProcessPdf(filePath),
            ".docx" => ProcessDocx(filePath),
            ".xlsx" or ".xls" => ProcessExcel(filePath),
            ".jpg" or ".jpeg" or ".png" or ".tiff" or ".bmp" => ProcessImage(filePath),
            ".txt" => ProcessTextFile(filePath),
            ".rtf" => ProcessRtf(filePath),
            ".html" or ".htm" => ProcessHtml(filePath),
            _ => throw new NotSupportedException($"Format {extension} not supported")
        };
    }

    private static string ProcessPdf(string pdfPath)
    {
        var result = new StringBuilder();

        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            var tableRegions = FindTableRegions(words);

            var nonTableWords = words.Where(w =>
                !tableRegions.Any(r => r.Contains(w.BoundingBox))).ToList();

            if (nonTableWords.Any())
            {
                var textContent = ProcessTextContent(nonTableWords);
                result.AppendLine(textContent);
            }
            
            foreach (var region in tableRegions)
            {
                var tableWords = words.Where(w => region.Contains(w.BoundingBox)).ToList();
                var tableMarkdown = CreateMarkdownTable(tableWords);
                result.AppendLine(tableMarkdown);
                result.AppendLine();
            }
            
            result.AppendLine("---");
        }

        return result.ToString();
    }

    private static List<PdfRectangle> FindTableRegions(List<Word> words)
    {
        var regions = new List<PdfRectangle>();
        var rows = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
            .OrderByDescending(g => g.Key)
            .ToList();

        for (int i = 0; i < rows.Count - 2; i++)
        {
            var row1 = rows[i].OrderBy(w => w.BoundingBox.Left).ToList();
            var row2 = rows[i + 1].OrderBy(w => w.BoundingBox.Left).ToList();
            var row3 = rows[i + 2].OrderBy(w => w.BoundingBox.Left).ToList();

            if (HasColumnAlignment(row1, row2, row3))
            {
                int startRow = i;
                int endRow = i + 2;

                for (int j = i + 3; j < rows.Count; j++)
                {
                    var nextRow = rows[j].OrderBy(w => w.BoundingBox.Left).ToList();
                    if (HasColumnAlignment(row3, rows[j - 1].OrderBy(w => w.BoundingBox.Left).ToList(), nextRow))
                    {
                        endRow = j;
                    }
                    else
                    {
                        break;
                    }
                }

                var tableWords = new List<Word>();
                for (int r = startRow; r <= endRow; r++)
                {
                    tableWords.AddRange(rows[r]);
                }

                double left = tableWords.Min(w => w.BoundingBox.Left) - 5;
                double bottom = tableWords.Min(w => w.BoundingBox.Bottom) - 5;
                double right = tableWords.Max(w => w.BoundingBox.Right) + 5;
                double top = tableWords.Max(w => w.BoundingBox.Top) + 5;

                regions.Add(new PdfRectangle(left, bottom, right, top));
                i = endRow;
            }
        }

        return regions;
    }

    private static bool HasColumnAlignment(List<Word> row1, List<Word> row2, List<Word> row3)
    {
        var xPos1 = row1.Select(w => w.BoundingBox.Left).ToList();
        var xPos2 = row2.Select(w => w.BoundingBox.Left).ToList();
        var xPos3 = row3.Select(w => w.BoundingBox.Left).ToList();

        double tolerance = 10.0;
        int alignedCount = 0;

        foreach (var x1 in xPos1)
        {
            bool aligned2 = xPos2.Any(x2 => Math.Abs(x1 - x2) < tolerance);
            bool aligned3 = xPos3.Any(x3 => Math.Abs(x1 - x3) < tolerance);

            if (aligned2 && aligned3)
            {
                alignedCount++;
            }
        }

        return alignedCount >= 2 && row1.Count >= 2 && row2.Count >= 2 && row3.Count >= 2;
    }

    private static string CreateMarkdownTable(List<Word> tableWords)
    {
        var rows = tableWords
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
            .OrderByDescending(g => g.Key)
            .ToList();

        if (!rows.Any()) return string.Empty;

        var columnPositions = GetColumnPositions(rows);
        int columnCount = columnPositions.Count;

        if (columnCount < 2)
        {
            columnCount = 2;
            columnPositions = new List<double> { tableWords.Max(w => w.BoundingBox.Left) / 2 };
        }

        var sb = new StringBuilder();
        sb.Append("|");
        var headerRow = rows.First().OrderBy(w => w.BoundingBox.Left).ToList();

        for (int i = 0; i < columnCount; i++)
        {
            double start = i == 0 ? 0 : columnPositions[i - 1];
            double end = i < columnPositions.Count - 1 ? columnPositions[i] : double.MaxValue;

            var cellWords = headerRow
                .Where(w => w.BoundingBox.Left >= start && w.BoundingBox.Left < end)
                .OrderBy(w => w.BoundingBox.Left)
                .ToList();

            string cellText = string.Join(" ", cellWords.Select(w => w.Text));
            sb.Append($" {CleanCellText(cellText)} ;");
        }

        sb.AppendLine();
        sb.Append("|");
        
        for (int i = 0; i < columnCount; i++)
        {
            sb.Append(" --- |");
        }

        sb.AppendLine();

        foreach (var row in rows.Skip(1))
        {
            sb.Append("|");
            var rowWords = row.OrderBy(w => w.BoundingBox.Left).ToList();

            for (int i = 0; i < columnCount; i++)
            {
                double start = i == 0 ? 0 : columnPositions[i - 1];
                double end = i < columnPositions.Count - 1 ? columnPositions[i] : double.MaxValue;

                var cellWords = rowWords
                    .Where(w => w.BoundingBox.Left >= start && w.BoundingBox.Left < end)
                    .OrderBy(w => w.BoundingBox.Left)
                    .ToList();

                string cellText = string.Join(" ", cellWords.Select(w => w.Text));
                sb.Append($" {CleanCellText(cellText)} |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static List<double> GetColumnPositions(List<IGrouping<double, Word>> rows)
    {
        var allPositions = new List<double>();
        foreach (var row in rows)
        {
            allPositions.AddRange(row.Select(w => w.BoundingBox.Left));
        }

        var tolerance = 10.0;
        var clusters = new List<List<double>>();

        foreach (var pos in allPositions.OrderBy(p => p))
        {
            bool added = false;
            foreach (var cluster in clusters)
            {
                if (Math.Abs(cluster.Average() - pos) < tolerance)
                {
                    cluster.Add(pos);
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                clusters.Add(new List<double> { pos });
            }
        }

        return clusters
            .Where(c => c.Count > Math.Max(2, rows.Count / 4))
            .OrderBy(c => c.Average())
            .Select(c => c.Average())
            .ToList();
    }

    private static string ProcessTextContent(List<Word> words)
    {
        var sb = new StringBuilder();
        var rows = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
            .OrderByDescending(g => g.Key);

        double prevRowHeight = 0;

        foreach (var row in rows)
        {
            string line = string.Join(" ", row.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));

            if (string.IsNullOrWhiteSpace(line)) continue;

            var firstWord = row.OrderBy(w => w.BoundingBox.Left).FirstOrDefault();
            double wordHeight = 0;

            if (firstWord != null)
            {
                wordHeight = firstWord.BoundingBox.Height;
                var letters = firstWord.Letters.ToList();
                
                if (letters.Any())
                {
                    double estimatedFontSize = letters.First().FontSize;
                    if (estimatedFontSize > 0)
                    {
                        wordHeight = estimatedFontSize;
                    }
                }
            }

            if (wordHeight > prevRowHeight * 1.2 && wordHeight > 10)
            {
                if (wordHeight > 14)
                {
                    sb.AppendLine($"## {line}");
                }
                else
                {
                    sb.AppendLine($"### {line}");
                }
            }
            else if (line.TrimStart().StartsWith("•") || line.TrimStart().StartsWith("-") ||
                     Regex.IsMatch(line.TrimStart(), @"^\d+\."))
            {
                int index = Math.Max(1, line.TrimStart().IndexOfAny(['•', '-', '.']));
                if (index < line.TrimStart().Length - 1)
                {
                    sb.AppendLine($"* {line.TrimStart().Substring(index + 1).Trim()}");
                }
                else
                {
                    sb.AppendLine($"* {line.TrimStart()}");
                }
            }
            else
            {
                sb.AppendLine(line);
                sb.AppendLine();
            }

            prevRowHeight = wordHeight;
        }

        return sb.ToString();
    }

    private static string CleanCellText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        text = text.Trim();
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Replace("|", "\\|");

        return text;
    }

    private static bool IsLikelyHeader(IEnumerable<Word> row, IEnumerable<Word> nextRow)
    {
        var enumerable = row.ToList();
        var words = nextRow as Word[] ?? nextRow.ToArray();
        if (!enumerable.Any() || !words.Any())
            return false;

        bool hasLettersFontInfo = enumerable.Any(w => w.Letters.Any());
        bool nextHasLettersFontInfo = words.Any(w => w.Letters.Any());

        if (hasLettersFontInfo && nextHasLettersFontInfo)
        {
            double rowAvgFontSize = enumerable
                .SelectMany(w => w.Letters)
                .Where(l => l.FontSize > 0)
                .Select(l => l.FontSize)
                .DefaultIfEmpty(0)
                .Average();

            double nextRowAvgFontSize = words
                .SelectMany(w => w.Letters)
                .Where(l => l.FontSize > 0)
                .Select(l => l.FontSize)
                .DefaultIfEmpty(0)
                .Average();

            return rowAvgFontSize > nextRowAvgFontSize * 1.2;
        }

        double rowAvgHeight = enumerable.Average(w => w.BoundingBox.Height);
        double nextRowAvgHeight = words.Average(w => w.BoundingBox.Height);

        return rowAvgHeight > nextRowAvgHeight * 1.2;
    }

    private static string ProcessExcel(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[SPREADSHEET_START]");

        using var document = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = document.WorkbookPart;
        var sheets = workbookPart!.Workbook.Descendants<Sheet>();
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        foreach (var sheet in sheets)
        {
            var worksheetPart = (WorksheetPart)workbookPart!.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            structuredContent.AppendLine($"[SHEET:{sheet.Name}]");

            if (!sheetData.Elements<Row>().Any())
            {
                structuredContent.AppendLine("[EMPTY_SHEET]");
                structuredContent.AppendLine($"[/SHEET:{sheet.Name}]");
                continue;
            }

            bool firstRow = true;
            int maxColumns = 0;

            foreach (var row in sheetData.Elements<Row>())
            {
                int cellCount = row.Elements<Cell>().Count();
                maxColumns = Math.Max(maxColumns, cellCount);
            }

            foreach (var row in sheetData.Elements<Row>())
            {
                var rowContent = new StringBuilder("|");
                var cells = row.Elements<Cell>().ToList();

                for (int i = 0; i < maxColumns; i++)
                {
                    string cellValue = "";
                    var cell = cells.FirstOrDefault(c => GetColumnIndex(GetColumnId(c.CellReference!)) == i);
                    
                    if (cell != null)
                    {
                        cellValue = GetCellValue(cell, sharedStringTable!);
                    }

                    rowContent.Append($" {cellValue} |");
                }

                structuredContent.AppendLine(rowContent.ToString());

                if (firstRow && maxColumns > 0)
                {
                    var separatorRow = new StringBuilder("|");
                    for (int i = 0; i < maxColumns; i++)
                    {
                        separatorRow.Append(" --- |");
                    }

                    structuredContent.AppendLine(separatorRow.ToString());
                    firstRow = false;
                }
            }

            structuredContent.AppendLine($"[/SHEET:{sheet.Name}]");
        }

        structuredContent.AppendLine("[SPREADSHEET_END]");
        return structuredContent.ToString();
    }

    private static string GetColumnId(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
            return "";

        return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
    }

    private static int GetColumnIndex(string columnId)
    {
        int index = 0;
        foreach (char c in columnId)
        {
            index = (index * 26) + (c - 'A' + 1);
        }

        return index - 1;
    }

    private static string GetCellValue(Cell cell, SharedStringTable sharedStringTable)
    {
        if (cell.CellValue == null)
            return string.Empty;

        string value = cell.CellValue.Text;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && sharedStringTable != null)
        {
            if (int.TryParse(value, out int ssid) && ssid >= 0 && ssid < sharedStringTable.Count())
            {
                return sharedStringTable.ElementAt(ssid).InnerText;
            }
        }

        return value;
    }

    private static string ProcessDocx(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[DOCUMENT_START]");

        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body;

        structuredContent.AppendLine("[METADATA_START]");
        if (!string.IsNullOrEmpty(document.PackageProperties.Title))
            structuredContent.AppendLine($"Title: {document.PackageProperties.Title}");
        if (!string.IsNullOrEmpty(document.PackageProperties.Creator))
            structuredContent.AppendLine($"Author: {document.PackageProperties.Creator}");
        if (!string.IsNullOrEmpty(document.PackageProperties.Subject))
            structuredContent.AppendLine($"Subject: {document.PackageProperties.Subject}");
        structuredContent.AppendLine("[METADATA_END]");

        foreach (var element in body?.Elements()!)
        {
            if (element is Paragraph paragraph)
            {
                string text = ExtractTextFromParagraph(paragraph);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (IsParagraphHeading(paragraph))
                {
                    structuredContent.AppendLine($"[HEADING]{text}[/HEADING]");
                }
                else
                {
                    structuredContent.AppendLine(text);
                }
            }
            else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
            {
                structuredContent.AppendLine("[TABLE_START]");
                FormatWordTableAsMarkdown(table, structuredContent);
                structuredContent.AppendLine("[TABLE_END]");
            }
        }

        structuredContent.AppendLine("[DOCUMENT_END]");
        return structuredContent.ToString();
    }

    private static string ExtractTextFromParagraph(Paragraph paragraph)
    {
        return string.Join(" ", paragraph.Descendants<Text>().Select(t => t.Text));
    }

    private static bool IsParagraphHeading(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return styleId != null && (styleId.StartsWith("Heading") || styleId.StartsWith("Title"));
    }

    private static void FormatWordTableAsMarkdown(DocumentFormat.OpenXml.Wordprocessing.Table table,
        StringBuilder output)
    {
        bool isFirstRow = true;

        foreach (var row in table.Elements<TableRow>())
        {
            StringBuilder rowBuilder = new StringBuilder("|");

            foreach (var cell in row.Elements<TableCell>())
            {
                string cellText = string.Join(" ", cell.Descendants<Text>().Select(t => t.Text));
                rowBuilder.Append($" {cellText.Trim()} |");
            }

            output.AppendLine(rowBuilder.ToString());

            if (isFirstRow)
            {
                isFirstRow = false;
                int cellCount = row.Elements<TableCell>().Count();
                StringBuilder separatorBuilder = new StringBuilder("|");

                for (int i = 0; i < cellCount; i++)
                {
                    separatorBuilder.Append(" --- |");
                }

                output.AppendLine(separatorBuilder.ToString());
            }
        }
    }

    private static string ProcessImage(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[IMAGE_DOCUMENT_START]");

        try
        {
            using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(filePath);
            using var page = engine.Process(img);

            string text = page.GetText();
            var lines = text.Split('\n');
            bool inTable = false;

            foreach (var line in lines)
            {
                bool looksLikeTableRow = line.Contains('\t') || line.Contains("   ");

                if (looksLikeTableRow && !inTable)
                {
                    structuredContent.AppendLine("[TABLE_START]");
                    inTable = true;
                }
                else if (!looksLikeTableRow && inTable)
                {
                    structuredContent.AppendLine("[TABLE_END]");
                    inTable = false;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (inTable)
                    {
                        string formattedLine = line.Trim();
                        formattedLine = Regex.Replace(formattedLine, @"\s{3,}", " | ");
                        formattedLine = formattedLine.Replace('\t', '|');
                        structuredContent.AppendLine($"|{formattedLine}|");
                    }
                    else
                    {
                        structuredContent.AppendLine(line);
                    }
                }
            }

            if (inTable)
            {
                structuredContent.AppendLine("[TABLE_END]");
            }
        }
        catch (Exception ex)
        {
            structuredContent.AppendLine($"[OCR_ERROR: {ex.Message}]");
        }

        structuredContent.AppendLine("[IMAGE_DOCUMENT_END]");
        return structuredContent.ToString();
    }

    private static string ProcessTextFile(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[TEXT_DOCUMENT_START]");

        string[] lines = File.ReadAllLines(filePath);
        bool inTable = false;

        foreach (var line in lines)
        {
            bool looksLikeTableRow = IsLikelyTableRow(line);

            if (looksLikeTableRow && !inTable)
            {
                structuredContent.AppendLine("[TABLE_START]");
                inTable = true;
            }
            else if (!looksLikeTableRow && inTable)
            {
                structuredContent.AppendLine("[TABLE_END]");
                inTable = false;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (inTable)
                {
                    string formattedLine = FormatPlainTextTableRow(line);
                    structuredContent.AppendLine(formattedLine);
                }
                else
                {
                    structuredContent.AppendLine(line);
                }
            }
        }

        if (inTable)
        {
            structuredContent.AppendLine("[TABLE_END]");
        }

        structuredContent.AppendLine("[TEXT_DOCUMENT_END]");
        return structuredContent.ToString();
    }

    private static bool IsLikelyTableRow(string line)
    {
        if (line.Count(c => c == '\t') >= 2)
            return true;

        var spaces = new List<int>();
        int currentSpace = 0;

        foreach (var t in line)
        {
            if (t == ' ')
            {
                currentSpace++;
            }
            else
            {
                if (currentSpace >= 3)
                {
                    spaces.Add(currentSpace);
                }

                currentSpace = 0;
            }
        }

        if (currentSpace >= 3)
        {
            spaces.Add(currentSpace);
        }

        return spaces.Count >= 2;
    }

    private static string FormatPlainTextTableRow(string line)
    {
        string formatted = line.Replace('\t', '|');
        formatted = Regex.Replace(formatted, @"\s{3,}", "|");

        if (!formatted.StartsWith("|"))
            formatted = "|" + formatted;

        if (!formatted.EndsWith("|"))
            formatted = formatted + "|";

        return formatted;
    }

    private static string ProcessRtf(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[RTF_DOCUMENT_START]");

        try
        {
            string rtfText = File.ReadAllText(filePath);
            string plainText = ConvertRtfToPlainText(rtfText);
            string[] lines = plainText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    structuredContent.AppendLine(line.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            structuredContent.AppendLine($"[RTF_PROCESSING_ERROR: {ex.Message}]");
        }

        structuredContent.AppendLine("[RTF_DOCUMENT_END]");
        return structuredContent.ToString();
    }

    private static string ConvertRtfToPlainText(string rtfText)
    {
        string plainText = rtfText;
        int headerEnd = plainText.IndexOf("\\viewkind4", StringComparison.Ordinal);
        
        if (headerEnd > 0)
        {
            plainText = plainText.Substring(headerEnd);
        }

        plainText = Regex.Replace(plainText, @"\\[a-zA-Z]+[0-9]*", " ");
        plainText = plainText.Replace("{", "").Replace("}", "");
        plainText = plainText.Replace("\\", "");
        plainText = Regex.Replace(plainText, @"\s+", " ");

        return plainText.Trim();
    }

    private static string ProcessHtml(string filePath)
    {
        var structuredContent = new StringBuilder();
        structuredContent.AppendLine("[HTML_DOCUMENT_START]");

        try
        {
            string htmlText = File.ReadAllText(filePath);
            string plainText = StripHtmlTags(htmlText);
            ExtractTablesFromHtml(htmlText, structuredContent);

            var lines = plainText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    structuredContent.AppendLine(line.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            structuredContent.AppendLine($"[HTML_PROCESSING_ERROR: {ex.Message}]");
        }

        structuredContent.AppendLine("[HTML_DOCUMENT_END]");
        return structuredContent.ToString();
    }

    private static string StripHtmlTags(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", " ");
    }

    private static void ExtractTablesFromHtml(string html, StringBuilder output)
    {
        var tableMatches = Regex.Matches(html, @"<table[^>]*>(.*?)</table>", RegexOptions.Singleline);

        foreach (Match tableMatch in tableMatches)
        {
            string tableHtml = tableMatch.Groups[1].Value;
            output.AppendLine("[TABLE_START]");
            var rowMatches = Regex.Matches(tableHtml, @"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
            bool isFirstRow = true;

            foreach (Match rowMatch in rowMatches)
            {
                string rowHtml = rowMatch.Groups[1].Value;
                StringBuilder rowBuilder = new StringBuilder("|");
                var cellMatches = Regex.Matches(rowHtml, @"<(td|th)[^>]*>(.*?)</(?:td|th)>", RegexOptions.Singleline);

                foreach (Match cellMatch in cellMatches)
                {
                    string cellContent = cellMatch.Groups[2].Value;
                    cellContent = Regex.Replace(cellContent, @"<[^>]+>", "");
                    rowBuilder.Append($" {cellContent.Trim()} |");
                }

                output.AppendLine(rowBuilder.ToString());

                if (isFirstRow)
                {
                    int cellCount = cellMatches.Count;
                    StringBuilder separatorBuilder = new StringBuilder("|");

                    for (int i = 0; i < cellCount; i++)
                    {
                        separatorBuilder.Append(" --- |");
                    }

                    output.AppendLine(separatorBuilder.ToString());
                    isFirstRow = false;
                }
            }

            output.AppendLine("[TABLE_END]");
        }
    }
}