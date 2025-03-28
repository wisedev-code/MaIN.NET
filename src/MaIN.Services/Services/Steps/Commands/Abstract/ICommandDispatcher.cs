namespace MaIN.Services.Services.Steps.Commands;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(
        ICommand<TResult> command, 
        string? commandName = null);
}