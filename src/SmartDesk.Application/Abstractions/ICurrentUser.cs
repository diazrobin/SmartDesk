namespace SmartDesk.Application.Abstractions;

/// <summary>Usuario que realiza la operación actual. La capa Api lo obtiene del token JWT.</summary>
public interface ICurrentUser
{
    string Id { get; }
    bool IsAgent { get; }
}
