using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartDesk.Application.Abstractions;

namespace SmartDesk.Infrastructure.Persistence;

/// <summary>Bloqueo consultivo de PostgreSQL ligado a una transacción.
/// Como el procesador usa el mismo DbContext, sus SaveChanges caen dentro de esa transacción.</summary>
internal sealed class PostgresClassificationLock(SmartDeskDbContext db) : IClassificationLock
{
    // Clave fija del bloqueo. No la cambies en producción sin coordinar todas las instancias.
    private const long LockKey = 7_201_001;

    public async Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(ct);

        var obtenido = await db.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({LockKey})")
            .SingleAsync(ct);

        if (obtenido) return new Lease(tx);

        await tx.DisposeAsync();
        return null;
    }

    private sealed class Lease(IDbContextTransaction tx) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            // El commit libera el bloqueo de transacción; el procesador ya habrá hecho SaveChanges
            await tx.CommitAsync();
            await tx.DisposeAsync();
        }
    }
}
