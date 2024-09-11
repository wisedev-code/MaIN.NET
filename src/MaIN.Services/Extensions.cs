namespace MaIN.Services;

public static class Extensions
{
    
    public static bool IsDockerEnv()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Docker";
    }
}