using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    public class InformationAssetScrollList
    {
        private IList<string> _results;
        private string _scrollId;

        public InformationAssetScrollList(string scrollId, IList<string> results)
        {
            if(String.IsNullOrWhiteSpace(scrollId) || results == null)
            {
                throw new TaxonomyException("Scroll Id and result list are rquired");
            }

            _scrollId = scrollId;
            _results = results;
        }

        public string ScrollId
        {
            get { return _scrollId; }
        }

        public IReadOnlyCollection<string> ScrollResults
        {
            get { return new ReadOnlyCollection<string>(_results); }
        }
    }
}
