using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal sealed class CategorisationParams
    {
            public int CategoriserStartDelay { get; set; }
            public uint  BatchSize { get; set; }
            public uint CategorisationBatchConcurrency { get; set; }
            public bool LogEachCategorisationResult { get; set; }
            public int TaxonomyExceptionThreshold { get; set; }
    }
}
