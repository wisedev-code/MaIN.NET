using System.Text.RegularExpressions;

namespace MaIN.Domain.Models;

public static class ReasoningFunctions
{
    public static LLMTokenValue ProcessDeepSeekToken(
        string token,
        ThinkingState state)
    {
        if (token == "<think>")
        {
            state.IsInThinkingMode = true;
            return new LLMTokenValue()
            {
                Text = string.Empty,
                Type = TokenType.Special
            };
        }

        if (token == "</think>")
        {
            state.IsInThinkingMode = false;
            return new LLMTokenValue()
            {
                Type = TokenType.Special,
                Text = String.Empty
            };
        }

        if (state.IsInThinkingMode)
        {
            return new LLMTokenValue()
            {
                Type = TokenType.Reason,
                Text = token
            };
        }

        return new LLMTokenValue()
        {
            Text = token,
            Type = TokenType.Message
        };
    }
    

    public static LLMTokenValue ProcessQwQ_QwenModToken(string token, ThinkingState state)
    {
        if (!state.Props.ContainsKey("message"))
        {
            state.Props["message"] = string.Empty;
            state.Props["previous_token"] = string.Empty;
        }

        var endReasonExists = state.Props.ContainsKey("end_reason");
        var previousToken = state.Props["previous_token"];
        state.Props["previous_token"] = token;
        state.Props["message"] += token;
    
        if (!state.IsInThinkingMode && state.Props["message"].Contains("<think>") && !endReasonExists)
        {
            state.IsInThinkingMode = true;
            return new LLMTokenValue { Text = string.Empty, Type = TokenType.Special };
        }

        if (state.IsInThinkingMode && state.Props["message"].Contains("</think") && !endReasonExists)
        {
            state.IsInThinkingMode = false;
            state.Props["end_reason"] = string.Empty;
            return new LLMTokenValue { Text = string.Empty, Type = TokenType.Special };
        }
    
        if (state.IsInThinkingMode)
        {
            return new LLMTokenValue { Type = TokenType.Reason, Text = token };
        }

        string combined = previousToken + token;
        if (token == "<" || token == "</" || token == "think" && Regex.IsMatch(combined, "^(<|<think|</|</think)$"))
        {
            return new LLMTokenValue { Text = string.Empty, Type = TokenType.Special };
        }
    
        return new LLMTokenValue { Text = token, Type = TokenType.Message };
    }
}