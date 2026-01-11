namespace MaIN.Services.Services.Steps.Commands.Abstract;

public interface ICommand<TResult>
{
    string? CommandName { get; }
}