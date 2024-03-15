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
    public static ILogger Log => Serilog.Log.Logger;
    
    static Monitoring()
    {
        //Serilog
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Seq("http://seq:5341")
            .Enrich.WithSpan()
            .CreateLogger();
    }
    
    public static OpenTelemetryBuilder Setup(this OpenTelemetryBuilder builder, string serviceName, string serviceVersion)
    {
        return builder.WithTracing(tcb =>
        {
            tcb
                .AddSource(serviceName)
                .AddZipkinExporter(c =>
                {
                    c.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                })
                .AddConsoleExporter()
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter();
        });
    }
}