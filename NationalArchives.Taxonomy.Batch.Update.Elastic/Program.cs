using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NationalArchives.Taxonomy.Batch.Update.OpenSearch.Service;
using NationalArchives.Taxonomy.Common.BusinessObjects;
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

            //var openSearchUpdateParams = config.GetSection("DiscoveryOpenSearchParams").Get<OpenSearchUpdateParams>();

            //var stagingQueueSqsParams = config.GetSection("UpdateStagingQueueParams").Get<AmazonSqsParams>();

            DiscoveryOpenSearchConnectionParameters discoveryOpenSearchConnParams = config.GetSection("DiscoveryOpenSearchParams").Get<DiscoveryOpenSearchConnectionParameters>();
            UpdateStagingQueueParams updateStagingQueueParams = config.GetSection("UpdateStagingQueueParams").Get<UpdateStagingQueueParams>();
            OpenSearchUpdateParams upDateParams = config.GetSection("OpenSearchUpdateParams").Get<OpenSearchUpdateParams>();

            services.AddSingleton(typeof(ILogger<UpdateOpenSearchWindowsService>), typeof(Logger<UpdateOpenSearchWindowsService>));
            services.AddSingleton(typeof(ILogger<UpdateOpenSearchService>), typeof(Logger<UpdateOpenSearchService>));
            services.AddSingleton(typeof(IAmazonSqsMessageReader<IaidWithCategories>), typeof(AmazonSqsJsonMessageReader<IaidWithCategories>));

            //Staging queue for updates.  Needs to be a singleton or we get multiple consumers!
            services.AddSingleton<IUpdateStagingQueueReceiver<IaidWithCategories>>((ctx) =>
            {
                var qParams = updateStagingQueueParams.AmazonSqsParams;
                var messageReader = ctx.GetRequiredService<IAmazonSqsMessageReader<IaidWithCategories>>();
                return new AmazonSqsReceiver<IaidWithCategories>(qParams, messageReader);
            });

            services.AddTransient<IOpenSearchIAViewUpdateRepository, OpenSearchIAViewUpdateRepository>((ctx) =>
            {
                return new OpenSearchIAViewUpdateRepository(discoveryOpenSearchConnParams);
            });

            services.AddSingleton<IUpdateOpenSearchService>((ctx) =>
            {
                int bulkUpdateBatchSize = upDateParams.BulkUpdateBatchSize;
                int queueFetchSleepTime = upDateParams.QueueFetchSleepTime;
                int queueFetchWaitTime = upDateParams.WaitMilliseconds;
                int searchDatabaseUpdateInterval = upDateParams.SearchDatabaseUpdateInterval;
                int maxInternalQueueSize = upDateParams.MaxInternalQueueSize;
                int nullCounterHours = upDateParams.NullCounterHours;

                Console.WriteLine($"Using a batch size of {bulkUpdateBatchSize} and a queue fetch interval of {queueFetchSleepTime} sceonds for Open Search bulk updates.");

                IUpdateStagingQueueReceiver<IaidWithCategories> interimQueue = ctx.GetRequiredService<IUpdateStagingQueueReceiver<IaidWithCategories>>();  
                IOpenSearchIAViewUpdateRepository updateRepo = ctx.GetRequiredService<IOpenSearchIAViewUpdateRepository>();
                ILogger<UpdateOpenSearchService> logger = ctx.GetRequiredService<ILogger<UpdateOpenSearchService>>();
                return new UpdateOpenSearchService(interimQueue, updateRepo, logger, bulkUpdateBatchSize, 
                    queueFetchWaitTime, queueFetchSleepTime, searchDatabaseUpdateInterval, maxInternalQueueSize, nullCounterHours);
            });

            services.AddHostedService<UpdateOpenSearchWindowsService>();

            ServiceProvider provider = services.BuildServiceProvider();
        }
    }
}
