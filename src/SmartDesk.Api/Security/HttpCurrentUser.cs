using System.Security.Claims;
using SmartDesk.Application.Abstractions;

namespace SmartDesk.Api.Security;

/// <summary>Obtiene el usuario actual de los claims del token JWT de la petición.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal User =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No hay una petición HTTP activa.");

    public string Id =>
        User.Identity?.Name ?? throw new InvalidOperationException("El token no incluye el nombre de usuario.");

    public bool IsAgent => User.IsInRole("agente");
}
