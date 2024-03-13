using HistoryService;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

/*** START OF IMPORTANT CONFIGURATION ***/
var serviceName = "MyTracer";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry().Setup();
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
/*** END OF IMPORTANT CONFIGURATION ***/

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddDbContext<CalcContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("YourConnectionStringName")));
var app = builder.Build();


// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();


app.MapControllers();
//app.UseHttpsRedirection();

app.Run();
