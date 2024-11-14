using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Elastic;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    public class ElasticIAViewUpdateRepository : IElasticIAViewUpdateRepository
    {
        //private ElasticConnectionParameters _parameters;
        private ElasticClient _elasticClient;

        //TODO: Not using the IConnectElastic interface here, it just seems to get in the way, look at refactoring generally.
        // But see where we get to on using Lucene.net and the InfoAseet input source.
        public ElasticIAViewUpdateRepository(ElasticConnectionParameters elasticConnectionParameters)
        {
            using (ConnectionSettings connectionSettings = ConnectionSettingsProvider.GetConnectionSettings(elasticConnectionParameters))
            {
                connectionSettings.DefaultFieldNameInferrer(p => p);
                _elasticClient = new ElasticClient(connectionSettings);
            };
        }

        public IaidWithCategories GetByDocReference(string docReference)
        {
            throw new NotImplementedException();
        }

        public void Save(IaidWithCategories iaidWithCategories)
        {
            if (iaidWithCategories == null)
            {
                throw new TaxonomyException("No IAID  with categories supplied to the elastic search update service.");
            }

            var update = new { TAXONOMY_ID = iaidWithCategories.CategoryIds };
            var response = _elasticClient.Update<ElasticRecordAssetView, object>(iaidWithCategories.Iaid, u => u.Doc(update).DocAsUpsert());
            if(!response.IsValid)
            {
                string errorInfo = GetElasticErrorINfo(response);

                throw new TaxonomyException(TaxonomyErrorType.ELASTIC_UPDATE_ERROR, errorInfo);
            }
        }

        public void SaveAll(IEnumerable<IaidWithCategories> iaidsWithCategories)
        {
            if(iaidsWithCategories == null)
            {
                throw new TaxonomyException("No IAID list with categories supplied to the elastic search update service.");
            }

            var descriptor = new BulkDescriptor();

            foreach (var iaidWithCategories in iaidsWithCategories)
            {
                var doc = new { TAXONOMY_ID = iaidWithCategories.CategoryIds };
                descriptor.Update<ElasticRecordAssetView, object>(u => u.Doc(doc).DocAsUpsert(true).Id(iaidWithCategories.Iaid));
            }

            //TODO: Async?
            var response = _elasticClient.BulkAsync(descriptor).Result;
            if (!response.IsValid)
            {
                string errorInfo = GetElasticErrorINfo(response);
                throw new TaxonomyException(TaxonomyErrorType.ELASTIC_BULK_UPDATE_ERROR, errorInfo);
            }
        }

        private String GetElasticErrorINfo(IResponse response)
        {
            StringBuilder sb = new StringBuilder("Invalid update response from Elastic Search");
            sb.Append(Environment.NewLine);

            if (response.OriginalException != null)
            {
                sb.AppendLine($"Original Exeception: {response.OriginalException}");
            }
            if (response.ServerError != null)
            {
                sb.AppendLine($"Server Error: {response.ServerError}");
            }
            return sb.ToString();
        }
    }
}
