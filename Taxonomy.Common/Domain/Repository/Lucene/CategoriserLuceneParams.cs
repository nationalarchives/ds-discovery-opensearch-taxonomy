using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    public class CategoriserLuceneParams
    {
        public string[] QueryFields { get; set; }
        public string DefaultTaxonomyField { get; set; }

        //TODO: This field doesn't really belong here - only used for the API search
        public bool UseDefaultTaxonomyFieldForApiSearch { get; set; }
    }
}
