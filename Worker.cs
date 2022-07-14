using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using RabbitMQToHTTPLoadBalancingService.Configuration;
using RabbitMQToHTTPLoadBalancingService.RabbitMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQToHTTPLoadBalancingService
{
    public class Worker : BackgroundService
    {
        private readonly Logger logger;

        public Worker(Logger logger, IConfiguration config)
        {
            this.logger = logger;

            List<string> ips = config.GetSection("AllowedIPs").Get<List<string>>();
            IPStorage.SetIps(ips, this.logger);
            rabbitMQConfigs = config.GetSection("RabbitMQConsumers").Get<List<RabbitMQConfig>>();
            this.logger.Info("RabbitMQConsumers: {@rabbit}", rabbitMQConfigs);
        }

        private readonly List<RabbitMQConfig> rabbitMQConfigs = new();
        private readonly List<RabbitMQServer> servers = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() => 
            {
                Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                logger.Info("Start service at {date}", DateTime.UtcNow);
                foreach(RabbitMQConfig configs in rabbitMQConfigs)
                {
                    servers.Add(new(configs, logger));
                }
            }, stoppingToken);
        }
    }
}
