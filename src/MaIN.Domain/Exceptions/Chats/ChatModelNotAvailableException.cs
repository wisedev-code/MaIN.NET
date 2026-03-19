using System.Net;

namespace MaIN.Domain.Exceptions.Chats;

public class ChatModelNotAvailableException(string chatId, string modelId)
    : MaINCustomException($"Model '{modelId}' used by chat '{chatId}' is not registered. If this is a dynamically registered model, it must be re-registered after application restart.")
{
    public override string PublicErrorMessage => $"Model '{modelId}' is not available.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.UnprocessableEntity;
}
