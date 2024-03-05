using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

namespace Monitoring;

public static class Monitoring
{
    public static readonly ActivitySource ActivitySource = new("CALC_" + Assembly.GetEntryAssembly()?.GetName().Name , "1.0.0");
    private static TracerProvider _tracerProvider;

    static Monitoring()
    {
        // Configure tracing
        var serviceName = Assembly.GetExecutingAssembly().GetName().Name;
        var version = "1.0.0";

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddZipkinExporter()
            .AddConsoleExporter()
            .AddSource(ActivitySource.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: ActivitySource.Name))
            .Build();
        
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithSpan()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.Console()
            .CreateLogger();
    }
}