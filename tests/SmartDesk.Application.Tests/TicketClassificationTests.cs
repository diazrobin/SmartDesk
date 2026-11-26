using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Tests;

public class TicketClassificationTests
{
    [Fact]
    public async Task CreateAsync_DejaElTicketPendienteDeClasificar()
    {
        var service = new TicketService(
            new InMemoryTicketRepository(),
            new FakeCurrentUser("ana", IsAgent: false),
            new FakeTimeProvider());

        var dto = await service.CreateAsync("Pantalla", "No enciende", CancellationToken.None);

        // La clasificación ocurre después, en PendingClassificationProcessor (worker)
        Assert.Equal("Pending", dto.Classification);
        Assert.Null(dto.Category);
        Assert.Equal("Unassigned", dto.Priority);
    }
}
