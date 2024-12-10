using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NationalArchives.Taxonomy.Batch.Update.OpenSearch.Service;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Service.Impl;
using NationalArchives.Taxonomy.Common.Service.Interface;
using NLog.Extensions.Logging;
using System;
using System.Text;

namespace NationalArchives.Taxonomy.Batch.Update.OpenSearch
{
    class Program
    {
        private const string EVENT_SOURCE = "Taxonomy Open Search Update";

        public static void Main(string[] args)
        {

            ILogger<Program> serviceLogger = null;

            try
            {
                var eventLogSettings = new EventLogSettings() { SourceName = EVENT_SOURCE };

                using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().AddEventSourceLogger().AddEventLog(eventLogSettings)))
                {
                    serviceLogger = loggerFactory.CreateLogger<Program>();
                    serviceLogger.LogInformation("Starting the taxonomy Open Search update service.");
                }

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("An error occurred whilst initialising or running the taxonomy Open Search update:");
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
                    config.AddUserSecrets<Program>();
                }).UseWindowsService();


        private static void ConfigureServicesForHost(HostBuilderContext context, IServiceCollection services)
        {
            IConfiguration config = context.Configuration;

            var openSearchUpdateParams = config.GetSection(nameof(OpenSearchUpdateParams)).Get<OpenSearchUpdateParams>();

            //var stagingQueueParams = config.GetSection(nameof(UpdateStagingQueueParams)).Get<UpdateStagingQueueParams>();
            var stagingQueueParams = config.GetSection("AmazonSqsParams").Get<AmazonSqsStagingQueueParams>();

            var updateOpenSearchConnParams = config.GetSection(nameof(UpdateOpenSearchConnectionParameters)).Get<UpdateOpenSearchConnectionParameters>();

            services.AddSingleton(typeof(ILogger<UpdateOpenSearchWindowsService>), typeof(Logger<UpdateOpenSearchWindowsService>));
            services.AddSingleton(typeof(ILogger<UpdateOpenSearchService>), typeof(Logger<UpdateOpenSearchService>));

            //Staging queue for updates.  Needs to be a singleton or we get multiple consumers!
            services.AddSingleton<IUpdateStagingQueueReceiver>((ctx) =>
            {
                return new AmazonSqsUpdateReceiver(stagingQueueParams);
            });

            services.AddTransient<IOpenSearchIAViewUpdateRepository, OpenSearchIAViewUpdateRepository>((ctx) =>
            {
                return new OpenSearchIAViewUpdateRepository(updateOpenSearchConnParams);
            });

            services.AddSingleton<IUpdateOpenSearchService>((ctx) =>
            {
                uint bulkUpdateBatchSize = openSearchUpdateParams.BulkUpdateBatchSize;
                uint queueFetchWaitTime = openSearchUpdateParams.QueueFetchSleepTime;
                Console.WriteLine($"Using a batch size of {bulkUpdateBatchSize} and a queue fetch interval of {queueFetchWaitTime} sceonds for Open Search bulk updates.");

                IUpdateStagingQueueReceiver interimQueue = ctx.GetRequiredService<IUpdateStagingQueueReceiver>();  
                IOpenSearchIAViewUpdateRepository updateRepo = ctx.GetRequiredService<IOpenSearchIAViewUpdateRepository>();
                ILogger<UpdateOpenSearchService> logger = ctx.GetRequiredService<ILogger<UpdateOpenSearchService>>();
                return new UpdateOpenSearchService(interimQueue, updateRepo, logger, bulkUpdateBatchSize, queueFetchWaitTime);
            });

            services.AddHostedService<UpdateOpenSearchWindowsService>();

            ServiceProvider provider = services.BuildServiceProvider();
        }
    }
}
