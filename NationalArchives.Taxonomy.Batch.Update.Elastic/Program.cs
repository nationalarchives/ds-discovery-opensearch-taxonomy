using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NationalArchives.Taxonomy.Batch.Update.Elastic.Service;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.Elastic;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using NationalArchives.Taxonomy.Common.Service.Impl;
using NationalArchives.Taxonomy.Common.Service.Interface;
using NLog.Extensions.Logging;
using System;
using System.Text;

namespace NationalArchives.Taxonomy.Batch.Update.Elastic
{
    class Program
    {
        private const string EVENT_SOURCE = "Taxonomy Elastic Search Update";

        public static void Main(string[] args)
        {

            ILogger<Program> serviceLogger = null;

            try
            {
                var eventLogSettings = new EventLogSettings() { SourceName = EVENT_SOURCE };

                using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().AddEventSourceLogger().AddEventLog(eventLogSettings)))
                {
                    serviceLogger = loggerFactory.CreateLogger<Program>();
                    serviceLogger.LogInformation("Starting the taxonomy elastic update service.");
                }

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("An error occurred whilst initialising or running the taxonomy elastic search update:");
                sb.Append("\n");
                sb.Append("Error: " + e.Message);
                sb.Append("\n");
                sb.Append("StackTrace: \n" + e.StackTrace);

                serviceLogger?.LogCritical(sb.ToString());
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServicesForHost(hostContext, services);
                }).ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                }).ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables("TAXONOMY_");
                }).UseWindowsService();


        private static void ConfigureServicesForHost(HostBuilderContext context, IServiceCollection services)
        {
            IConfiguration config = context.Configuration;

            var elasticUpdateParams = config.GetSection(nameof(ElasticUpdateParams)).Get<ElasticUpdateParams>();
            var stagingQueueParams = config.GetSection(nameof(UpdateStagingQueueParams)).Get<UpdateStagingQueueParams>();
            var updateElasticConnParams = config.GetSection(nameof(UpdateElasticConnectionParameters)).Get<UpdateElasticConnectionParameters>();

            services.AddSingleton(typeof(ILogger<UpdateElasticWindowsService>), typeof(Logger<UpdateElasticWindowsService>));
            services.AddSingleton(typeof(ILogger<UpdateElasticService>), typeof(Logger<UpdateElasticService>));


            //Staging queue for updates.  Needs to be a singleton or we get multiple consumers!
            services.AddSingleton<IUpdateStagingQueueReceiver>((ctx) =>
            {
                return new ActiveMqUpdateReceiver(stagingQueueParams);
            });

            services.AddTransient<IElasticIAViewUpdateRepository, ElasticIAViewUpdateRepository>((ctx) =>
            {
                return new ElasticIAViewUpdateRepository(updateElasticConnParams);
            });

            services.AddSingleton<IUpdateElasticService>((ctx) =>
            {
                uint bulkUpdateBatchSize = elasticUpdateParams.BulkUpdateBatchSize;
                uint queueFetchWaitTime = elasticUpdateParams.QueueFetchSleepTime;
                Console.WriteLine($"Using a batch size of {bulkUpdateBatchSize} and a queue fetch interval of {queueFetchWaitTime} sceonds for Elastic bulk updates.");

                IUpdateStagingQueueReceiver interimQueue = ctx.GetRequiredService<IUpdateStagingQueueReceiver>();  
                IElasticIAViewUpdateRepository updateRepo = ctx.GetRequiredService<IElasticIAViewUpdateRepository>();
                ILogger<UpdateElasticService> logger = ctx.GetRequiredService<ILogger<UpdateElasticService>>();
                return new UpdateElasticService(interimQueue, updateRepo, logger, bulkUpdateBatchSize, queueFetchWaitTime);
            });

            services.AddHostedService<UpdateElasticWindowsService>();

            ServiceProvider provider = services.BuildServiceProvider();
        }
    }
}
