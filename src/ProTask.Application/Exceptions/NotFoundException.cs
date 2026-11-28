namespace ProTask.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public string ErrorCode { get; } = "resource_not_found";

    public NotFoundException(string entityName, object key)
        : base($"No se encontró {entityName} con el identificador '{key}'.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
