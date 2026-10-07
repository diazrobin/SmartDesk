namespace SmartDesk.Domain.Common;

/// <summary>Se lanza cuando se viola una regla de negocio.</summary>
public sealed class DomainException(string message) : Exception(message);
