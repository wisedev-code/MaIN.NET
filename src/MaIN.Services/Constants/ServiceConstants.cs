namespace MaIN.Services.Constants;

public static class ServiceConstants
{

    public static class HttpClients
    {
        public const string ImageGenClient = "ImageGenClient";
        public const string OpenAiClient = "OpenAiClient";
        public const string GeminiClient = "GeminiClient";
        public const string DeepSeekClient = "DeepSeekClient";
        public const string ClaudeClient = "ClaudeClient";
        public const string ImageDownloadClient = "ImageDownloadClient";
        public const string ModelContextDownloadClient = "ModelContextDownloadClient";
    }
    
    public static class ApiUrls
    {
        public const string OpenAiImageGenerations = "https://api.openai.com/v1/images/generations";
        public const string OpenAiChatCompletions = "https://api.openai.com/v1/chat/completions";
        public const string OpenAiModels = "https://api.openai.com/v1/models";

        public const string GeminiImageGenerations = "https://generativelanguage.googleapis.com/v1beta/openai/images/generations";
        public const string GeminiOpenAiChatCompletions = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions"; 
        public const string GeminiModels = "https://generativelanguage.googleapis.com/v1beta/models";

        public const string DeepSeekOpenAiChatCompletions = "https://api.deepseek.com/v1/chat/completions";
        public const string DeepSeekModels = "https://api.deepseek.com/models";

        public const string ClaudeChatMessages = "https://api.anthropic.com/v1/messages";
        public const string ClaudeModels = "https://api.anthropic.com/v1/models";
    }

    public static class Messages
    {
        public const string GeneratedImageContent = "Generated Image:";
        public const string UnprocessedMessage = "Unprocessed";
    }

    public static class Properties
    {
        public const string PreProcessProperty = "Pre_Process";
        public const string DisableCacheProperty = "DisableCache";
    }
    
    public static class Defaults
    {
        public const string ImageSize = "1024x1024";
        public const int HttpImageModelTimeoutInMinutes = 5;
    }
    
    public static class Notifications
    {
        public const string ReceiveMessageUpdate = "ReceiveMessageUpdate";
    }

    public static class Roles
    {
        public const string Assistant = "assistant";
        public const string User = "user";
        public const string System = "system";
    }

}