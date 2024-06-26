using Polly;
using Polly.Extensions.Http;
using Monitoring;
using OpenTelemetry.Trace;
using API.Services;

var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

/*** START OF IMPORTANT CONFIGURATION ***/
var serviceName = "API";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry().Setup(serviceName, serviceVersion);
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));

/*** END OF IMPORTANT CONFIGURATION ***/

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy  =>
        {
            policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HERE we add the controllers
builder.Services.AddControllers();

// HERE we add the IHttpClientFactory
builder.Services.AddHttpClient();

// HTTP Circuit breakers

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var policies = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

builder.Services.AddHttpClient("SumServiceClient", client => {
        client.BaseAddress = new Uri("http://sum-service:80");
    })
    .AddPolicyHandler(policies);

builder.Services.AddHttpClient("SubtractServiceClient", client => {
        client.BaseAddress = new Uri("http://subtract-service:80");
    })
    .AddPolicyHandler(policies);

builder.Services.AddHttpClient("FallbackClient", client => {
        client.BaseAddress = null;
    })
    .AddPolicyHandler(policies);

// Fall-back QUEUE strategy
builder.Services.AddHostedService<FailedRequestProcessor>();
builder.Services.AddSingleton<IFailedRequestQueueFactory, FailedRequestQueueFactory>();
builder.Services.AddSingleton<InMemoryFailedRequestQueue>();
builder.Services.AddSingleton<IFailedRequestQueue>(_ => _.GetRequiredService<IFailedRequestQueueFactory>().GetQueue(serviceName));
builder.Services.AddSingleton<FailedRequestProcessor>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(MyAllowSpecificOrigins);

// HERE we map the controllers
app.MapControllers();

//app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.Run();