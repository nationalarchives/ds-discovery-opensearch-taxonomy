using AutoMapper;
using Lucene.Net.Analysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue;
using NationalArchives.Taxonomy.Batch.FullReindex.Producers;
using NationalArchives.Taxonomy.Batch.FullReindex.Queues;
using NationalArchives.Taxonomy.Batch.Service;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Service;
using NLog.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("NationalArchives.Taxonomy.Batch.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NationalArchives.Taxonomy.Batch
{
    class Program
    {
        private const string EVENT_SOURCE = "Taxonomy Generator";

        private static OperationMode _operationMode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Aruments must include either -f (full reindex) or -d (daily updates)</param>
        /// <returns></returns>
        public static void Main(string[] args)
        {
            ILogger<Program> serviceLogger = null;

            try
            {
                var eventLogSettings = new EventLogSettings() { SourceName = EVENT_SOURCE };

                using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().AddEventSourceLogger().AddEventLog(eventLogSettings)))
                {
                    serviceLogger = loggerFactory.CreateLogger<Program>();
                    serviceLogger.LogInformation("Starting the taxonomy generator.");
                }

                var builder = CreateHostBuilder(args);
                builder.Build().Run();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("An error occurred whilst initialising or running the taxonomy generator:");
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
            try
            {
                _operationMode = (OperationMode)Enum.Parse(typeof(OperationMode), config.GetValue<string>("OperationMode"), true);
            }
            catch (ArgumentException ae)
            {
                throw new ApplicationException("Invalid or missing operation mode. Please specify either Full_Reindex or Daily_Update for the OperationMode in the appsetting.json file.");
            }

            string categorisationParamsConfigSource = _operationMode == OperationMode.Full_Reindex ? "CategorisationParamsFullReindex" : "CategorisationParamsDailyUpdates";

            CategorisationParams categorisationParams = config.GetSection(categorisationParamsConfigSource).Get<CategorisationParams>();

            services.AddAutoMapper(mc => mc.AddMaps(new[] { "NationalArchives.Taxonomy.Common" }));

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<Program>), typeof(Logger<Program>));

            services.AddSingleton(typeof(ILogger<Analyzer>), typeof(Logger<Analyzer>));
            services.AddSingleton(typeof(ILogger<ICategoriserRepository>), typeof(Logger<InMemoryCategoriserRepository>));
  
            DiscoveryOpenSearchConnectionParameters discoveryOpenSearchConnParams = config.GetSection("DiscoveryOpenSearchParams").Get<DiscoveryOpenSearchConnectionParameters>();
           
            services.AddSingleton<CategorisationParams>(categorisationParams);
            // Need to add as a service as FullReindexService and DailyUpdate service are instantiated via AddHostedService where we can't pass parameters directly.

            CategoriserLuceneParams categoriserLuceneParams = config.GetSection("CategoriserLuceneParams").Get<CategoriserLuceneParams>();
            LuceneHelperTools.ConfigureLuceneServices(categoriserLuceneParams, services);

            // IAIDs connection info
            services.AddTransient<IConnectOpenSearch<OpenSearchRecordAssetView>>((ctx) =>
            {
                IConnectOpenSearch<OpenSearchRecordAssetView> recordAssetsOpenSearchConnection = new OpenSearchConnection<OpenSearchRecordAssetView>(discoveryOpenSearchConnParams);
                return recordAssetsOpenSearchConnection;
            });

            // IAView repo i.e. to fetch IAIDs for indexing, using category connection info.
            services.AddTransient<IIAViewRepository>((ctx) =>
            {
                IMapper mapper = ctx.GetRequiredService<IMapper>();
                IConnectOpenSearch<OpenSearchRecordAssetView> openSearchConnectionInfo = ctx.GetRequiredService<IConnectOpenSearch<OpenSearchRecordAssetView>>();
                LuceneHelperTools luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
                OpenSearchIAViewRepository iaRepo = new OpenSearchIAViewRepository(openSearchConnectionInfo, luceneHelperTools, mapper);
                return iaRepo;
            });


            CategorySource categorySource = (CategorySource)Enum.Parse(typeof(CategorySource), config.GetValue<string>("CategorySource"));
            // Get the categories form either Mongo or open Search
            switch(categorySource)
            {
                case CategorySource.OpenSearch:

                    // Categories connection info
                    services.AddTransient<IConnectOpenSearch<CategoryFromOpenSearch>>((ctx) =>
                    {
                        CategoryDataOpenSearchConnectionParameters categoryDataOpenSearchConnParams = config.GetSection("CategoryOpenSearchParams").Get<CategoryDataOpenSearchConnectionParameters>();
                        IConnectOpenSearch<CategoryFromOpenSearch> categoriesOpenSearchConnection = new OpenSearchConnection<CategoryFromOpenSearch>(categoryDataOpenSearchConnParams);
                        return categoriesOpenSearchConnection;
                    });

                    // category list repo using category connection info.
                    services.AddTransient<ICategoryRepository, OpenSearchCategoryRepository>((ctx) =>
                    {
                        IMapper mapper = ctx.GetRequiredService<IMapper>();
                        IConnectOpenSearch<CategoryFromOpenSearch> openSearchConnectionInfo = ctx.GetRequiredService<IConnectOpenSearch<CategoryFromOpenSearch>>();
                        OpenSearchCategoryRepository categoryRepo = new OpenSearchCategoryRepository(openSearchConnectionInfo, mapper);
                        return categoryRepo;
                    });

                    break;

                case CategorySource.Mongo:
                    //Mongo categories
                    services.AddSingleton<ICategoryRepository, MongoCategoryRepository>((ctx) =>
                    {
                        IMapper mapper = ctx.GetRequiredService<IMapper>();
                        MongoConnectionParams categoryDataMongoConnParams = config.GetSection("CategoryMongoParams").Get<MongoConnectionParams>();
                        MongoCategoryRepository categoryRepo = new MongoCategoryRepository(categoryDataMongoConnParams, mapper);
                        return categoryRepo;
                    });

                    break;
                default:
                    throw new ApplicationException("Invalid category Source");
            }


            services.AddTransient<ICategoriserRepository>((ctx) =>
            {
                var analyser = ctx.GetRequiredService<Analyzer>();
                var luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
                var logger = ctx.GetRequiredService<ILogger<ICategoriserRepository>>();
                InMemoryCategoriserRepository categoriserRepo = new InMemoryCategoriserRepository(iaViewIndexAnalyser: analyser, luceneHelperTools: luceneHelperTools, logger: logger, batchSize: categorisationParams.BatchSize);
                return categoriserRepo; 
            });

            //########################################################################

            UpdateStagingQueueParams updateStagingQueueParams = config.GetSection("UpdateStagingQueueParams").Get<UpdateStagingQueueParams>();
            services.AddSingleton<UpdateStagingQueueParams>(updateStagingQueueParams);

            if (_operationMode == OperationMode.Full_Reindex)
            {
                services.AddSingleton(typeof(ILogger<IUpdateStagingQueueSender>), typeof(Logger<AmazonSqsUpdateSender>));

                services.AddSingleton<IUpdateStagingQueueSender>((ctx) =>
                {
                    var logger = ctx.GetRequiredService<ILogger<IUpdateStagingQueueSender>>();
                    UpdateStagingQueueParams qParams = ctx.GetRequiredService<UpdateStagingQueueParams>();
                    return new AmazonSqsUpdateSender(updateStagingQueueParams, logger);
                });
            }

            if (_operationMode == OperationMode.Daily_Update)
            {
                services.AddSingleton(typeof(ILogger<IUpdateStagingQueueSender>), typeof(Logger<AmazonSqsDirectUpdateSender>));

                services.AddSingleton<IUpdateStagingQueueSender>((ctx) =>
                {
                    var logger = ctx.GetRequiredService<ILogger<IUpdateStagingQueueSender>>();
                    return new AmazonSqsDirectUpdateSender(updateStagingQueueParams, logger);
                });
            }
            //########################################################################

            services.AddTransient<ICategoriserService<CategorisationResult>>((ctx) =>
            {
                ICategoryRepository categeoryRepo = ctx.GetRequiredService<ICategoryRepository>(); // source repo for list of categories.
                IIAViewRepository iaViewRepository = ctx.GetRequiredService<IIAViewRepository>();  // source repo for information assets.
                ICategoriserRepository categoriserRepo = ctx.GetRequiredService<ICategoriserRepository>();
                IUpdateStagingQueueSender stagingQueueSender = ctx.GetRequiredService<IUpdateStagingQueueSender>();
                
                return new QueryBasedCategoriserService(iaViewRepository, categeoryRepo, categoriserRepo, stagingQueueSender);
            });

            services.AddTransient<IInformationAssetViewService>((ctx) =>
            {
                IIAViewRepository iaViewRepository = ctx.GetRequiredService<IIAViewRepository>();
                return new InformationAssetViewService(iaViewRepository);
            });


            if (_operationMode == OperationMode.Full_Reindex)
            {
                FullReindexQueueParams fullIndexQParams = config.GetSection("FullReindexQueueParams").Get<FullReindexQueueParams>();

                FullReindexIaidProducerSource fullReindexIaidSource = (FullReindexIaidProducerSource)Enum.Parse(typeof(FullReindexIaidProducerSource), fullIndexQParams.IaidSource);
                services.AddSingleton<FullReindexQueueParams>(fullIndexQParams);


                services.AddSingleton<FullReIndexIaidPcQueue<string>>((ctx) =>
                {
                    FullReindexQueueParams qparams = ctx.GetRequiredService<FullReindexQueueParams>();
                    return new FullReIndexIaidPcQueue<string>(qparams.MaxSize);
                }); // =>  FullReindexService

                var openSearchAssetBrowseParams = config.GetSection("OpenSearchAssetFetchParams").Get<OpenSearchAssetBrowseParams>();

                switch(fullReindexIaidSource)
                {
                    case FullReindexIaidProducerSource.OpenSearch:
                        services.AddSingleton<IIAIDProducer>((ctx) =>
                        {
                            var iaViewService = ctx.GetRequiredService<IInformationAssetViewService>();
                            var logger = ctx.GetRequiredService<ILogger<FullReindexService>>();
                            var reindexQueue = ctx.GetRequiredService<FullReIndexIaidPcQueue<string>>();

                            return new FullReindexOpenSearchIaidProducer(reindexQueue, iaViewService, openSearchAssetBrowseParams, logger, discoveryOpenSearchConnParams.Uri.AbsoluteUri);
                        });
                        break;
                    case FullReindexIaidProducerSource.SqsQueue:
                        services.AddSingleton<IIAIDProducer>((ctx) =>
                        {
                            var qparams = ctx.GetRequiredService<FullReindexQueueParams>();
                            var iaViewService = ctx.GetRequiredService<IInformationAssetViewService>();
                            var logger = ctx.GetRequiredService<ILogger<FullReindexService>>();
                            var reindexQueue = ctx.GetRequiredService<FullReIndexIaidPcQueue<string>>();

                            return new FullReindexSqsQueueIaidProducer(qparams, reindexQueue, iaViewService, openSearchAssetBrowseParams, logger);
                        });
                        break;
                    default:
                        throw new Exception("Invalid Iaid Source for Fulll Reindex Operation");
                }

                services.AddSingleton(typeof(ILogger<FullReindexService>), typeof(Logger<FullReindexService>));
                services.AddHostedService<FullReindexService>();
            }


            if (_operationMode == OperationMode.Daily_Update)
            {
                DailyUpdateQueueParams dailyUpdatemessageQueueParams = config.GetSection("DailyUpdateQueueParams").Get<DailyUpdateQueueParams>();
                services.AddSingleton<DailyUpdateQueueParams>(dailyUpdatemessageQueueParams);

                services.AddSingleton(typeof(ILogger<DailyUpdatesManagerService>), typeof(Logger<DailyUpdatesManagerService>));

                services.AddSingleton(typeof(ILogger<CategoriseDocAmazonSqsConsumer>), typeof(Logger<CategoriseDocAmazonSqsConsumer>));
                services.AddSingleton(typeof(ILogger<DeleteDocAmazonSqsMessageConsumer>), typeof(Logger<DeleteDocAmazonSqsMessageConsumer>));

                

                services.AddSingleton<ISourceIaidInputQueueConsumer>((ctx) =>
                {
                    DailyUpdateQueueParams dailyUpdatemessageQueueParams = ctx.GetRequiredService<DailyUpdateQueueParams>();
                    ICategoriserService<CategorisationResult> categoriserService = ctx.GetRequiredService<ICategoriserService<CategorisationResult>>();
                    ILogger<CategoriseDocAmazonSqsConsumer> categoriseConsumerLogger = ctx.GetRequiredService<ILogger<CategoriseDocAmazonSqsConsumer>>();
                    return new CategoriseDocAmazonSqsConsumer(categoriserService, dailyUpdatemessageQueueParams, categoriseConsumerLogger);
                });

                services.AddSingleton<DeleteDocAmazonSqsMessageConsumer>((ctx) =>
                {
                    AmazonSqsParams dailyUpdatemessageQueueParams = ctx.GetRequiredService<AmazonSqsParams>();
                    ILogger<DeleteDocAmazonSqsMessageConsumer> dailyDeleteLogger = ctx.GetRequiredService<ILogger<DeleteDocAmazonSqsMessageConsumer>>();
                    return new DeleteDocAmazonSqsMessageConsumer(dailyUpdatemessageQueueParams, dailyDeleteLogger);
                });

                services.AddHostedService<DailyUpdatesManagerService>();
            }

            ServiceProvider provider = services.BuildServiceProvider(); 
        }
    }
}
