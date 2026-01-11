
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Steps.Commands;


public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IDictionary<string, Type> _namedHandlers = new Dictionary<string, Type>();
    
    public void RegisterNamedHandler<TCommand, TResult, THandler>(string commandName)
        where TCommand : ICommand<TResult>
        where THandler : ICommandHandler<TCommand, TResult>
    {
        _namedHandlers[commandName] = typeof(THandler);
    }
    
    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, string? commandName = null)
    {
        Type handlerType;
        commandName ??= command.CommandName;
        if (!string.IsNullOrEmpty(commandName) && _namedHandlers.TryGetValue(commandName, out var namedHandlerType))
        {
            handlerType = namedHandlerType;
        }
        else
        {
            var commandType = command.GetType();
            var handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
            
            var handler = serviceProvider.GetService(handlerInterfaceType);
            
            var resolvedHandler = handler ?? throw new InvalidOperationException($"No handler registered for command type {commandType.Name}");
            return await ((ICommandHandler<ICommand<TResult>, TResult>)resolvedHandler).HandleAsync(command);
        }

        var namedHandler = serviceProvider.GetService(handlerType);
        var resolvedNamedHandler = namedHandler ?? throw new InvalidOperationException($"No handler registered for command name {commandName}");

        var method = handlerType.GetMethod("HandleAsync");
        var resolvedMethod = method ?? throw new InvalidOperationException($"HandleAsync method not found on handler {handlerType.Name}");

        var task = (Task<TResult>)resolvedMethod.Invoke(resolvedNamedHandler, [command])!;
        return await task;
    }
}