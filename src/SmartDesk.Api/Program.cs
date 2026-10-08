using SmartDesk.Api.Endpoints;
using SmartDesk.Api.Errors;
using SmartDesk.Application;
using SmartDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SmartDesk")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'SmartDesk'.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddProblemDetails();                              // errores en formato estándar
builder.Services.AddExceptionHandler<DomainExceptionHandler>();    // DomainException -> 409
builder.Services.AddValidation();                                  // valida [Required], [MaxLength]... (.NET 10)
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "SmartDesk API en marcha");
app.MapTicketEndpoints();

app.Run();
