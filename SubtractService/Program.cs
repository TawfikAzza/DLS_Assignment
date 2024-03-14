using Monitoring;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

/*** START OF IMPORTANT CONFIGURATION ***/
var serviceName = "SubtractService";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry().Setup(serviceName, serviceVersion);
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
/*** END OF IMPORTANT CONFIGURATION ***/

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddHttpClient();

// HTTP Circuit breakers

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

var policies = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

builder.Services.AddHttpClient("HistoryServiceClient", client => {
        client.BaseAddress = new Uri("http://history-service:80");
    })
    .AddPolicyHandler(policies);

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();


//app.UseHttpsRedirection();

app.Run();