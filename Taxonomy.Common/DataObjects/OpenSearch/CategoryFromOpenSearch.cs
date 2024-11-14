namespace NationalArchives.Taxonomy.Common.DataObjects.OpenSearch
{
    public class CategoryFromOpenSearch
    {
        public string ID { get; set; } // i.e. Ciaid
        public string query_text { get; set; }
        public string title { get; set; }
        public bool locked { get; set; }
        public double SC { get; set; }
    }
}
