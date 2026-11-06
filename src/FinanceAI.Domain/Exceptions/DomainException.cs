namespace FinanceAI.Domain.Exceptions;

/// <summary>Base for business-rule errors; the API maps each subtype to an HTTP status.</summary>
public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string entity, object key)
    : DomainException($"{entity} '{key}' não encontrado(a).");

public sealed class DuplicatedException(string message) : DomainException(message);

public sealed class ValidationException(string message) : DomainException(message);
