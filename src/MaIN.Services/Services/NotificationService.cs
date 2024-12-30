using System.Drawing;
using System.Text.Json;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class NotificationService : INotificationService
{
    public Task DispatchNotification(object message, string messageType)
    {
        var msg = JsonSerializer.Serialize(message);
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}");
        Console.ForegroundColor = originalColor;

        return Task.CompletedTask;
    }
}