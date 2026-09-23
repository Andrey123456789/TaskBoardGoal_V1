using System.Text.Json.Serialization;
using Serilog;
using TaskBoard.Api.ErrorHandling;
using TaskBoard.Api.Extensions;
using TaskBoard.Application;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(
    (services, logger) => logger
        .ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
        .ReadFrom.Services(services)
        .Enrich.FromLogContext(),
    preserveStaticLogger: true);

builder.Services
    .AddControllers(options =>
    {
        // Request validation is owned by the Application validators; MVC only rejects
        // requests it cannot bind.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.AllowInputFormatterExceptionMessages = false;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PersistenceConflictExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();
builder.Services.AddFrontendCors();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Log through this host's logger instead of the process-wide static Log.Logger.
    options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsExtensions.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDataAsync();
}

await app.RunAsync();

public partial class Program;
