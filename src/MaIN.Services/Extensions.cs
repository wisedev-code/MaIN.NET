namespace MaIN.Services;

public static class Extensions
{
    
    public static bool IsDockerEnv()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Docker";
    }

    public static void AddProperty(this Dictionary<string, string> dict, string propertyName, string? value = null)
    {
        dict.Add(propertyName, value ?? string.Empty);
    }

    public static bool CheckProperty(this Dictionary<string, string> dict, string propertyName)
    {
        return dict.ContainsKey(propertyName);
    }
}