using Microsoft.AspNetCore.Diagnostics;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Errors;

/// <summary>Convierte las excepciones de negocio en respuestas Problem Details (RFC 9457).</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            DomainException   => (StatusCodes.Status409Conflict, "Regla de negocio incumplida"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Datos no validos"),
            _                 => (0, string.Empty)
        };

        if (status == 0) return false; // no es nuestra: que la gestione el manejador por defecto (500)

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message }
        });
    }
}
