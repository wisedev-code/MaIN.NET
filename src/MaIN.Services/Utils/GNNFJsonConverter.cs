using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MaIN.Services.Utils;

public class GBNFToJsonConverter
    {
        private Dictionary<string, string> rules;
        private HashSet<string> visitedRules;
        private int maxDepth;
        private int currentDepth;

        public GBNFToJsonConverter(int maxDepth = 10)
        {
            rules = new Dictionary<string, string>();
            visitedRules = new HashSet<string>();
            this.maxDepth = maxDepth;
            currentDepth = 0;
        }

        /// <summary>
        /// Parse GBNF grammar and convert to JSON schema example
        /// </summary>
        /// <param name="gbnfText">GBNF grammar text</param>
        /// <param name="startRule">Starting rule name (default: "root")</param>
        /// <returns>JSON schema example string</returns>
        public string ConvertToJson(string gbnfText, string startRule = "root")
        {
            ParseGrammar(gbnfText);
            visitedRules.Clear();
            currentDepth = 0;
            
            if (!rules.ContainsKey(startRule))
                throw new ArgumentException($"Start rule '{startRule}' not found in grammar");

            var result = ConvertFromRule(startRule);
            result = CleanupResult(result);
            
            // Try to parse and format as JSON
            try
            {
                var jsonDoc = JsonDocument.Parse(result);
                return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        private string CleanupResult(string result)
        {
            // Fix escaped quotes in JSON keys
            result = result.Replace("\\\"", "\"");
            
            // Clean up whitespace
            result = Regex.Replace(result, @"\s+", " ");
            
            return result.Trim();
        }

        private void ParseGrammar(string gbnfText)
        {
            rules.Clear();
            var lines = gbnfText.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.Contains("::="))
                {
                    var parts = trimmed.Split(new[] { "::=" }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var ruleName = parts[0].Trim();
                        var ruleBody = parts[1].Trim();
                        rules[ruleName] = ruleBody;
                    }
                }
            }
        }

        private string ConvertFromRule(string ruleName)
        {
            if (currentDepth > maxDepth)
                return "\"...\"";

            // Handle whitespace rules - ignore them
            if (IsWhitespaceRule(ruleName))
                return "";

            if (!rules.ContainsKey(ruleName))
            {
                return InferTypeFromRuleName(ruleName);
            }

            currentDepth++;
            var ruleBody = rules[ruleName];
            
            // Only special handling for array patterns (knowledge grammar)
            if (IsArrayPattern(ruleBody))
            {
                var result = ConvertArrayPattern(ruleBody);
                currentDepth--;
                return result;
            }
            
            // Use normal expression conversion for everything else (including decision grammar)
            var normalResult = ConvertFromExpression(ruleBody);
            currentDepth--;
            
            return normalResult;
        }

        private bool IsArrayPattern(string expression)
        {
            // Look for patterns like: "[" ... "]" with optional comma-separated items
            return expression.Contains("\"[\"") && expression.Contains("\"]\"") && 
                   (expression.Contains("\",\"") || expression.Contains("(") && expression.Contains(")*"));
        }

        private bool IsObjectPattern(string expression)
        {
            // Look for patterns like: "{" ... "}" with key-value pairs
            return expression.Contains("\"{\"") && expression.Contains("\"}\"") && 
                   (expression.Contains("\":\"") || expression.Contains("\":"));
        }

        private string ConvertArrayPattern(string expression)
        {
            // Extract the item type from array pattern
            // For pattern like: "[" ws (string (ws "," ws string)*)? ws "]"
            // We want to find what 'string' represents
            
            var tokens = TokenizeExpression(expression);
            string itemType = "\"string\""; // default
            
            // Find the main item rule reference (usually appears twice - once alone, once in repetition)
            var ruleReferences = tokens.Where(t => !t.StartsWith("\"") && !t.StartsWith("[") && 
                                                  !IsWhitespaceRule(t) && !t.Contains("(") && !t.Contains(")") &&
                                                  !t.EndsWith("*") && !t.EndsWith("+") && !t.EndsWith("?"))
                                   .GroupBy(t => t)
                                   .Where(g => g.Count() > 1 || !IsStructuralToken(g.Key))
                                   .Select(g => g.Key)
                                   .FirstOrDefault();

            if (!string.IsNullOrEmpty(ruleReferences))
            {
                itemType = ConvertFromRule(ruleReferences);
            }

            return $"[{itemType}]";
        }

        private string ConvertObjectPattern(string expression)
        {
            // For object patterns, we need to extract key-value pairs
            var result = new StringBuilder("{");
            var tokens = TokenizeExpression(expression);
            
            bool foundPairs = false;
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                // Look for pattern: "key" ":" or "key": followed by a rule
                if (tokens[i].StartsWith("\"") && tokens[i].EndsWith("\"") && tokens[i].Length > 2)
                {
                    var content = tokens[i].Substring(1, tokens[i].Length - 2);
                    
                    // Check if this looks like a JSON key (alphabetic)
                    if (Regex.IsMatch(content, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                    {
                        // Look for the value after this key
                        string valueType = "\"string\""; // default
                        
                        // Check next few tokens for the value rule
                        for (int j = i + 1; j < Math.Min(i + 4, tokens.Count); j++)
                        {
                            var token = tokens[j];
                            if (!token.StartsWith("\"") && !IsWhitespaceRule(token) && 
                                !token.Contains(":") && !token.Contains(",") && !token.Contains("}"))
                            {
                                valueType = ConvertFromRule(token);
                                break;
                            }
                        }
                        
                        if (foundPairs) result.Append(",");
                        result.Append($"\"{content}\":{valueType}");
                        foundPairs = true;
                    }
                }
            }
            
            result.Append("}");
            return result.ToString();
        }

        private bool IsStructuralToken(string token)
        {
            return token == "\"[\"" || token == "\"]\"" || token == "\"{\"" || token == "\"}\"" || 
                   token == "\",\"" || token == "\":\"" || token == "(" || token == ")" ||
                   token.EndsWith("*") || token.EndsWith("+") || token.EndsWith("?");
        }

        private bool IsWhitespaceRule(string ruleName)
        {
            var lower = ruleName.ToLower();
            return lower == "ws" || lower == "whitespace" || lower == "space" || lower == "_";
        }

        private string InferTypeFromRuleName(string ruleName)
        {
            var lower = ruleName.ToLower();
            
            if (lower.Contains("bool") || lower == "boolean")
                return "\"boolean\"";
                
            if (lower.Contains("string") || lower.Contains("name") || lower.Contains("text") || 
                lower.Contains("title") || lower.Contains("description") || lower.Contains("email") ||
                lower.Contains("url") || lower.Contains("decision"))
                return "\"string\"";
                
            if (lower.Contains("float") || lower.Contains("decimal") || lower.Contains("certainty") || 
                lower.Contains("score") || lower.Contains("rate") || lower.Contains("percentage"))
                return "\"float\"";
                
            if (lower.Contains("number") || lower.Contains("int") || lower.Contains("age") || 
                lower.Contains("count") || lower.Contains("size") || lower.Contains("length") ||
                lower.Contains("value"))
                return "\"integer\"";
                
            if (lower.Contains("array") || lower.Contains("list"))
                return "[\"string\"]";
                
            if (lower.Contains("object"))
                return "{}";
                
            return "\"string\"";
        }

        private string ConvertFromExpression(string expression)
        {
            expression = expression.Trim();
            
            // Handle alternatives (|) - use smart selection
            if (ContainsTopLevelOperator(expression, '|'))
            {
                var alternatives = SplitByTopLevelOperator(expression, '|');
                return SelectBestAlternative(alternatives);
            }
            
            // Handle sequences (space separated)
            var tokens = TokenizeExpression(expression);
            if (tokens.Count == 1)
            {
                return ConvertFromToken(tokens[0]);
            }
            else if (tokens.Count > 1)
            {
                var sb = new StringBuilder();
                foreach (var token in tokens)
                {
                    var converted = ConvertFromToken(token);
                    if (!string.IsNullOrEmpty(converted))
                    {
                        sb.Append(converted);
                    }
                }
                return sb.ToString();
            }
            
            return "";
        }

        private string SelectBestAlternative(List<string> alternatives)
        {
            // For boolean alternatives, return "boolean"
            var booleanOptions = alternatives.Where(a => a.Trim() == "\"true\"" || a.Trim() == "\"false\"").ToList();
            if (booleanOptions.Any())
            {
                return "\"boolean\"";
            }

            // Check if ANY alternative suggests float (this handles complex patterns like certainty)
            bool hasFloatPattern = alternatives.Any(alt => IsFloatPattern(alt.Trim()) || 
                                                          alt.Contains(".") || 
                                                          alt.Contains("0.") ||
                                                          alt.Contains("1.0") ||
                                                          alt.Contains("1.00"));
            
            // For numeric alternatives, determine if it's int or float
            foreach (var alt in alternatives)
            {
                var trimmed = alt.Trim();
                if (IsNumericPattern(trimmed))
                {
                    return hasFloatPattern ? "\"float\"" : "\"integer\"";
                }
            }

            // For string alternatives, return "string"
            foreach (var alt in alternatives)
            {
                var trimmed = alt.Trim();
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    return "\"string\"";
                }
            }

            // Fallback
            return "\"string\"";
        }

        private bool IsNumericPattern(string pattern)
        {
            // Remove quotes if present
            pattern = pattern.Trim('"');
            
            // Check if it's a simple number
            if (Regex.IsMatch(pattern, @"^\d+(\.\d+)?$"))
                return true;
                
            // Check if it contains numeric character classes
            return pattern.Contains("[0-9]") || Regex.IsMatch(pattern, @"\[\d-\d\]");
        }

        private bool IsFloatPattern(string pattern)
        {
            // Remove quotes if present
            pattern = pattern.Trim('"');
            
            // Check if pattern contains decimal points or float indicators
            if (pattern.Contains(".") || pattern.Contains("0.") || pattern.Contains("1.0") || pattern.Contains("1.00"))
                return true;
                
            // Check for patterns like [0-9] after a decimal point
            if (Regex.IsMatch(pattern, @"\.\s*\[0-9\]"))
                return true;
                
            return false;
        }

        private string ConvertNumericPattern(string pattern)
        {
            pattern = pattern.Trim('"');
            
            // Return type name instead of actual value
            if (IsFloatPattern(pattern))
                return "\"float\"";
                
            return "\"integer\"";
        }

        private string ConvertFromToken(string token)
        {
            token = token.Trim();
            
            if (string.IsNullOrEmpty(token))
                return "";

            // Handle whitespace rules
            if (IsWhitespaceRule(token))
                return "";
            
            // String literals - handle JSON structure properly
            if (token.StartsWith("\"") && token.EndsWith("\"") && token.Length >= 2)
            {
                var content = token.Substring(1, token.Length - 2);
                
                // If the content is a JSON structural element, return as-is (no quotes)
                if (content == "{" || content == "}" || content == "[" || content == "]" || 
                    content == "," || content == ":")
                {
                    return content;
                }
                
                // If it's a boolean literal, return type name
                if (content == "true" || content == "false")
                {
                    return "\"boolean\"";
                }
                
                // If it's a number literal, return type name
                if (Regex.IsMatch(content, @"^\d+(\.\d+)?$"))
                {
                    return content.Contains(".") ? "\"float\"" : "\"integer\"";
                }
                
                // For JSON keys (property names like "decision", "certainty"), return properly quoted
                if (Regex.IsMatch(content, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    return $"\"{content}\"";
                }
                
                // For other string content, return as quoted string
                return $"\"{content}\"";
            }
            
            // Character classes
            if (token.StartsWith("[") && token.EndsWith("]"))
            {
                return ConvertFromCharClass(token);
            }
            
            // Repetition operators - for schema, just show the base type
            if (token.EndsWith("+") || token.EndsWith("*"))
            {
                var baseToken = token.Substring(0, token.Length - 1);
                return ConvertFromToken(baseToken);
            }
            
            if (token.EndsWith("?"))
            {
                var baseToken = token.Substring(0, token.Length - 1);
                return ConvertFromToken(baseToken);
            }
            
            // Parentheses grouping
            if (token.StartsWith("(") && token.EndsWith(")"))
            {
                var inner = token.Substring(1, token.Length - 2);
                return ConvertFromExpression(inner);
            }
            
            // Rule reference
            return ConvertFromRule(token);
        }

        private string ConvertFromCharClass(string charClass)
        {
            var pattern = charClass.Substring(1, charClass.Length - 2);
            
            // Handle negation [^...]
            if (pattern.StartsWith("^"))
            {
                return "\"string\"";
            }
            
            // Handle whitespace patterns
            if (pattern.Contains("\\t") || pattern.Contains("\\n") || pattern.Contains("\\r") || pattern.Contains(" "))
            {
                return ""; // Ignore whitespace
            }
            
            // Handle digit patterns
            if (pattern.Contains("0-9") || Regex.IsMatch(pattern, @"^\d-\d$"))
            {
                return "\"integer\"";
            }
            
            // Handle letter patterns
            if (pattern.Contains("a-z") || pattern.Contains("A-Z") || pattern.Contains("a-zA-Z"))
            {
                return "\"string\"";
            }
            
            return "\"string\"";
        }

        private List<string> TokenizeExpression(string expression)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            var depth = 0;
            var inQuotes = false;
            var inBrackets = false;

            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];

                if (c == '"' && (i == 0 || expression[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (inQuotes)
                {
                    current.Append(c);
                }
                else if (c == '[')
                {
                    inBrackets = true;
                    current.Append(c);
                }
                else if (c == ']')
                {
                    inBrackets = false;
                    current.Append(c);
                }
                else if (inBrackets)
                {
                    current.Append(c);
                }
                else if (c == '(')
                {
                    depth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    depth--;
                    current.Append(c);
                }
                else if (c == ' ' && depth == 0)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        }

        private bool ContainsTopLevelOperator(string expression, char op)
        {
            var depth = 0;
            var inQuotes = false;
            var inBrackets = false;

            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];
                
                if (c == '"' && (i == 0 || expression[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == '[') inBrackets = true;
                    else if (c == ']') inBrackets = false;
                    else if (!inBrackets)
                    {
                        if (c == '(') depth++;
                        else if (c == ')') depth--;
                        else if (c == op && depth == 0) return true;
                    }
                }
            }
            return false;
        }

        private List<string> SplitByTopLevelOperator(string expression, char op)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            var depth = 0;
            var inQuotes = false;
            var inBrackets = false;

            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];
                
                if (c == '"' && (i == 0 || expression[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == '[') inBrackets = true;
                    else if (c == ']') inBrackets = false;
                    else if (!inBrackets)
                    {
                        if (c == '(') depth++;
                        else if (c == ')') depth--;
                        else if (c == op && depth == 0)
                        {
                            parts.Add(current.ToString());
                            current.Clear();
                            continue;
                        }
                    }
                }
                current.Append(c);
            }

            parts.Add(current.ToString());
            return parts;
        }
    }