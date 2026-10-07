using SmartDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SmartDesk")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'SmartDesk'.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "SmartDesk API en marcha");

app.Run();
