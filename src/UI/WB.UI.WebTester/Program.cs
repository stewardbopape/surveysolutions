using System;
using System.IO;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace WB.UI.WebTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                CreateHostBuilder(args)
                    .Build()
                    .Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((host, loggerConfig) =>
                {
                    var logsFileLocation = Path.Combine(host.HostingEnvironment.ContentRootPath, "..", "logs", "log.log");
                    var verboseLog = Path.Combine(host.HostingEnvironment.ContentRootPath, "..", "logs", "verbose.log");

                    loggerConfig
                        //.MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("Quartz.Core", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.File(logsFileLocation, rollingInterval: RollingInterval.Day,
                            restrictedToMinimumLevel: LogEventLevel.Warning)
                        .WriteTo.File(verboseLog, rollingInterval: RollingInterval.Day,
                            restrictedToMinimumLevel: LogEventLevel.Verbose, retainedFileCountLimit: 2);
                    if (host.HostingEnvironment.IsDevelopment())
                    {
                        // To debug logitems source add {SourceContext} to output template
                        // outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                        loggerConfig.WriteTo.Console();
                    }
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.AddIniFile("appsettings.ini", false, true);
                    c.AddIniFile("appsettings.cloud.ini", true, true);
                    c.AddIniFile($"appsettings.{Environment.MachineName}.ini", true);
                    c.AddIniFile("appsettings.Production.ini", true);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
