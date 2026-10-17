using Microsoft.Extensions.Time.Testing;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Domain.Tests;

public class TicketTests
{
    private readonly FakeTimeProvider _clock =
        new(new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_TicketValido_QuedaAbiertoYSinPrioridad()
    {
        var ticket = Ticket.Create("No funciona el VPN", "Error 809 al conectar", "ana", _clock);

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(TicketPriority.Unassigned, ticket.Priority);
        Assert.Equal("ana", ticket.CreatedBy);
        Assert.Equal(_clock.GetUtcNow(), ticket.CreatedAt);
    }

    [Fact]
    public void Create_SinCreador_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Ticket.Create("Título", "Descripción", "  ", _clock));
    }

    [Fact]
    public void Resolve_SinEstarEnCurso_LanzaDomainException()
    {
        var ticket = Ticket.Create("Título", "Descripción", "ana", _clock);

        Assert.Throws<DomainException>(() => ticket.Resolve(_clock));
    }

    [Fact]
    public void Resolve_FlujoCompleto_RegistraFechaDeResolucion()
    {
        var ticket = Ticket.Create("Título", "Descripción", "ana", _clock);
        ticket.StartProgress();
        _clock.Advance(TimeSpan.FromHours(2));

        ticket.Resolve(_clock);

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.Equal(_clock.GetUtcNow(), ticket.ResolvedAt);
    }
}
