using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using lnu = Lucene.Net.Util;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;

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
    class TaxonomyMultiFieldQueryParser : MultiFieldQueryParser
    {
        public TaxonomyMultiFieldQueryParser(lnu.LuceneVersion luceneVersion, string[] fieldNames, Analyzer analyaser)
            : base(luceneVersion, fieldNames, analyaser)
        {
        }

        protected override Query NewRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            
            if (InformationAssetViewFields.SOURCE.ToString().Equals(field))
            {
                return NumericRangeQuery.NewInt32Range(field, Int32.Parse(part1), Int32.Parse(part2),
                    startInclusive, endInclusive);
            }
            return (TermRangeQuery)base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
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
    }
}
