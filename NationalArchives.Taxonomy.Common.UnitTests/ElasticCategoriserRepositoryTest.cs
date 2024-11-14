using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace NationalArchives.Taxonomy.Common.UnitTests
{
    /// <summary>
    /// N.B. Ignored tests fail when run with the other tests even though they pass when run individually.
    /// This appears to be a Lucene.Net issue when running consecutive queries with different analysers, 
    /// even though each run should be isolated from the others.
    /// </summary>
    [TestClass]
    public class InMemoryCategoriserRepositoryTest
    {
        IList<Category> _listOfCategories;
        string[] _queryFields;

        private ILogger<Analyzer> _logger;

        [TestInitialize]
        public void Init()
        {
            _listOfCategories = PopulateCategories();
            _queryFields = new string[] { "TITLE", "DESCRIPTION", "CONTEXT", "CATALOGUE_REFERENCE", "COVERING_DATES" };
        }

        [Ignore]
        [TestMethod]
        public void FindRelevantCategoriesForDocument_TextGen()
        {
            string defaultField = InformationAssetViewFields.text.ToString();
            var analyzer = GetAnalyzer(AnalyzerKind.IAViewTextGen);

            var luceneHelperTools = new LuceneHelperTools(defaultField, analyzer, _queryFields);

            var openSearchCategoryRepository = new InMemoryCategoriserRepository(iaViewIndexAnalyser :analyzer, luceneHelperTools: luceneHelperTools, logger: null);

            InformationAssetView iaView = GetInformationAssetView();

            IList<CategorisationResult> categorisationResults = openSearchCategoryRepository.FindRelevantCategoriesForDocument(iaView, PopulateCategories());

            Assert.IsNotNull(categorisationResults);
            Trace.WriteLine(categorisationResults.Count + " caetgories found to match.");
            Assert.IsFalse(categorisationResults.Count == 0);

            Assert.IsTrue(categorisationResults.Select(r => r.CategoryName).ToArray().Contains("Air Force"));
        }

        [TestMethod]
        public void FindRelevantCategoriesForDocument_TextNoCasNoPunc()
        {
            string defaultField = InformationAssetViewFields.textnocasnopunc.ToString();
            var analyzer = GetAnalyzer(AnalyzerKind.IAViewTextNoCasNoPunc);

            var luceneHelperTools = new LuceneHelperTools(defaultField, analyzer, _queryFields);

            var openSearchCategoryRepository = new InMemoryCategoriserRepository( analyzer, luceneHelperTools: luceneHelperTools, logger: null);

            InformationAssetView iaView = GetInformationAssetView();

            IList<CategorisationResult> categorisationResults = openSearchCategoryRepository.FindRelevantCategoriesForDocument(iaView, PopulateCategories());

            Assert.IsNotNull(categorisationResults);
            Trace.WriteLine(categorisationResults.Count + " categories found to match.");
            Assert.IsFalse(categorisationResults.Count == 0);

            Assert.IsTrue(categorisationResults.Select(r => r.CategoryName).ToArray().Contains("Air Force"));

        }

        [Ignore]
        [TestMethod]
        public void FindRelevantCategoriesForDocument_TextCasNoPunc()
        {
            string defaultField = InformationAssetViewFields.textcasnopunc.ToString();
            var analyzer = GetAnalyzer(AnalyzerKind.IAViewTextCasNoPunc);


            var luceneHelperTools = new LuceneHelperTools(defaultField, analyzer, _queryFields);

            var openSearchCategoryRepository = new InMemoryCategoriserRepository(iaViewIndexAnalyser: analyzer, luceneHelperTools: luceneHelperTools, logger: null);

            InformationAssetView iaView = GetInformationAssetView();

            IList<CategorisationResult> categorisationResults = openSearchCategoryRepository.FindRelevantCategoriesForDocument(iaView, _listOfCategories);

            Assert.IsNotNull(categorisationResults);
            Assert.IsFalse(categorisationResults.Count == 0);

            Assert.IsTrue(categorisationResults.Select(r => r.CategoryName).ToArray().Contains("Air Force"));
        }



        [TestMethod]
        [ExpectedException(typeof(TaxonomyException))]  
        public void FindRelevantCategoriesForDocument_TextCasPunc()
        {
            string defaultField = "textnocasnopunc";
            var analyzer = GetAnalyzer(AnalyzerKind.IAViewTextCasPunc);

            var luceneHelperTools = new LuceneHelperTools(defaultField, analyzer, _queryFields);

            var openSearchCategoryRepository = new InMemoryCategoriserRepository(iaViewIndexAnalyser: analyzer, luceneHelperTools: luceneHelperTools, logger: null);

            InformationAssetView iaView = GetInformationAssetView();

            IList<CategorisationResult> categorisationResults = openSearchCategoryRepository.FindRelevantCategoriesForDocument(iaView, _listOfCategories);

            Assert.IsNotNull(categorisationResults);
            Assert.IsFalse(categorisationResults.Count == 0);

            Assert.IsTrue(categorisationResults.Select(r => r.CategoryName).ToArray().Contains("Air Force"));

        }

        private IList<Category> PopulateCategories()
        {

            //string fileName = @"C:\Temp\taxonomy_fiddler.json";
            string fileName = Path.Combine(Environment.CurrentDirectory, "resources", "elastic_taxonomy_fiddler.json");

            var listOfCategories = new List<Category>();

            using (StreamReader reader = File.OpenText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();

                string json = reader.ReadToEnd();
                dynamic files = JsonConvert.DeserializeObject(json);
                XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(json, "hits");
                XmlNodeList sourceNodes = doc.SelectNodes("descendant::_source");

                foreach (XmlNode node in sourceNodes)
                {
                    string id = node.SelectSingleNode("id").InnerText;
                    string query = node.SelectSingleNode("query_text").InnerText;
                    string title = node.SelectSingleNode("title").InnerText;
                    bool isLocked = Convert.ToBoolean(node.SelectSingleNode("locked").InnerText);
                    double score = Convert.ToDouble(node.SelectSingleNode("sc").InnerText);

                    var category = new Category() { Id = id, Title = title, Query = query, Lock = isLocked, Score = score };
                    listOfCategories.Add(category);
                }
            }
            return listOfCategories;
        }

        private InformationAssetView GetInformationAssetView()
        {
            //C508096
            var random = new Random(Guid.NewGuid().GetHashCode());
            InformationAssetView iaView = new InformationAssetView();
            iaView.CatDocRef = "AIR 37/177";
            iaView.ContextDescription = "Air Ministry: Allied Expeditionary Air Force, later Supreme Headquarters Allied Expeditionary Force (Air), and 2nd Tactical Air Force: Registered Files and Reports.";
            iaView.CoveringDates = "1942";
            iaView.Description = "CHIEF OF STAFF, SUPREME ALLIED COMMAND: Operation \"Round-up\": operational organisation of RAF.";
            iaView.DocReference = "C" + random.Next();
            iaView.Title = "CHIEF OF STAFF, SUPREME ALLIED COMMAND: Operation \"Round-up\": operational organisation of RAF";

            return iaView;
        }

        private Analyzer GetAnalyzer(AnalyzerKind analyserType)
        {
            // IAViewTextGenAnalyser
            var synonymFilterFactoryArgs = new Dictionary<string, string>()
            {
                { "synonyms", "synonyms.txt" },
                { "expand", "true" },
                { "ignoreCase", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var synonymFilterFactory = new SynonymFilterFactory(synonymFilterFactoryArgs);

            var wordDelimiterFilterArgs = new Dictionary<string, string>()
            {
                {"preserveOriginal", "1" },
                { "generateWordParts", "1" },
                { "catenateWords", "1" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var wordDelimiterFilterFactory = new WordDelimiterFilterFactory(wordDelimiterFilterArgs);

            var stopFilterArgs = new Dictionary<string, string>()
            {
                { "words", "stopwords.txt" },
                { "enablePositionIncrements", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var stopFilterFactory = new StopFilterFactory(stopFilterArgs);

            Analyzer analyser;

            switch (analyserType)
            {
                case AnalyzerKind.IAViewTextNoCasNoPunc:
                    analyser = new IAViewTextNoCasNoPuncAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, _logger);
                    break;
                case AnalyzerKind.IAViewTextCasNoPunc:
                    analyser = new IAViewTextCasNoPuncAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, _logger);
                    break;
                case AnalyzerKind.IAViewTextCasPunc:
                    analyser = new IAViewTextCasPuncAnalyser(stopFilterFactory, synonymFilterFactory, AnalyzerType.INDEX, _logger);
                    break;
                case AnalyzerKind.IAViewTextGen:
                    analyser = new IAViewTextGenAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, _logger);
                    break;
                default:
                    analyser = new StandardAnalyzer(LuceneVersion.LUCENE_CURRENT);
                    break;
            }

            return analyser;
        }

        private enum AnalyzerKind
        {
            Standard,
            IAViewTextNoCasNoPunc,
            IAViewTextCasNoPunc,
            IAViewTextCasPunc,
            IAViewTextGen
        }
    }
}
