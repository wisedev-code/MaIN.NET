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
    
    public static LLMTokenValue ProcessExaONEToken(
        string token,
        ThinkingState state)
    {
        if (!state.Props.ContainsKey("message"))
        {
            state.Props.Add("message", string.Empty);
            state.Props.Add("previous_token", string.Empty);
        }

        var endReason = state.Props.ContainsKey("end_reason");
        var previousToken = state.Props["previous_token"];
        state.Props["previous_token"] = token;
        
        state.Props["message"] += token;
        if (!state.IsInThinkingMode && state.Props["message"].Contains("<thought>") && !endReason)
        {
            state.IsInThinkingMode = true;
            return new LLMTokenValue()
            {
                Text = string.Empty,
                Type = TokenType.Special
            };
        }

        if (state.IsInThinkingMode && state.Props["message"].Contains("</thought>") && !endReason)
        {
            state.IsInThinkingMode = false;
            state.Props.Add("end_reason", String.Empty);
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

        if (token == "\\boxed" || (previousToken == String.Empty && token == "<") || (token == "thought" && previousToken == "<"))
        {
            return new LLMTokenValue()
            {
                Text = string.Empty,
                Type = TokenType.Special
            };
        }
        return new LLMTokenValue()
        {
            Text = token,
            Type = TokenType.Message
        };
    }
}