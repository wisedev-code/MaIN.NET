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
            Type = TokenType.Answer
        };
    }
}