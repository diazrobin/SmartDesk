using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using SmartDesk.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartDesk.Integration.Tests;

/// <summary>Levanta PostgreSQL con pgvector una vez para todos los tests y aplica las migraciones reales.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Prueba real: las migraciones deben partir de una base vacía
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }

    public SmartDeskDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SmartDeskDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.UseVector())
            .Options;
        return new SmartDeskDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>Los tests de esta colección comparten base de datos, así que se ejecutan en serie.</summary>
[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
