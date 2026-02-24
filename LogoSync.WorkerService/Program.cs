// =============================================
// LogoSync.WorkerService/Program.cs
// =============================================

using LogoSync.Core.Interfaces;
using LogoSync.Infrastructure.Data;
using LogoSync.Infrastructure.JplatformApi;
using LogoSync.WorkerService;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/sync-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Logo jPlatform Sync Service...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    // Configuration
    var config = builder.Configuration;

    // =============================================
    // JGDB05 Repository (Logo Sync)
    // =============================================
    builder.Services.AddSingleton<ISyncRepository>(sp =>
        new SyncRepository(config.GetConnectionString("SqlConnection")!));

    // =============================================
    // PUNTO Repository (Import)
    // =============================================
    var puntoConnectionString = config.GetConnectionString("PuntoConnection");
    if (!string.IsNullOrEmpty(puntoConnectionString))
    {
        builder.Services.AddSingleton<IPuntoRepository>(sp =>
            new PuntoRepository(puntoConnectionString));

        // PUNTO Import Worker
        builder.Services.AddHostedService<PuntoImportWorker>();
        Log.Information("PUNTO Import Worker enabled");
    }
    else
    {
        Log.Warning("PUNTO connection string not configured. Import worker disabled.");
    }

    // =============================================
    // jPlatform Settings & Client
    // =============================================
    var jplatformSettings = new JplatformSettings
    {
        BaseUrl = config["Jplatform:BaseUrl"]!,
        Username = config["Jplatform:Username"]!,
        Password = config["Jplatform:Password"]!,
        FirmNo = config["Jplatform:FirmNo"]!,
        PeriodNo = config["Jplatform:PeriodNo"]!
    };
    builder.Services.AddSingleton(jplatformSettings);

    // API Client
    builder.Services.AddSingleton<IJplatformApiClient, JplatformApiClient>();

    // =============================================
    // Logo Sync Worker
    // =============================================
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}