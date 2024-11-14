using NationalArchives.Taxonomy.Common.DataObjects.Elastic;

namespace NationalArchives.Taxonomy.Common.Domain
{
    public class InformationAssetView // : ISearchResult
    {
        //TODO: Property names ported from Java App (only capitalisation changed)
        //- do they need to (or can they) change for Elastic?
        public string DocReference { get; set; }
        public string CatDocRef { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string[] CorpBodys { get; set; }
        public string[] Subjects { get; set; }
        public string[] Place_Name { get; set; }
        public string[] Person_FullName { get; set; }
        public string ContextDescription { get; set; }
        public string CoveringDates { get; set; }
        public string Series { get; set; }
        public string Source { get; set; }
    }

    public class InformationAssetViewWithScore : InformationAssetView, ISearchResult
    {
        public double Score { get; set; }
    }
}
