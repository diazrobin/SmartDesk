using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartDesk.Api.Endpoints;
using SmartDesk.Api.Errors;
using SmartDesk.Api.Security;
using SmartDesk.Application;
using SmartDesk.Application.Abstractions;
using SmartDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SmartDesk")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'SmartDesk'.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration.GetSection("Ai"));
builder.Services.AddAnswerCache(builder.Configuration.GetConnectionString("Redis"));

// Health checks: readiness depende solo de la base de datos
builder.Services.AddHealthChecks().AddSmartDeskChecks();

// Claves de DataProtection persistentes: solo si se configura una ruta (p. ej. en Docker Compose)
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));

// Usuario actual: Application solo ve la interfaz; aquí se conecta con el token JWT
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

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

app.MapGet("/", () => "SmartDesk API en marcha");
app.MapTicketEndpoints();
app.MapKnowledgeEndpoints();

// Health checks sin autenticación, para Docker y balanceadores
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.Run();
