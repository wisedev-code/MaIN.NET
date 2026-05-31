using System.Net;

namespace MaIN.Domain.Exceptions.Tools;

public class InvalidToolIterationsException(int value)
    : MaINCustomException($"MaxIterations must be at least 1, but received {value}.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;

    public static void ThrowIfInvalid(int value)
    {
        if (value < 1)
        {
            throw new InvalidToolIterationsException(value);
        }
    }
}
