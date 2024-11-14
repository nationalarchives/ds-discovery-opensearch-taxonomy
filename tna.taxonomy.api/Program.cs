using AutoMapper;
using Lucene.Net.Analysis;
using Microsoft.OpenApi.Models;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.Elastic;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using NationalArchives.Taxonomy.Common.Service;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// Add services to the container.
builder.Services.AddAutoMapper(mc => mc.AddMaps(new[] { "NationalArchives.Taxonomy.Common" }));

builder.Services.AddSingleton(config.GetSection("DiscoveryElasticParams").Get<DiscoverySearchElasticConnectionParameters>());
builder.Services.AddSingleton(config.GetSection("CategoryElasticParams").Get<CategoryDataElasticConnectionParameters>());
builder.Services.AddSingleton(typeof(ILogger<ICategoriserRepository>), typeof(Logger<InMemoryCategoriserRepository>));

builder.Services.AddScoped<IConnectElastic<ElasticRecordAssetView>>((ctx) =>
{
    ElasticConnectionParameters cparams = ctx.GetRequiredService<DiscoverySearchElasticConnectionParameters>();
    IConnectElastic<ElasticRecordAssetView> recordAssetsElasticConnection = new ElasticConnection<ElasticRecordAssetView>(cparams);
    return recordAssetsElasticConnection;
});

CategorySource categorySource = (CategorySource)Enum.Parse(typeof(CategorySource), config.GetValue<string>("CategorySource"));
// Get the categories form either Mongo or Elastic
switch (categorySource)
{
    case CategorySource.Elastic:

        // Categories connection info
        builder.Services.AddTransient<IConnectElastic<CategoryFromElastic>>((ctx) =>
        {
            CategoryDataElasticConnectionParameters categoryDataElasticConnParams = config.GetSection("CategoryElasticParams").Get<CategoryDataElasticConnectionParameters>();
            IConnectElastic<CategoryFromElastic> categoriesElasticConnection = new ElasticConnection<CategoryFromElastic>(categoryDataElasticConnParams);
            return categoriesElasticConnection;
        });

        // category list repo using category connection info.
        builder.Services.AddTransient<ICategoryRepository, ElasticCategoryRepository>((ctx) =>
        {
            IMapper mapper = ctx.GetRequiredService<IMapper>();
            IConnectElastic<CategoryFromElastic> elasticConnectionInfo = ctx.GetRequiredService<IConnectElastic<CategoryFromElastic>>();
            ElasticCategoryRepository categoryRepo = new ElasticCategoryRepository(elasticConnectionInfo, mapper);
            return categoryRepo;
        });

        break;

    case CategorySource.Mongo:
        //Mongo categories
        builder.Services.AddTransient<ICategoryRepository, MongoCategoryRepository>((ctx) =>
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

CategoriserLuceneParams categoriserLuceneParams = config.GetSection("CategoriserLuceneParams").Get<CategoriserLuceneParams>();

//TODO: This repo should be internal but has to be public to inject here.  Can we somehow make it internal
// and still inject via DI?
builder.Services.AddTransient<IIAViewRepository>((ctx) =>
{
    IMapper mapper = ctx.GetRequiredService<IMapper>();
    IConnectElastic<ElasticRecordAssetView> elasticConnectionInfo = ctx.GetRequiredService<IConnectElastic<ElasticRecordAssetView>>();
    LuceneHelperTools luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
    ElasticIAViewRepository iaRepo = new ElasticIAViewRepository(elasticConnectionInfo, luceneHelperTools, mapper);
    return iaRepo;
});

builder.Services.AddTransient<IInformationAssetViewService>((ctx) =>
{

    IIAViewRepository iaViewRepository = ctx.GetRequiredService<IIAViewRepository>();
    return new InformationAssetViewService(iaViewRepository, categoriserLuceneParams.UseDefaultTaxonomyFieldForApiSearch);
});

LuceneHelperTools.ConfigureLuceneServices(categoriserLuceneParams, builder.Services);

builder.Services.AddTransient<ICategoriserRepository>((ctx) =>
{
    var analyser = ctx.GetRequiredService<Analyzer>();
    var luceneHelperTools = ctx.GetRequiredService<LuceneHelperTools>();
    var logger = ctx.GetRequiredService<ILogger<ICategoriserRepository>>();
    InMemoryCategoriserRepository categoriserRepo = new InMemoryCategoriserRepository(iaViewIndexAnalyser: analyser, luceneHelperTools: luceneHelperTools, logger: logger);
    return categoriserRepo;
});

builder.Services.AddTransient<ICategoriserService<CategorisationResult>>((ctx) =>
{
    ICategoryRepository categeoryRepo = ctx.GetRequiredService<ICategoryRepository>();
    IIAViewRepository iaViewRepository = ctx.GetRequiredService<IIAViewRepository>();
    ICategoriserRepository categoriserRepository = ctx.GetRequiredService<ICategoriserRepository>();
    return new QueryBasedCategoriserService(iaViewRepository: iaViewRepository, categoryRepository: categeoryRepo, categoriserRepository: categoriserRepository);
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "taxonomy api", Version = "v1" });
});

var app = builder.Build();

var loggerFactory = app.Services.GetService<ILoggerFactory>();
 loggerFactory.AddFile(config["Logging:LogFilePath"].ToString());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();
app.Run();
