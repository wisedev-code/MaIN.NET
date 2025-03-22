namespace MaIN.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // Enable Swagger in all environments for demo purposes
        app.UseSwagger();
        app.UseSwaggerUI();
        
        // Configure middleware pipeline
        app.UseHttpsRedirection();
        app.UseCors("AllowFE");
        app.MapHub<NotificationHub>("/diagnostics");
        
        return app;
    }
}