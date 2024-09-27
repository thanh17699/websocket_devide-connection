using Serilog;
using Vidoc.Socket;
using Vidoc.Socket.Configs;


public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File($"{AppDomain.CurrentDomain.BaseDirectory}logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting up the service");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;
                var appConfig = config.GetSection("AppConfig").Get<AppConfig>();
                services.AddSingleton(appConfig);
                services.AddHostedService<Worker>();
            })
            .UseWindowsService();
}


