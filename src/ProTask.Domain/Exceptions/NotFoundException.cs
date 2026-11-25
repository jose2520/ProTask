namespace ProTask.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"No se encontró {entityName} con el identificador '{key}'.", "resource_not_found")
    {
    }

    public NotFoundException(string message)
        : base(message, "resource_not_found")
    {
    }
}
