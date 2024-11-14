using lna = Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.Domain.Repository.Lucene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;

namespace NationalArchives.Taxonomy.Common.UnitTests.Lucene.Analyzer
{
    [TestClass]
    public class TaxonomyGeneralAnalyzerTest
    {
        Dictionary<string, string> _synonymFilterFactoryArgs;
        Dictionary<string, string> _wordDelimiterFilterArgs;
        Dictionary<string, string> _stopFilterArgs;

        SynonymFilterFactory _synonymFilterFactory;
        WordDelimiterFilterFactory _wordDelimiterFilterFactory;
        StopFilterFactory _stopFilterFactory;

        
        ILogger<lna.Analyzer> _logger;

        [TestInitialize]
        public void Init()
        {
            // IAViewTextGenAnalyser
            _synonymFilterFactoryArgs = new Dictionary<string, string>()
            {
                { "synonyms", "synonyms.txt" },
                { "expand", "true" },
                { "ignoreCase", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            _synonymFilterFactory = new SynonymFilterFactory(_synonymFilterFactoryArgs);

            _wordDelimiterFilterArgs = new Dictionary<string, string>()
            {
                {"preserveOriginal", "1" },
                { "generateWordParts", "1" },
                { "catenateWords", "1" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            _wordDelimiterFilterFactory = new WordDelimiterFilterFactory(_wordDelimiterFilterArgs);

            _stopFilterArgs = new Dictionary<string, string>()
            {
                { "words", "stopwords.txt" },
                { "enablePositionIncrements", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            _stopFilterFactory = new StopFilterFactory(_stopFilterArgs);
        }

        [TestMethod]
        public void Test_IAViewTextCasNoPuncAnalyser()
        {
            lna.Analyzer analyser = new IAViewTextCasNoPuncAnalyser(_synonymFilterFactory, _wordDelimiterFilterFactory, AnalyzerType.QUERY, _logger);
            StringReader reader = new StringReader("archiveS tEst MELODY");
            //TokenStream stream = analyser.CreateComponents("test", reader);
        }
    }
}
