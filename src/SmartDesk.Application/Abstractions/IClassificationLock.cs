namespace SmartDesk.Application.Abstractions;

/// <summary>Exclusión entre instancias de la API: solo una clasifica pendientes a la vez.</summary>
public interface IClassificationLock
{
    /// <summary>Devuelve un permiso si nadie más lo tiene, o null si otra instancia está clasificando.
    /// Al liberar el permiso se termina la transacción y se suelta el bloqueo.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken ct);
}
