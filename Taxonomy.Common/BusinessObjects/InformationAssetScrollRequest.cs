using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    public class InformationAssetScrollRequest
    {
        public string ScrollId { get; set; }

        public int Timeout { get; set; }

        public int PageSize { get; set; }
    }
}
