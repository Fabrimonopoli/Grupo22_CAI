namespace Notifications.API.Exceptions;

public static class NotificationErrorCodes
{
    public const string Ntf001 = "NTF-001";
    public const string Ntf002 = "NTF-002";
    public const string Ntf003 = "NTF-003";
    public const string Ntf004 = "NTF-004";
}

public class NotFoundException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
