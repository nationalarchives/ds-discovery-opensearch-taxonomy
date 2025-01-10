using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using OpenSearch.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class OpenSearchIAViewUpdateRepository : IOpenSearchIAViewUpdateRepository
    {
        private readonly OpenSearchClient _openSearchClient;

        //TODO: Not using the IConnectElastic interface here, it just seems to get in the way, look at refactoring generally.
        // But see where we get to on using Lucene.net and the InfoAseet input source.
        public OpenSearchIAViewUpdateRepository(OpenSearchConnectionParameters openSearchConnectionParameters)
        {
            using (ConnectionSettings connectionSettings = ConnectionSettingsProvider.GetConnectionSettings(openSearchConnectionParameters))
            {
                connectionSettings.DefaultFieldNameInferrer(p => p);
                _openSearchClient = new OpenSearchClient(connectionSettings);
            };
        }

        public IaidWithCategories GetByDocReference(string docReference)
        {
            throw new NotImplementedException();
        }

        public async Task Save(IaidWithCategories iaidWithCategories)
        {
            if (iaidWithCategories == null)
            {
                throw new TaxonomyException("No IAID  with categories supplied to the elastic search update service.");
            }

            var update = new { TAXONOMY_ID = iaidWithCategories.CategoryIds };
            var response = await _openSearchClient.UpdateAsync<OpenSearchRecordAssetView, object>(iaidWithCategories.Iaid, u => u.Doc(update).DocAsUpsert());
            if(!response.IsValid)
            {
                string errorInfo = GetOpenSearchErrorInfo(response);

                throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_UPDATE_ERROR, errorInfo);
            }
        }

        public async Task SaveAll(IEnumerable<IaidWithCategories> iaidsWithCategories)
        {
            if(iaidsWithCategories == null)
            {
                throw new TaxonomyException("No IAID list with categories supplied to the Open search update service.");
            }

            var descriptor = new BulkDescriptor();

            foreach (var iaidWithCategories in iaidsWithCategories)
            {
                var doc = new { TAXONOMY_ID = iaidWithCategories.CategoryIds };
                descriptor.Update<OpenSearchRecordAssetView, object>(u => u.Doc(doc).DocAsUpsert(true).Id(iaidWithCategories.Iaid));
            }

            BulkResponse response = await _openSearchClient.BulkAsync(descriptor);
            if (!response.IsValid)
            {
                string errorInfo = GetOpenSearchErrorInfo(response);
                throw new TaxonomyException(TaxonomyErrorType.OPEN_SEARCH_BULK_UPDATE_ERROR, errorInfo);
            }
        }

        private String GetOpenSearchErrorInfo(IResponse response)
        {
            StringBuilder sb = new StringBuilder("Invalid update response from Open Search");
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
