using System.Text.RegularExpressions;

namespace MainFE;

public static class ExtensionMethods
{
    public static string GetWorkingEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == null)
        {
            throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT environment variable is not set");
        }

        return environment;
    }
    
    public static string GetApiUrl()
    {
        return Environment.GetEnvironmentVariable("API_URL") ?? throw new InvalidOperationException("API_URL environment variable is not set");
    }
    
    public static string GetDemoApiUrl()
    {
        return Environment.GetEnvironmentVariable("DEMO_API_URL") ?? throw new InvalidOperationException("DEMO_API_URL environment variable is not set");
    }
    
    public static bool IsVisionModel(string? modelName)
    {
        if(modelName == null)
        {
            return false;
        }
        
        string[] knownVisionModels = ["llava"];
        return knownVisionModels.Any(modelName.Contains);
    }
    
    public static bool IsImageModel(string? modelName)
    {
        if(modelName == null)
        {
            return false;
        }
        
        string[] knownImageModels = ["FLUX"];
        return knownImageModels.Any(modelName.Contains);
    }

}