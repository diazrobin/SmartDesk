using SmartDesk.Api.Endpoints;
using SmartDesk.Api.Errors;
using SmartDesk.Application;
using SmartDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SmartDesk")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'SmartDesk'.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// Autenticación: valida tokens JWT (emisor, audiencia y firma se leen de la configuración)
builder.Services.AddAuthentication().AddJwtBearer();

// Autorización: política "Agente" = el token debe incluir el rol "agente"
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(TicketEndpoints.AgentPolicy, policy => policy.RequireRole("agente"));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // ¿quién eres?
app.UseAuthorization();    // ¿qué puedes hacer?

app.MapGet("/", () => "SmartDesk API en marcha");   // pública: sirve como comprobación de vida
app.MapTicketEndpoints();

app.Run();
