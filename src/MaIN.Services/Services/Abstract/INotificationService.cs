namespace MaIN.Services.Services.Abstract;

public interface INotificationService
{
    Task DispatchNotification(object message);
}