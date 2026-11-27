using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Integration.Tests;

[Collection("Postgres")]
public class ClassificationLockTests(PostgresFixture pg)
{
    [Fact]
    public async Task SegundoPermisoMientrasEsteEnUso_NoSeConcede()
    {
        await using var db1 = pg.CreateDb();
        await using var db2 = pg.CreateDb();
        var lock1 = new PostgresClassificationLock(db1);
        var lock2 = new PostgresClassificationLock(db2);

        var permiso1 = await lock1.TryAcquireAsync(CancellationToken.None);
        Assert.NotNull(permiso1);

        Assert.Null(await lock2.TryAcquireAsync(CancellationToken.None));

        await permiso1.DisposeAsync();

        var permiso2 = await lock2.TryAcquireAsync(CancellationToken.None);
        Assert.NotNull(permiso2);
        await permiso2.DisposeAsync();
    }
}
