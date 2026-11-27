using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tests;

public class PendingClassificationProcessorTests
{
    private readonly InMemoryTicketRepository _repo = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero));

    private Ticket NuevoPendiente(string titulo)
    {
        var ticket = Ticket.Create(titulo, "descripcion", "ana", _clock);
        _repo.Add(ticket);
        return ticket;
    }

    [Fact]
    public async Task ProcessBatch_ClasificaLosPendientes()
    {
        var ticket = NuevoPendiente("Impresora");
        var processor = new PendingClassificationProcessor(_repo, new ClasificadorOk(), NullLogger<PendingClassificationProcessor>.Instance);

        var procesados = await processor.ProcessBatchAsync(10, CancellationToken.None);

        Assert.Equal(1, procesados);
        Assert.Equal(ClassificationStatus.Classified, ticket.Classification);
        Assert.Equal("Hardware", ticket.Category);
    }

    [Fact]
    public async Task ProcessBatch_FallosRepetidos_LlevanAFailedTrasTresIntentos()
    {
        var ticket = NuevoPendiente("Impresora");
        var processor = new PendingClassificationProcessor(_repo, new ClasificadorFallido(), NullLogger<PendingClassificationProcessor>.Instance);

        await processor.ProcessBatchAsync(10, CancellationToken.None);
        await processor.ProcessBatchAsync(10, CancellationToken.None);
        Assert.Equal(ClassificationStatus.Pending, ticket.Classification);

        await processor.ProcessBatchAsync(10, CancellationToken.None);
        Assert.Equal(ClassificationStatus.Failed, ticket.Classification);
        Assert.Equal(TicketPriority.Unassigned, ticket.Priority);
    }

    [Fact]
    public async Task ProcessBatch_TicketResueltoAntesDeClasificar_SeAbandonaSinLlamarALaIA()
    {
        var ticket = NuevoPendiente("Impresora");
        ticket.StartProgress();
        ticket.Resolve(_clock);
        var clasificador = new ClasificadorOk();
        var processor = new PendingClassificationProcessor(_repo, clasificador, NullLogger<PendingClassificationProcessor>.Instance);

        await processor.ProcessBatchAsync(10, CancellationToken.None);

        Assert.Equal(ClassificationStatus.Failed, ticket.Classification);
        Assert.Equal(0, clasificador.Llamadas);
    }

    [Fact]
    public async Task ProcessBatch_YaClasificados_NoSeProcesan()
    {
        var ticket = NuevoPendiente("Impresora");
        ticket.Classify("Software", TicketPriority.Low);
        var processor = new PendingClassificationProcessor(_repo, new ClasificadorOk(), NullLogger<PendingClassificationProcessor>.Instance);

        var procesados = await processor.ProcessBatchAsync(10, CancellationToken.None);

        Assert.Equal(0, procesados);
    }

    private sealed class ClasificadorOk : ITicketClassifier
    {
        public int Llamadas { get; private set; }

        public Task<TicketClassification> ClassifyAsync(string title, string description, CancellationToken ct)
        {
            Llamadas++;
            return Task.FromResult(new TicketClassification("Hardware", TicketPriority.High));
        }
    }

    private sealed class ClasificadorFallido : ITicketClassifier
    {
        public Task<TicketClassification> ClassifyAsync(string title, string description, CancellationToken ct) =>
            throw new InvalidOperationException("IA caída");
    }
}
