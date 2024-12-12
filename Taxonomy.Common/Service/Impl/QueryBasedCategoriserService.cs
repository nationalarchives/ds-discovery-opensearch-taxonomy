using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Service
{
    public class QueryBasedCategoriserService : ICategoriserService<CategorisationResult>
    {
        private readonly IIAViewRepository _iaViewRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoriserRepository _categoriserRepository;
        private readonly IUpdateStagingQueueSender _stagingQueueSender;
        private readonly CancellationToken _token;

        //TODO: Java version also had inMemoryiaViewRepository - do we need this?

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iaViewRepository">Repository from which source Information Asset data is retrieved for subsequent categorisation.</param>
        /// <param name="categoryRepository">Repository from whihc the list of categories is retrieved (currently Elastic but could be another source type).</param>
        /// <param name="categoriserRepository">The Elastic repository used for categorisation.  If null, then the iaViewRepository is used instead.</param>
        /// <param name="stagingQueue">Queue used to store the results from categorisation i.e. iaid with zero, one or more category IDs.
        /// Currently we're using Active MQ but it could be another queue type.</param>
        public QueryBasedCategoriserService(IIAViewRepository iaViewRepository, ICategoryRepository categoryRepository, ICategoriserRepository categoriserRepository = null, IUpdateStagingQueueSender stagingQueueSender = null, CancellationToken token = default)
        {
            _iaViewRepository = iaViewRepository;
            _categoryRepository = categoryRepository;
            _categoriserRepository = categoriserRepository;
            _stagingQueueSender = stagingQueueSender;
            _token = token;
        }

        public async Task<IList<CategorisationResult>> TestCategoriseSingle(string docReference)
        {
            CheckForValidDocReference(docReference);

            try
            {
                IList<CategorisationResult> results = await TestCategoriseSingleAsync(docReference);
                return results;
            }
            catch (Exception e)
            {
                throw;
            }

            async Task<IList<CategorisationResult>> TestCategoriseSingleAsync(string docReference1)
            {
                InformationAssetView iaView = await _iaViewRepository.SearchDocByDocReference(docReference);
                IList<CategorisationResult> results1 = await TestCategoriseSingle(iaView, true, null);
                return results1;
            }
        }

        public async Task<IList<CategorisationResult>> TestCategoriseSingle(string docReference, bool includeScores)
        {
            CheckForValidDocReference(docReference);

            try
            {
                IList<CategorisationResult> results = await TestCategoriseSingleAsync(docReference);
                return results;
            }
            catch (Exception e)
            {
                throw;
            }

            async Task<IList<CategorisationResult>> TestCategoriseSingleAsync(string docReference1)
            {
                InformationAssetView iaView = await _iaViewRepository.SearchDocByDocReference(docReference);
                IList<CategorisationResult> results1 = await TestCategoriseSingle(iaView, includeScores, null);
                return results1;
            }
        }


        public async Task<IList<CategorisationResult>> TestCategoriseSingle(InformationAssetView iaView, bool retrieveScoreForAllRelevantCategories, IList<Category> cachedCategories)
        {

            CheckForValidDocReference(iaView?.CatDocRef);

            try
            {
                IList<CategorisationResult> results = await TestCategoriseSingleAsync(iaView, retrieveScoreForAllRelevantCategories, cachedCategories);
                return results;
            }
            catch (Exception e)
            {
                throw;
            }

            async Task<IList<CategorisationResult>> TestCategoriseSingleAsync(InformationAssetView iaView1, bool retrieveScoreForAllRelevantCategories1, IList<Category> cachedCategories1)
            {
                IList<Category> sourceCategories = cachedCategories ?? await _categoryRepository.FindAll();

                // TODO: If we do use CategoryWithElasticQuery as per above, then we will be able to pass this directly, either
                // via another overload or variance.
                IList<CategorisationResult> listOfCategorisationResults;


                try
                {
                    if (_categoriserRepository != null)
                    {
                        listOfCategorisationResults = _categoriserRepository.FindRelevantCategoriesForDocument(iaView, sourceCategories, retrieveScoreForAllRelevantCategories1);
                    }
                    else
                    {
                        listOfCategorisationResults = _iaViewRepository.FindRelevantCategoriesForDocument(iaView, sourceCategories, retrieveScoreForAllRelevantCategories1);
                    }

                    return listOfCategorisationResults;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }


        private async Task<IDictionary<string, List<CategorisationResult>>> TestCategoriseMultiple(InformationAssetView[] iaViews, bool retrieveScoreForAllRelevantCategories, IList<Category> cachedCategories)
        {

            try
            {
                IDictionary<string, List<CategorisationResult>> results =  await TestCategoriseMultipleAsync(iaViews, retrieveScoreForAllRelevantCategories, cachedCategories);
                return results;
            }
            catch (Exception e)
            {
                throw;
            }

            async Task<IDictionary<string, List<CategorisationResult>>> TestCategoriseMultipleAsync(InformationAssetView[] iaViews1, bool retrieveScoreForAllRelevantCategories1, IList<Category> cachedCategories1)
            {
                IList<Category> sourceCategories = cachedCategories ?? await _categoryRepository.FindAll();

                // TODO: If we do use CategoryWithElasticQuery as per above, then we will be able to pass this directly, either
                // via another overload or variance.
                IDictionary<string, List<CategorisationResult>> listOfCategorisationResults;


                try
                {
                   listOfCategorisationResults =  _categoriserRepository.FindRelevantCategoriesForDocuments(iaViews1, sourceCategories, retrieveScoreForAllRelevantCategories1);

                    return listOfCategorisationResults;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        public Task<IList<CategorisationResult>> CategoriseSingle(string docReference, IList<Category> cachedCategories = null)
        {
            CheckForValidDocReference(docReference);

            return CategoriseSingleAsync(docReference, cachedCategories);

            async Task<IList<CategorisationResult>> CategoriseSingleAsync(string docReference1, IList<Category> cachedCategories1 = null)
            {
                InformationAssetView iaView = await _iaViewRepository.SearchDocByDocReference(docReference);
                return await CategoriseSingle(iaView, cachedCategories);
            }
        }

        public Task<IList<CategorisationResult>> CategoriseSingle(string docReference)
        {
            CheckForValidDocReference(docReference);
            return CategoriseSingle(docReference, null);
        }

        public async Task<IList<CategorisationResult>> CategoriseSingle(InformationAssetView iaView, IList<Category> cachedCategories = null)
        {
            CheckForValidDocReference(iaView.CatDocRef);
            CheckStagingQueue();

            try
            {
                IList<CategorisationResult> listOfCategorisationResults = await TestCategoriseSingle(iaView, false, cachedCategories);
                SaveResultsToIntermUpdateQueue(iaView, listOfCategorisationResults);
                return listOfCategorisationResults;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void SaveResultsToIntermUpdateQueue(InformationAssetView iaView, IList<CategorisationResult> listOfCategorisationResults)
        {
            IaidWithCategories iaidWithCategories = new IaidWithCategories(iaView.DocReference, listOfCategorisationResults.Select(r => r.CategoryID).ToList());
            _stagingQueueSender.Enqueue(iaidWithCategories, _token);
        }

        private void SaveResultsToIntermUpdateQueue(string iaid, IList<CategorisationResult> listOfCategorisationResults)
        {
            IaidWithCategories iaidWithCategories = new IaidWithCategories(iaid, listOfCategorisationResults.Select(r => r.CategoryID).ToList());
            _stagingQueueSender.Enqueue(iaidWithCategories, _token);
        }

        public IAViewUpdate FindLastIAViewUpdate()
        {
            //TODO: This appears to be querying Mongo and getting back an IAIDs
            // with categories.  But we don't appear to store the IAID- Category relationship
            // anywhere in Mongo.
            // (Calls into Java class IAViewUpdateRepositoryImpl)
            throw new NotImplementedException();
        }

        public IList<IAViewUpdate> GetNewCategorisedDocumentsAfterDocumentAndUpToNSecondsInPast(IAViewUpdate afterIAViewUpdate, int nbOfSecondsInPast, int limit)
        {
            //TODO: This appears to be querying Mongo and getting back a list of IAIDs
            // with categories.  But we don't appear to store the IAID - Category relationship
            // anywhere in Mongo.
            // (Calls into Java class IAViewUpdateRepositoryImpl)
            throw new NotImplementedException();
        }

        public IList<IAViewUpdate> GetNewCategorisedDocumentsFromDateToNSecondsInPast(DateTime date, int nbOfSecondsInPast, int limit)
        {
            // See comment above.
            throw new NotImplementedException();
        }

        public void RefreshTaxonomyIndex()
        {
            // TODO: May not need this - it calls into org.apache.lucene.search code to refresh
            // the index to ensure a document is visible on the in memory index after indexing
            // but before we run categorisation.  This is because unlike e.g. SQL server, indexing 
            // does not make the documnent immediately visible;
            // Currently we're doing a fresh after indexing but before running the categorisation query
            // This may suffice but need to monitor performance.
            // See IndexDocument method in ElasticConnection.cs
            //
            // In the Java app this is called only from the batch application, once on starting the All Docs
            // epic and again on processing the message queue for the daily updates.  
            // But the actual indexing into the in mem index
            // doesn't occur until after this call and then it's not being called for each indivivual categorise
            // request, so not clear what's going on..
            //
            // Possibly ths is a global setting whcih ensures docs are always immediately visible, so we don't need
            // to call refresh on each one indiviually
            throw new NotImplementedException();
        }

        private void SaveTaxonomiesForInformationAsset(InformationAssetView iaAsset  )
        {

        }

        private void CheckForValidDocReference(string docReference)
        {
            //TODO: Also check regex for expected format?
            if (String.IsNullOrWhiteSpace(docReference))
            {
                throw new TaxonomyException(TaxonomyErrorType.MISSING_OR_INVALID_DOC_REFERENCE, "Missing or null documenent reference" + (!String.IsNullOrWhiteSpace(docReference) ? docReference : String.Empty));
            }
        }

        private void CheckStagingQueue()
        {
            if(_stagingQueueSender == null)
            {
                throw new TaxonomyException("Cannot categorise this request as Categoriser has no staging queue provided");
            }
        }

        public async Task<IDictionary<string, List<CategorisationResult>>> CategoriseMultiple(string[] docReferences, IList<Category> cachedCategories)
        {

            foreach (string s in docReferences)
            {
                CheckForValidDocReference(s);
            }
            
            IList<Category> sourceCategories = cachedCategories ?? await _categoryRepository.FindAll();

            try
            {
                //TODO: Possibly fetch multiple assets at once in batch operation.
                DateTime fetchStart = DateTime.Now;

                IList<InformationAssetView> assets1 = await _iaViewRepository.SearchDocByMultipleDocReferences(docReferences);

                DateTime fetchComplete = DateTime.Now;
                TimeSpan fetchTime = fetchComplete - fetchStart;


                Console.WriteLine($"Fetched {assets1.Count} from Elastic Search for categorising in {Math.Round(fetchTime.TotalSeconds, 5)} seconds");

                IDictionary<string, List<CategorisationResult>> listOfCategorisationResultsForAllAssets = await TestCategoriseMultiple(assets1.ToArray(), false, sourceCategories);
                foreach (var listOfCategorisationResults in listOfCategorisationResultsForAllAssets)
                {
                    SaveResultsToIntermUpdateQueue(listOfCategorisationResults.Key, listOfCategorisationResults.Value); 
                }
                return listOfCategorisationResultsForAllAssets;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
