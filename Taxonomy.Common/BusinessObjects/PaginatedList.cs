using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NationalArchives.Taxonomy.Common.Domain
{
    // TODO: Move to new file or replace, depending what Elastic can provide...
    // we really just need to wrap a list of InformationAssetView representing a
    // subset of the total results with the total number of hits etc.
    public class PaginatedList<T>
    {
        private IList<T> results;
        private long limit;
        private long offset;
        private long numberOfResults;
        private Double minimumScore;

        /**
         * Returns the current number of elements. <br/>
         * Null safe
         * 
         * @return
         */
        public int size()
        {
            return results.Count;
        }

        public PaginatedList()
        {

        }

        public IList<T> Results
        {
            get => new ReadOnlyCollection<T>(results);
            set => results = value;
        }

        //TODO: Possibly rename, this is the TOTAL number of results, not just the current page.
        public long NumberOfResults
        {
            get => numberOfResults;
            set => numberOfResults = value;
        }

        public long Limit
        {
            get => limit;
            set => limit = value;
        }

        public long Offset
        {
            get => offset;
            set => offset = value;
        }

        public double MinimumScore
        {
            get => minimumScore;
            set => minimumScore = value;
        }
    }
}
