using System;
using System.Collections.Generic;
using System.Text;
using Nest;
using Elasticsearch.Net;

namespace NationalArchives.Taxonomy.Common.DataObjects.Elastic
{
    public class CategoryFromElastic
    {
        public string ID { get; set; } // i.e. Ciaid
        public string query_text { get; set; }
        public string title { get; set; }
        public bool locked { get; set; }
        public double SC { get; set; }
    }
}
