using System.Text.Json;
using MaIN.Domain.Models;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class NotificationService : INotificationService
{
    public Task DispatchNotification(object message, string messageType)
    {
        var originalColor = Console.ForegroundColor;
        if (messageType == "ReceiveMessageUpdate")
        {
            var msg = message as Dictionary<string,string>;
            var done = msg!["Done"] == "True";
            var type = msg["Type"];
            if (!done)
            {
                if (type == TokenType.Reason.ToString())
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.Write(msg["Content"]);
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine();
            }
        }
        else
        {
            var msg = JsonSerializer.Serialize(message);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}");
            Console.ForegroundColor = originalColor;
        }

        return Task.CompletedTask;
    }
}