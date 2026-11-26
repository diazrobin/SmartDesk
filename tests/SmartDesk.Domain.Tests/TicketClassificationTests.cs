using SmartDesk.Domain.Common;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Domain.Tests;

public class TicketClassificationTests
{
    private static Ticket NuevoTicket() =>
        Ticket.Create("Impresora", "No imprime desde ayer", "ana", TimeProvider.System);

    [Fact]
    public void Create_DejaLaClasificacionPendiente()
    {
        var ticket = NuevoTicket();

        Assert.Equal(ClassificationStatus.Pending, ticket.Classification);
        Assert.Equal(TicketPriority.Unassigned, ticket.Priority);
    }

    [Fact]
    public void Classify_PasaAClasificado()
    {
        var ticket = NuevoTicket();

        ticket.Classify("Hardware", TicketPriority.High);

        Assert.Equal(ClassificationStatus.Classified, ticket.Classification);
        Assert.Equal("Hardware", ticket.Category);
    }

    [Fact]
    public void FallosRepetidos_LlevanAFailedTrasElLimite()
    {
        var ticket = NuevoTicket();

        ticket.RecordClassificationFailure(3);
        ticket.RecordClassificationFailure(3);
        Assert.Equal(ClassificationStatus.Pending, ticket.Classification);

        ticket.RecordClassificationFailure(3);
        Assert.Equal(ClassificationStatus.Failed, ticket.Classification);
        Assert.Equal(3, ticket.ClassificationAttempts);
    }

    [Fact]
    public void RecordFailure_SinLimiteValido_Lanza()
    {
        var ticket = NuevoTicket();

        Assert.Throws<ArgumentOutOfRangeException>(() => ticket.RecordClassificationFailure(0));
    }

    [Fact]
    public void RecordFailure_SobreUnTicketYaClasificado_Lanza()
    {
        var ticket = NuevoTicket();
        ticket.Classify("Software", TicketPriority.Low);

        Assert.Throws<DomainException>(() => ticket.RecordClassificationFailure(3));
    }
}
