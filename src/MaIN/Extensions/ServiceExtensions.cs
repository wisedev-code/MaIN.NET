using MaIN.Services;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;

namespace MaIN.Extensions;

public static class ServiceExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // API documentation
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Core services
        builder.Services.AddHttpClient();
        builder.Services.AddSignalR();
        
        // CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFE",
                policy =>
                {
                    policy.AllowAnyOrigin() 
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        builder.Services.ConfigureMaIN(builder.Configuration);
        builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();
        
        return builder;
    }
}