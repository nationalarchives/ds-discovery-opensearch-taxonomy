using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;
using System.Text.RegularExpressions;
using lnu = Lucene.Net.Util;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    /**
    * Parser for Taxonomy<br/>
    * by default Lucene does not handle numeric values. This class handles Source
    * intField in term queries (SOURCE:100) and numeric rangse (SOURCE:[100 TO 200]
    * 
    * @author jcharlet
    *
    * @author Brian O'Reilly (migration form Java to C#)
    */
    class TaxonomyQueryParser : QueryParser
    {
        private static Regex startDateRegex = new Regex(@"START_DATE:\s*\{(\d{4})-(\d{2})-(\d{2})\s+TO\s+\*\}", RegexOptions.IgnoreCase);
        private static Regex endDateRegex = new Regex(@"END_DATE:\s*\{\*\s+TO\s+(\d{4})-(\d{2})-(\d{2})\}", RegexOptions.IgnoreCase);

        public TaxonomyQueryParser(lnu.LuceneVersion luceneVersion, string fieldName, Analyzer analyaser)
            : base(luceneVersion, fieldName, analyaser)
        {
        }

        protected override Query NewRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            
            if (InformationAssetViewFields.SOURCE.ToString().Equals(field))
            {
                return NumericRangeQuery.NewInt32Range(field, Int32.Parse(part1), Int32.Parse(part2),
                    startInclusive, endInclusive);
            }

            if (InformationAssetViewFields.NUM_START_DATE.ToString().Equals(field) || InformationAssetViewFields.NUM_END_DATE.ToString().Equals(field))
            {
                int firstDate;
                int lastDate;

                int? nFirstDate;
                int? nLastDate;

                if (! int.TryParse(part1, out firstDate))
                {
                    nFirstDate = null;
                }
                else
                {
                    nFirstDate = firstDate;
                }

                if (!int.TryParse(part2, out lastDate))
                {
                    nLastDate = null;
                }
                else
                {
                    nLastDate = lastDate;
                }

                    return NumericRangeQuery.NewInt32Range(field, nFirstDate, nLastDate,
                        startInclusive, endInclusive);
            }

            TermRangeQuery termRangeQuery = TermRangeQuery.NewStringRange(field, part1, part2, startInclusive, endInclusive);
            //TermRangeQuery termRangeQuery = (TermRangeQuery)base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);

            return termRangeQuery;
        }

        protected override Query NewTermQuery(Term term)
        {
            if (InformationAssetViewFields.SOURCE.ToString().Equals(term.Field))
            {
                lnu.BytesRef bytesRef = new lnu.BytesRef();
                lnu.NumericUtils.Int32ToPrefixCoded(Int32.Parse(term.Text()), 0, bytesRef);
                TermQuery tq = new TermQuery(new Term(term.Field, bytesRef));

                return tq;
            }
            return base.NewTermQuery(term);
        }

        public override Query Parse(string query)
        {
            if(query.Contains(InformationAssetViewFields.START_DATE.ToString()))
            {
                // string pattern = @"START_DATE:\s*\{(\d{4})-(\d{2})-(\d{2})\s+TO\s+\*\}";
                string replacement = @"NUM_START_DATE:[$1$2$3 TO *]";

                // query = Regex.Replace(query, pattern, replacement);
                query = startDateRegex.Replace(query, replacement);
            }

            if (query.Contains(InformationAssetViewFields.END_DATE.ToString()))
            {
                //string patternEnd = @"END_DATE:\s*\{\*\s+TO\s+(\d{4})-(\d{2})-(\d{2})\}";
                string replacementEnd = @"NUM_END_DATE:[* TO $1$2$3]";

                //query = Regex.Replace(query, patternEnd, replacementEnd);
                query = endDateRegex.Replace(query, replacementEnd);
            }

            return base.Parse(query);
        }

    }
}
