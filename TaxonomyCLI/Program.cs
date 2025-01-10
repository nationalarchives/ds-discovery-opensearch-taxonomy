using AutoMapper;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NationalArchives.Taxonomy.CLI
{
    public class Program
    {
        private const string SHOW_CONFIG_INFO = "Shows application confirguration information.";

        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Configuring application.");

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables("TAXONOMY_")
                 .AddUserSecrets<Program>();

                var config = builder.Build();
                IServiceProvider provider = ConfigureServices(config, args);

                var app = new CommandLineApplication<Categoriser>();

                app.Conventions
                    .UseDefaultConventions()
                    .UseConstructorInjection(provider);
                int executeResult = app.Execute(args);
                return executeResult;
            }
            catch(AggregateException ae)
            {
                foreach(Exception e in ae.InnerExceptions)
                {
                    Console.WriteLine("Error Occured: " + e.Message);
                }
                return -1;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Occured: " + e.Message);

                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                }
                return -1;
            }
            finally
            {
#if DEBUG
                Console.ReadLine();
#endif
            }
        }

        
        private static ServiceProvider ConfigureServices(IConfigurationRoot config, string[] args)
        {
            var services = new ServiceCollection();

            services.AddAutoMapper(mc => mc.AddMaps(new[] { "NationalArchives.Taxonomy.Common" }));

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<Analyzer>), typeof(Logger<Analyzer>));

            services.AddSingleton<DiscoveryOpenSearchConnectionParameters>(config.GetSection("DiscoveryOpenSearchParams").Get<DiscoveryOpenSearchConnectionParameters>());
            services.AddSingleton<CategoryDataOpenSearchConnectionParameters>(config.GetSection("CategoryOpenSearchParams").Get<CategoryDataOpenSearchConnectionParameters>());
            services.AddSingleton(typeof(ILogger<ICategoriserRepository>), typeof(Logger<InMemoryCategoriserRepository>));

            services.AddTransient<IConnectOpenSearch<OpenSearchRecordAssetView>>((ctx) =>
            {
                OpenSearchConnectionParameters cparams = ctx.GetRequiredService<DiscoveryOpenSearchConnectionParameters>();
                IConnectOpenSearch<OpenSearchRecordAssetView> recordAssetsElasticConnection = new OpenSearchConnection<OpenSearchRecordAssetView>(cparams);
                return recordAssetsElasticConnection;
            });

            services.AddTransient<IIAViewRepository>((ctx) =>
            {
                IMapper mapper = ctx.GetRequiredService<IMapper>();
                IConnectOpenSearch<OpenSearchRecordAssetView> elasticConnectionInfo = ctx.GetRequiredService<IConnectOpenSearch<OpenSearchRecordAssetView>>();
                LuceneHelperTools luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
                OpenSearchIAViewRepository iaRepo = new OpenSearchIAViewRepository(elasticConnectionInfo, luceneHelperTools, mapper);
                return iaRepo;
            });

            CategorySource categorySource = (CategorySource)Enum.Parse(typeof(CategorySource), config.GetValue<string>("CategorySource"));
            // Get the categories form either Mongo or Elastic
            switch (categorySource)
            {
                case CategorySource.OpenSearch:

                    // Categories connection info
                    services.AddTransient<IConnectOpenSearch<CategoryFromOpenSearch>>((ctx) =>
                    {
                        CategoryDataOpenSearchConnectionParameters categoryDataElasticConnParams = config.GetSection("CategoryOpenSearchParams").Get<CategoryDataOpenSearchConnectionParameters>();
                        IConnectOpenSearch<CategoryFromOpenSearch> categoriesElasticConnection = new OpenSearchConnection<CategoryFromOpenSearch>(categoryDataElasticConnParams);
                        return categoriesElasticConnection;
                    });

                    // category list repo using category connection info.
                    services.AddTransient<ICategoryRepository, OpenSearchCategoryRepository>((ctx) =>
                    {
                        IMapper mapper = ctx.GetRequiredService<IMapper>();
                        IConnectOpenSearch<CategoryFromOpenSearch> elasticConnectionInfo = ctx.GetRequiredService<IConnectOpenSearch<CategoryFromOpenSearch>>();
                        OpenSearchCategoryRepository categoryRepo = new OpenSearchCategoryRepository(elasticConnectionInfo, mapper);
                        return categoryRepo;
                    });

                    break;

                case CategorySource.Mongo:
                    //Mongo categories
                    services.AddTransient<ICategoryRepository, MongoCategoryRepository>((ctx) =>
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

            bool hasLiveUpdates = args.Any(a => a.StartsWith("-c"));

            if (hasLiveUpdates)
            {
                //params for update staging queue.
                //services.AddSingleton<UpdateStagingQueueParams>(config.GetSection("UpdateStagingQueueParams").Get<UpdateStagingQueueParams>());
                AmazonSqsStagingQueueParams awsSqsParams = config.GetSection("AmazonSqsParams").Get<AmazonSqsStagingQueueParams>();
                services.AddSingleton<AmazonSqsStagingQueueParams>(awsSqsParams);

                services.AddSingleton(typeof(ILogger<IUpdateStagingQueueSender>), typeof(Logger<AmazonSqsUpdateSender>));

                services.AddSingleton<IUpdateStagingQueueSender>((ctx) =>
                {
                    //UpdateStagingQueueParams qParams = ctx.GetRequiredService<UpdateStagingQueueParams>();
                    //return new ActiveMqDirectUpdateSender(qParams);

                    AmazonSqsStagingQueueParams qParams = ctx.GetRequiredService<AmazonSqsStagingQueueParams>();
                    var logger = ctx.GetRequiredService<ILogger<IUpdateStagingQueueSender>>();
                    return new AmazonSqsDirectUpdateSender(qParams, logger);
                }); 
            }

            CategoriserLuceneParams categoriserLuceneParams = config.GetSection("CategoriserLuceneParams").Get<CategoriserLuceneParams>();

            LuceneHelperTools.ConfigureLuceneServices(categoriserLuceneParams, services);

            services.AddTransient<ICategoriserRepository>((ctx) =>
            {
                //TODO: Further testing on  setting - so far we seem to get better performance with false!
                var analyser = ctx.GetRequiredService<Analyzer>();
                var luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
                var logger = ctx.GetRequiredService<ILogger<ICategoriserRepository>>();
                InMemoryCategoriserRepository categoriserRepo = new InMemoryCategoriserRepository(iaViewIndexAnalyser: analyser, luceneHelperTools: luceneHelperTools, logger: logger);
                return categoriserRepo;
            });

            services.AddTransient<ICategoriserService<CategorisationResult>>((ctx) =>
            {
                ICategoryRepository categeoryRepo = ctx.GetRequiredService<ICategoryRepository>();
                IIAViewRepository iaViewRepository = ctx.GetRequiredService<IIAViewRepository>();
                IUpdateStagingQueueSender stagingQueueSender = hasLiveUpdates ? ctx.GetRequiredService<IUpdateStagingQueueSender>() : null;
                ICategoriserRepository categoriserRepo = ctx.GetRequiredService<ICategoriserRepository>();
                return new QueryBasedCategoriserService
                (   
                    iaViewRepository: iaViewRepository, 
                    categoryRepository: categeoryRepo, 
                    categoriserRepository: categoriserRepo,
                    stagingQueueSender: stagingQueueSender
                );
            });


                ServiceProvider provider = services.BuildServiceProvider();
            return provider;
        }
    }
}
