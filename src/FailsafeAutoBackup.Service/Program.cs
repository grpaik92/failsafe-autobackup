using FailsafeAutoBackup.Service;
using FailsafeAutoBackup.Service.Interfaces;
using FailsafeAutoBackup.Service.Services;
using FailsafeAutoBackup.Shared.Configuration;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
            "FailsafeAutoBackup", "Logs", "service-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    
    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "FailsafeAutoBackup Service";
    });

    // Configure logging
    builder.Services.AddSerilog();

    // Register configuration
    var serviceConfig = new ServiceConfiguration();
    builder.Configuration.Bind("ServiceConfiguration", serviceConfig);
    builder.Services.AddSingleton(serviceConfig);

    // Register services
    builder.Services.AddSingleton<IHeartbeatService, HeartbeatService>();
    builder.Services.AddSingleton<IDocumentDetector, DocumentDetector>();
    builder.Services.AddSingleton<IBackupService, BackupService>();
    builder.Services.AddSingleton<WordComService>();

    // Add the main worker
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    Log.Information("Failsafe AutoBackup Service starting");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

