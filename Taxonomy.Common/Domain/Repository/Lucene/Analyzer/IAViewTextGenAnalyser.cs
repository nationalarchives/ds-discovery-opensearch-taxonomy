/** 
 * Copyright (c) 2019, The National Archives
 * http://www.nationalarchives.gov.uk 
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public 
 * License, v. 2.0. If a copy of the MPL was not distributed with this 
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using System.IO;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    /**
 * General search no stemming punctuation removed originals preserved
 * 
 * @author jcharlet, boreilly
 *
 */
    public sealed class IAViewTextGenAnalyser : Analyzer
    {

        private readonly ILogger<Analyzer> _logger;

        private WordDelimiterFilterFactory _wordDelimiterFilterFactory;
        private int _positionIncrementGap;
        private readonly SynonymFilterFactory _synonymFilterFactory;
        private AnalyzerType _analyzerType;

        private readonly LuceneVersion _luceneVersion = LuceneVersion.LUCENE_CURRENT;

        /// <summary>
        /// Creates a new tokenizer
        /// </summary>
        /// <param name="synonymFilterFactory"></param>
        /// <param name="wordDelimiterFilterFactory"></param>
        /// <param name="analyzerType"></param>
        public IAViewTextGenAnalyser(SynonymFilterFactory synonymFilterFactory,
                                   WordDelimiterFilterFactory wordDelimiterFilterFactory, AnalyzerType analyzerType,
                                   ILogger<Analyzer> logger)
        {
            _synonymFilterFactory = synonymFilterFactory;
            _wordDelimiterFilterFactory = wordDelimiterFilterFactory;
            _analyzerType = analyzerType;
            _logger = logger;
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return _positionIncrementGap;
        }

        public void GetPositionIncrementGap(int positionIncrementGap)
        {
            _positionIncrementGap = positionIncrementGap;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new ClassicTokenizer(_luceneVersion, reader);

            TokenStream result = null;

            if (AnalyzerType.QUERY.Equals(_analyzerType))
            {
                if (_synonymFilterFactory != null)
                {
                    result = this._synonymFilterFactory.Create(source);
                }
                else
                {
                    _logger.LogWarning(".createComponents: synonymFilter disabled");
                }
            }
            result = this._wordDelimiterFilterFactory.Create(result == null ? source : result);
            result = new EnglishPossessiveFilter(_luceneVersion, result);
            result = new LowerCaseFilter(_luceneVersion, result);
            result = new ASCIIFoldingFilter(result);

            return new TokenStreamComponents(source, result);
        }
    }
}
