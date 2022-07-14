using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System;
using System.IO;

namespace RabbitMQToHTTPLoadBalancingService
{
    public class Program
    {
        public static string BasePath { get; set; }
        static Logger logger;
        public static void Main(string[] args)
        {
            BasePath = GetBasePath();
            string pth = Path.Combine(BasePath, "nlog.config");
            logger = NLogBuilder.ConfigureNLog(pth).GetCurrentClassLogger();
            LogManager.Configuration.Variables["logDir"] = BasePath;

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(BasePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            _ = builder.Build();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton(logger);
                }).ConfigureAppConfiguration((builderContext, config) =>
                {
                    config.SetBasePath(GetBasePath());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                }).UseWindowsService().ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                }).UseNLog();
        }

        private static string GetBasePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
