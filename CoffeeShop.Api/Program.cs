using CoffeeShop.Api.Configuration;
using CoffeeShop.Api.Database;
using CoffeeShop.Api.Entities;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CoffeeShopDbContext>(options => options.UseInMemoryDatabase("CoffeeShop"));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        metrics.AddMeter(DiagnosticsConfig.Meter.Name);

        metrics.AddOtlpExporter();
        //metrics.AddOtlpExporter(options => options.Endpoint = new Uri("http://coffeeshop.dashboard:18888"));
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        tracing.AddOtlpExporter();
    });

builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("coffee", (CoffeeType coffeeType, CoffeeShopDbContext dbContext, ILogger<Program> logger) =>
{
    if (!Enum.IsDefined(coffeeType))
    {
        logger.LogWarning("Invalid CoffeType {CoffeType}", coffeeType);

        return Results.BadRequest();
    }

    var entry = dbContext.Sales.Add(new Sale
    {
        CoffeeType = coffeeType,
        CreatedOnUtc = DateTime.UtcNow
    });
    dbContext.SaveChanges();

    DiagnosticsConfig.SalesCounter.Add(
        1,
        new KeyValuePair<string, object?>("sales.coffee.type", coffeeType),
        new KeyValuePair<string, object?>("sales.id", entry.Entity.Id),
        new KeyValuePair<string, object?>("sales.date", entry.Entity.CreatedOnUtc.Date.ToShortTimeString())
    );

    logger.LogInformation("Successfully created {@Sale}", entry.Entity);

    return Results.Ok(entry.Entity.Id);
});

app.UseHttpsRedirection();

app.Run();
