namespace Orders.API.Exceptions;

public static class OrderErrorCodes
{
    public const string Ord001 = "ORD-001";
    public const string Ord002 = "ORD-002";
    public const string Ord003 = "ORD-003";
    public const string Ord004 = "ORD-004";
    public const string Ord005 = "ORD-005";
    public const string Ord006 = "ORD-006";
    public const string Ord007 = "ORD-007";
}

public class NotFoundException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
