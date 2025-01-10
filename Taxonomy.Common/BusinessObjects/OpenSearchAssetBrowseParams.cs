using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    public class OpenSearchAssetBrowseParams
    {
        public int PageSize { get; set; }
        public int  ScrollTimeout { get; set; }
        public HeldByCode HeldByCode { get; set; }

        public bool LogFetchedAssetIds { get; set; }
    }
}
