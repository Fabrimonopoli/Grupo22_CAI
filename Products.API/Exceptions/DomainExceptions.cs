namespace Products.API.Exceptions;

public static class ProductErrorCodes
{
    public const string Prd001 = "PRD-001";
    public const string Prd002 = "PRD-002";
    public const string Prd003 = "PRD-003";
    public const string Prd004 = "PRD-004";
    public const string Prd005 = "PRD-005";
}

public class NotFoundException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
