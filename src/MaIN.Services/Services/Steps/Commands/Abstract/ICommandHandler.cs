namespace MaIN.Services.Services.Steps.Commands.Abstract;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}