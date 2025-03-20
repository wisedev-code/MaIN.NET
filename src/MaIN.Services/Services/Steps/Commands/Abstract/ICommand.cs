namespace MaIN.Services.Services.Steps.Commands;

public interface ICommand<TResult>
{
    string? CommandName { get; }
}