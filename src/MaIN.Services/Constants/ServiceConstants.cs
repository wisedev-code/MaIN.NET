namespace MaIN.Services.Constants;

public static class ServiceConstants
{

    public static class HttpClients
    {
        public const string ImageGenClient = "ImageGenClient";
        public const string OpenAiClient = "OpenAiClient";
        public const string ImageDownloadClient = "ImageDownloadClient";
    }
    
    public static class ApiUrls
    {
        public const string OpenAiImageGenerations = "https://api.openai.com/v1/images/generations";
        public const string OpenAiChatCompletions = "https://api.openai.com/v1/chat/completions";
        public const string OpenAiModels = "https://api.openai.com/v1/models";
    }

    public static class Messages
    {
        public const string GeneratedImageContent = "Generated Image:";
        public const string AssistantRole = "Assistant";
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