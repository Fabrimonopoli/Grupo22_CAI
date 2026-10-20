namespace Cart.API.Exceptions;

public class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
