﻿using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Infrastructure.Native.Monitoring;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.UI.Headquarters.Configs;

namespace WB.UI.Headquarters.Metrics
{
    public static class UseExtensions
    {
        public static void UseMetrics(this IServiceCollection services)
        {
            services.AddHostedService<PushGatewayMetricsPusher>();
            services.AddTransient<IOnDemandCollector, BrokenPackagesStatsCollector>();
            services.AddTransient<IOnDemandCollector, NHibernateStatsCollector>();
        }

        public static void UseMetrics(this IApplicationBuilder app, IConfiguration configuration)
        {
            var metricsConfig = configuration.MetricsConfiguration().Get<MetricsConfig>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<MetricsRegistry>>();

            var collectors = app.ApplicationServices.GetServices<IOnDemandCollector>();

            MetricsRegistry.Instance.RegisterOnDemandCollectors(collectors.ToArray());

            if (metricsConfig.UseMetricsEndpoint)
            {
                app.UseMetricServer();
                logger.LogInformation("Metrics server started");
            }
        }
    }
}
