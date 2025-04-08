
using ln = Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using lns = Lucene.Net.Search;
using Lucene.Net.Store;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using lnu = Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    public class InMemoryCategoriserRepository : ICategoriserRepository
    {

        private readonly Analyzer _iaViewIndexAnalyser;
        private readonly lnu.LuceneVersion _luceneVersion = lnu.LuceneVersion.LUCENE_CURRENT;
        private readonly LuceneHelperTools _luceneHelperTools;
        private uint _batchSize;
        private ILogger<ICategoriserRepository> _logger;

        private static IList<CategoryWithLuceneQuery> _categoriesWithLuceneQueries;

        public InMemoryCategoriserRepository(Analyzer iaViewIndexAnalyser, LuceneHelperTools luceneHelperTools, ILogger<ICategoriserRepository> logger, uint batchSize = 1000)
        {
            if(iaViewIndexAnalyser == null || luceneHelperTools == null)
            {
                throw new Exception("Analyser and Lucene Helper tools are required!");
            }

            _iaViewIndexAnalyser = iaViewIndexAnalyser;
            _luceneHelperTools = luceneHelperTools;
            _batchSize = batchSize;  // only used for CategorisingMultipleDocuments in a single in-memory operation
            _logger = logger;
        }

        public IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IEnumerable<Category> categoriesToCheck, bool includeScores = false)
        {
            IList<CategoryWithLuceneQuery> categoriesWithQuery = _categoriesWithLuceneQueries ?? GetCategoriesWithParsedQueries(categoriesToCheck);
            return FindRelevantCategoriesForDocument(iaView, categoriesWithQuery, includeScores);
        }


        /// <summary>
        /// TODO: Possibly just query on the category, if we can be sure that the index only has the one
        /// document./  This is what the Java code does.  Check performance and review.
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="categoryWithIdQuery"></param>
        /// <returns></returns>
        private bool IsMatch(lns.IndexSearcher searcher,  lns.BooleanQuery booleanQuery)
        {
            var hits = searcher.Search(booleanQuery, 1);
            return (hits.TotalHits > 0);
        }

        private bool IsMatch(lns.IndexSearcher searcher, lns.Query query)
        {
            var hits = searcher.Search(query, 1);
            return (hits.TotalHits > 0);
        }

        private bool IsMatch(lns.IndexSearcher searcher, lns.BooleanQuery booleanQuery, out double? score)
        {
            var hits = searcher.Search(booleanQuery, (int)_batchSize).ScoreDocs;
            if(hits.Count() > 0)
            {
                score = hits[0].Score;
                return true;
            }
            else
            {
                score = null;
                return false;
            }
        }

        private string[] MatchingIaids(lns.IndexSearcher searcher, lns.Query query)
        {
            lns.ScoreDoc[] scoreDocs = searcher.Search(query, (int)_batchSize).ScoreDocs;

            if(scoreDocs.Count() == 0)
            {
                return new string[0];
            }
            else
            {
                string[] matchingIaids = new string[scoreDocs.Count()];
                for (int i = 0; i < scoreDocs.Length; i++)
                {
                    Document document = searcher.Doc(scoreDocs[i].Doc);
                    matchingIaids[i] = document.Get("id");
                }
                return matchingIaids;
            }
        }

        private List<Tuple<string, double>> MatchingIaidsWithScores(lns.IndexSearcher searcher, lns.Query query)
        {
            lns.ScoreDoc[] scoreDocs = searcher.Search(query, 1).ScoreDocs;

            var results = new List<Tuple<string, double>>();

            for (int i = 0; i < scoreDocs.Length; i++)
            {
                Document document = searcher.Doc(scoreDocs[i].Doc);
                string iaid = document.Get("id");
                double score = scoreDocs[i].Score;
                results.Add(new Tuple<string, double>(iaid, score));
            }
            return results;
            
        }


        private bool IsMatch(lns.IndexSearcher searcher, lns.Query query, out double? score)
        {

            var hits = searcher.Search(query, 1).ScoreDocs;
            if (hits.Count() > 0)
            {
                score = hits[0].Score;
                return true;
            }
            else
            {
                score = null;
                return false;
            }
        }

        public IList<CategorisationResult> FindRelevantCategoriesForDocument(InformationAssetView iaView, IList<CategoryWithLuceneQuery> sourceCategories, bool includeScores = false)
        {
            IList<CategorisationResult> categorisationResults = new List<CategorisationResult>();

            lns.SearcherManager searcherManager = null;   // org.apache.lucene.search;
            lns.IndexSearcher searcher = null;   //org.apache.lucene.store;
            RAMDirectory ramDirectory = null;  //org.apache.lucene.store;

            try
            {
                ramDirectory = CreateRamDirectoryForDocument(iaView);  //org.apache.lucene.store;
                searcherManager = new lns.SearcherManager(ramDirectory, null);   //org.apache.lucene.search;
                searcher = searcherManager.Acquire(); //org.apache.lucene.store;

                // Run all the categories against the in memory doc?  But why are we starting out with
                    var luceneHelperTools =  _luceneHelperTools ?? new LuceneHelperTools();

                Dictionary<string, lns.BooleanQuery> categoryIdsWithBooleanQueries =
                    sourceCategories.ToDictionary(c => c.Id, c => luceneHelperTools.BuildBooleanQuery(iaView, c.ParsedQuery));

                Dictionary<string, lns.Query> categoryQueries = sourceCategories.ToDictionary(c => c.Id, c => c.ParsedQuery);

                var queriesToUse = new Dictionary<string, lns.Query>();

                foreach(var item in categoryIdsWithBooleanQueries)
                {
                    queriesToUse.Add(item.Key, item.Value);
                }

                if (includeScores)
                {
                    foreach(var query in queriesToUse)
                    {
                        double? score;
                        bool isMatch = IsMatch(searcher, query.Value, out score);
                        if (isMatch)
                        {
                            Category category = sourceCategories.Single(c => c.Id == query.Key).Category;
                            categorisationResults.Add(new CategorisationResult(category, score));
                        }
                    }
                }
                else
                {

                    var matchingQueries = queriesToUse.Where(bq => IsMatch(searcher, bq.Value));
                    matchingQueries.ToList().ForEach(q =>
                        {
                            Category category = sourceCategories.Single(c => c.Id == q.Key).Category;
                            categorisationResults.Add(new CategorisationResult(category, null));
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                throw new TaxonomyException(TaxonomyErrorType.LUCENE_IO_EXCEPTION, ex);
            }
            finally
            {
                LuceneHelperTools.ReleaseSearcherManagerQuietly(searcherManager, searcher);
                LuceneHelperTools.DisposeQuietly(ramDirectory);
            }

            Trace.WriteLine(categorisationResults.Count + " matching categories.");
            Debug.Print(categorisationResults.Count + " matching categories.");
		    return categorisationResults;
        }

        private Document GetLuceneDocumentFromIaView(InformationAssetView iaView, bool storeIdField = false)
        {

            Document document = new Document(); // org.apache.lucene.document;
            var listOfFields = new List<Field>();

            listOfFields.Add(new StringField("id", iaView.DocReference.ToLowerInvariant(), storeIdField ? Field.Store.YES : Field.Store.NO));

            switch(_iaViewIndexAnalyser)
            {
                case IAViewTextNoCasNoPuncAnalyser tncnp:
                    listOfFields.AddRange(GetCopyIAViewFieldsToTaxonomyField(iaView, OpenSearchFieldConstants.TEXT_NO_CAS_NO_PUNC));
                    break;
                case IAViewTextCasNoPuncAnalyser tcnp:
                    listOfFields.AddRange(GetCopyIAViewFieldsToTaxonomyField(iaView, OpenSearchFieldConstants.TEXT_CAS_NO_PUNC));
                    break;
                case IAViewTextCasPuncAnalyser tcp:
                    listOfFields.AddRange(GetCopyIAViewFieldsToTaxonomyField(iaView, OpenSearchFieldConstants.TEXT_CAS_PUNC));
                    break;
                case IAViewTextGenAnalyser tg:
                    listOfFields.AddRange(GetCopyIAViewFieldsToTaxonomyField(iaView, OpenSearchFieldConstants.TEXT_GEN));
                    break;
                default:
                    listOfFields.AddRange(GetListOfUnmodifiedFieldsFromIAView(iaView));
                    break;
            }

            AddFieldsToLuceneDocument(document, listOfFields);

            return document;
        }

        private void AddFieldsToLuceneDocument(Document document, IList<Field> listOfFields)
        {

            foreach (Field field in listOfFields)
            {
                document.Add(field);
            }
        }

        private List<Field> GetCopyIAViewFieldsToTaxonomyField(InformationAssetView iaView, string targetCommonIndexField)
        {

            List<Field> listOfFields = new List<Field>();

            string[] queryFields =_luceneHelperTools.QueryFields.ToArray();



            listOfFields.Add(new TextField(targetCommonIndexField, iaView.Description, Field.Store.NO));

            if (!String.IsNullOrWhiteSpace(iaView.Title) && queryFields.Contains("title", StringComparer.OrdinalIgnoreCase))
            {
                listOfFields.Add(new TextField(targetCommonIndexField, iaView.Title, Field.Store.NO));
            }
            if (!String.IsNullOrWhiteSpace(iaView.ContextDescription) && queryFields.Contains(OpenSearchFieldConstants.CONTEXT, StringComparer.OrdinalIgnoreCase))
            {
                listOfFields.Add(new TextField(targetCommonIndexField, iaView.ContextDescription, Field.Store.NO));
            }
            if (iaView.CorpBodys != null  && iaView.CorpBodys.Length > 0 && queryFields.Contains(OpenSearchFieldConstants.CORPORATE_BODY, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string corpBody in iaView.CorpBodys)
                {
                    listOfFields.Add(new TextField(targetCommonIndexField, corpBody, Field.Store.NO));
                }
            }
            if (iaView.Subjects != null  && iaView.Subjects.Length > 0 && queryFields.Contains(OpenSearchFieldConstants.SUBJECT, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string subject in iaView.Subjects)
                {
                    listOfFields.Add(new TextField(targetCommonIndexField, subject, Field.Store.NO));
                }
            }

            if (iaView.Person_FullName != null && iaView.Person_FullName.Length > 0 && queryFields.Contains(OpenSearchFieldConstants.PERSON, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string person in iaView.Person_FullName)
                {
                    listOfFields.Add(new TextField(targetCommonIndexField, person, Field.Store.NO));
                }
            }
            if (iaView.Place_Name != null && iaView.Place_Name.Length > 0 && queryFields.Contains(OpenSearchFieldConstants.PLACE_NAME, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string place in iaView.Place_Name)
                {
                    listOfFields.Add(new TextField(targetCommonIndexField, place, Field.Store.NO));
                }
            }
            if (!String.IsNullOrWhiteSpace(iaView.CatDocRef) && queryFields.Contains(OpenSearchFieldConstants.CATALOGUE_REFERENCE, StringComparer.OrdinalIgnoreCase))
            {
                listOfFields.Add(new TextField(targetCommonIndexField, iaView.CatDocRef, Field.Store.NO));
            }
            return listOfFields;
        }

        private List<Field> GetListOfUnmodifiedFieldsFromIAView(InformationAssetView iaView)
        {
            List<Field> listOfUnmodifiedFields = new List<Field>();
            if (iaView.CatDocRef != null)
            {
                listOfUnmodifiedFields.Add(new TextField(InformationAssetViewFields.CATDOCREF.ToString(), iaView.CatDocRef, Field.Store.NO));
            }
            if (iaView.Description != null)
            {
                listOfUnmodifiedFields.Add(new TextField(InformationAssetViewFields.DESCRIPTION.ToString(), iaView.Description, Field.Store.NO));
            }
            if (iaView.Title != null)
            {
                listOfUnmodifiedFields.Add(new TextField(InformationAssetViewFields.TITLE.ToString(), iaView.Title, Field.Store.NO));
            }
            if (iaView.Source != null)
            {
                listOfUnmodifiedFields.Add(new Int32Field(InformationAssetViewFields.SOURCE.ToString(), Int32.Parse(iaView.Source), Field.Store.NO));
            }
            return listOfUnmodifiedFields;
        }

        private IList<CategoryWithLuceneQuery> GetCategoriesWithParsedQueries(IEnumerable<Category> categories)
        {

            if(_categoriesWithLuceneQueries != null)
            {
                return _categoriesWithLuceneQueries;
            }

            var categoriesWithluceneQueries = new List<CategoryWithLuceneQuery>();

            foreach (Category c in categories)
            {
                try
                {
                    var luceneQuery = _luceneHelperTools.BuildSearchQuery(c.Query);
                    var categoryWithLuceneQuery = new CategoryWithLuceneQuery(c, luceneQuery);
                    categoriesWithluceneQueries.Add(categoryWithLuceneQuery);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to parse query: " + c.Id);
                }
            }
            _categoriesWithLuceneQueries = categoriesWithluceneQueries;
            return categoriesWithluceneQueries;
        }

        public IDictionary<string, List<CategorisationResult>> FindRelevantCategoriesForDocuments(InformationAssetView[] iaViews, IEnumerable<Category> sourceCategories, bool includeScores = false)
        {
            IList<CategoryWithLuceneQuery> categoriesWithLuceneQueries = GetCategoriesWithParsedQueries(sourceCategories);
            return FindRelevantCategoriesForDocuments(iaViews, categoriesWithLuceneQueries, includeScores);
        }

        private IDictionary<string, List<CategorisationResult>> FindRelevantCategoriesForDocuments(InformationAssetView[] iaViews, IEnumerable<CategoryWithLuceneQuery> sourceCategories, bool includeScores = false)
        {

            var categorisationResults = new Dictionary<string, List<CategorisationResult>>(StringComparer.OrdinalIgnoreCase);

            //Seed the dictionary so we have a key for each IAID that could potentially match 1 or more categories.
            foreach(InformationAssetView iaView in iaViews)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(iaView?.DocReference))
                    {
                        _logger?.LogError($"Error in FindRelevantCategoriesForDocuments - null or missing IAID reference in document retrieved from Elastic Search.");
                    }
                    else
                    {
                        categorisationResults.Add(iaView.DocReference, new List<CategorisationResult>()); 
                    }
                }
                catch (ArgumentException aex)
                {
                    _logger?.LogError($"Error in FindRelevantCategoriesForDocuments - unable to add IAID {iaView.DocReference} to the collection of IAIDs to be categorised.  Possible duplicate key.");
                }
            }

            lns.SearcherManager searcherManager = null;   // org.apache.lucene.search;
            lns.IndexSearcher searcher = null;   //org.apache.lucene.store;

            ln.Store.Directory luceneDirectory = CreateRamDirectoryForDocuments(iaViews.Where(a => a != null).ToArray());  //org.apache.lucene.store;

            searcherManager = new lns.SearcherManager(luceneDirectory, null);   //org.apache.lucene.search;
            searcher = searcherManager.Acquire(); //org.apache.lucene.store;

            var luceneHelperTools = _luceneHelperTools ?? new LuceneHelperTools();

            Dictionary<string, lns.Query> taxonomyCategoryQueries = sourceCategories.ToDictionary(c => c.Id, c => c.ParsedQuery);

            try
            {
                if (includeScores)
                {
                    foreach (var taxonomyCategoryQuery in taxonomyCategoryQueries)
                    {
                        double? score;
                        bool isMatch = IsMatch(searcher, taxonomyCategoryQuery.Value, out score);
                        List<Tuple<string, double>> results = MatchingIaidsWithScores(searcher, taxonomyCategoryQuery.Value);

                        Category category = sourceCategories.Single(c => c.Id == taxonomyCategoryQuery.Key).Category;

                        foreach (Tuple<string, double> result in results)
                        {
                            categorisationResults[result.Item1].Add(new CategorisationResult(category, result.Item2));
                        }
                    }
                }
                else
                {
                    int counter = 0;
                    foreach (var taxonomyCategoryQuery in taxonomyCategoryQueries)
                    {
                        Category category = sourceCategories.Single(c => c.Id == taxonomyCategoryQuery.Key).Category;
                        counter++;
                        string[] matchingIaids = MatchingIaids(searcher, taxonomyCategoryQuery.Value);

                        foreach (string iaid in matchingIaids)
                        {
                            categorisationResults[iaid].Add(new CategorisationResult(category, null));
                        }
                    }
                }

                return categorisationResults;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                LuceneHelperTools.ReleaseSearcherManagerQuietly(searcherManager, searcher);
                LuceneHelperTools.DisposeQuietly(luceneDirectory);
            }

        }

        private string[] GetDocReferencesForBatch(InformationAssetView[] iaViews)
        {
            return iaViews.Select(i => i.DocReference).Where(r => !String.IsNullOrEmpty(r)).ToArray();
        }

        private RAMDirectory CreateRamDirectoryForDocument(InformationAssetView iaView)
        {
            RAMDirectory ramDirectory = new RAMDirectory();  //org.apache.lucene.store

            // Make an writer to create the index
            var indexWriterConfig = new IndexWriterConfig(_luceneVersion, _iaViewIndexAnalyser);

            using (IndexWriter writer = new IndexWriter(ramDirectory, indexWriterConfig))
            {
                // Add some Document objects containing quotes
                Document document = GetLuceneDocumentFromIaView(iaView);
                writer.AddDocument(document);
            }

            // Optimize and close the writer to finish building the index
            return ramDirectory;

        }

        private RAMDirectory CreateRamDirectoryForDocuments(InformationAssetView[] iaViews)
        {
            RAMDirectory ramDirectory = new RAMDirectory();
            ramDirectory.SetLockFactory(NoLockFactory.GetNoLockFactory());

            // Make an writer to create the index
            var indexWriterConfig = new IndexWriterConfig(_luceneVersion, _iaViewIndexAnalyser);


            using (IndexWriter writer = new IndexWriter(ramDirectory, indexWriterConfig))
            {
                foreach (InformationAssetView iaView in iaViews)
                {
                    // Add some Document objects containing quotes
                    Document document = GetLuceneDocumentFromIaView(iaView,storeIdField: true);
                    writer.AddDocument(document);
                }
            }

            // Optimize and close the writer to finish building the index
            return ramDirectory;

        }
    }
}

