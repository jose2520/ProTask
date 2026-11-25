namespace ProTask.Domain.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, string errorCode = "business_rule_violation")
        : base(message, errorCode)
    {
    }
}
