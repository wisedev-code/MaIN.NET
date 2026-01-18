namespace MaIN.Services.Services.Steps.Commands.Abstract;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TResult>(
        ICommand<TResult> command, 
        string? commandName = null);
}