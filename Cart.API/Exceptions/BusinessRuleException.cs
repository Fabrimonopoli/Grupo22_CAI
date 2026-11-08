namespace Cart.API.Exceptions;

public static class CartErrorCodes
{
    public const string Crt001 = "CRT-001";
    public const string Crt002 = "CRT-002";
    public const string Crt003 = "CRT-003";
    public const string Crt004 = "CRT-004";
    public const string Crt005 = "CRT-005";
}

public class BusinessRuleException(string code, string message) : Exception(message)
{
    public string ErrorCode => code;
    public string Code => code;
}

public class NotFoundException(string code, string message) : Exception(message)
{
    public string ErrorCode => code;
    public string Code => code;
}