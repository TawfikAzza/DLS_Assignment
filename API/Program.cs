using Polly;
using Polly.Extensions.Http;
using Monitoring;
using OpenTelemetry.Trace;


var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

/*** START OF IMPORTANT CONFIGURATION ***/
var serviceName = "MyTracer";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry().Setup();
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

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HERE we add the controllers
builder.Services.AddControllers();

// HERE we add the IHttpClientFactory
builder.Services.AddHttpClient();

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

// HTTP Circuit breakers

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

var policies = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

builder.Services.AddHttpClient("SumServiceClient", client => {
        client.BaseAddress = new Uri("http://sum-service:80");
    })
    .AddPolicyHandler(policies);

builder.Services.AddHttpClient("SubtractServiceClient", client => {
        client.BaseAddress = new Uri("http://subtract-service:80");
    })
    .AddPolicyHandler(policies);


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

app.Run();