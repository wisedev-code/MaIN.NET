using System.Net;
using System.Text.RegularExpressions;

namespace MaIN.Domain.Exceptions;

public abstract class MaINCustomException(string message) : Exception(message)
{
    public string ErrorCode => GenerateErrorCode();
    public string LogMessage { get; private set; } = message;
    public abstract string PublicErrorMessage { get; }
    public abstract HttpStatusCode HttpStatusCode { get; }
    
    private string GenerateErrorCode()
    {
        var typeName = GetType().Name;
        var snakeCaseTypeName = Regex.Replace(typeName, "(?<!^)([A-Z])", "_$1").ToLower();
        
        return snakeCaseTypeName.Replace("_exception", string.Empty);
    }

}