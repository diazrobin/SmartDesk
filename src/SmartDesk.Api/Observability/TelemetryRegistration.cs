using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SmartDesk.Api.Observability;

/// <summary>Configura las trazas de la API. El destino se elige con OTEL_EXPORTER_OTLP_ENDPOINT.</summary>
public static class TelemetryRegistration
{
    public const string ServiceName = "SmartDesk.Api";

    public static WebApplicationBuilder AddSmartDeskTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()             // una traza por petición HTTP
                .AddHttpClientInstrumentation()             // llamadas salientes (Gemini)
                .AddEntityFrameworkCoreInstrumentation()    // consultas SQL de EF Core
                .AddOtlpExporter());                        // envío por OTLP

        return builder;
    }
}
