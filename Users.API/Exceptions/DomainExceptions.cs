namespace Users.API.Exceptions;

public static class UserErrorCodes
{
    public const string Usr001 = "USR-001";
    public const string Usr002 = "USR-002";
    public const string Usr003 = "USR-003";
    public const string Usr004 = "USR-004";
    public const string Usr005 = "USR-005";
    public const string Usr006 = "USR-006";
}

public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
