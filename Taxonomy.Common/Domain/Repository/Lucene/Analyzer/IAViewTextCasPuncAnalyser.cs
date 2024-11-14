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
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using System.IO;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    /**
* taxonomy search no stemming case sensitive punctuation retained
* 
* @author jcharlet
*
*/
    public sealed class IAViewTextCasPuncAnalyser : Analyzer
    {
        private readonly ILogger<Analyzer> _logger;

        private readonly StopFilterFactory _stopFilterFactory;
        private readonly SynonymFilterFactory _synonymFilterFactory;
        private readonly AnalyzerType _analyzerType;
        private int _positionIncrementGap;

        private readonly LuceneVersion _luceneVersion = LuceneVersion.LUCENE_CURRENT;

        /**
         * Creates a new {@link WhitespaceAnalyzer}
         * 
         */
        public IAViewTextCasPuncAnalyser(StopFilterFactory stopFilterFactory, SynonymFilterFactory synonymFilterFactory,
            AnalyzerType analyzerType, ILogger<Analyzer> logger)
        {
            _stopFilterFactory = stopFilterFactory;
            _synonymFilterFactory = synonymFilterFactory;
            _analyzerType = analyzerType;
            _logger = logger;
        }

        
        public override int GetPositionIncrementGap(string fieldName)
        {
            return _positionIncrementGap;
        }

        public void SetPositionIncrementGap(int positionIncrementGap)
        {
            _positionIncrementGap = positionIncrementGap;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new WhitespaceTokenizer(_luceneVersion, reader);

            TokenStream result = null;

            if (_stopFilterFactory != null)
            {
                result = this._stopFilterFactory.Create(source);
            }
            else
            {
                _logger.LogWarning(".createComponents: stopFilter disabled");
            }

            if (AnalyzerType.QUERY.Equals(_analyzerType))
            {
                if (_synonymFilterFactory != null)
                {
                    result = this._synonymFilterFactory.Create(result == null ? source : result);
                }
                else
                {
                    _logger.LogWarning(".createComponents: synonymFilter disabled");
                }
            }
            return new TokenStreamComponents(source, result == null ? source : result);
        }
    } 
}