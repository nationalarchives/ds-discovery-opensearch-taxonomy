using NationalArchives.Taxonomy.Common.DataObjects.OpenSearch;
using OpenSearch.Client;
using System;

namespace NationalArchives.Taxonomy.Common.Domain
{
    public class InformationAssetView
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
        public int NUM_START_DATE { get; set; }   
        public int NUM_END_DATE { get; set; }     
    }

    public class InformationAssetViewWithScore : InformationAssetView, ISearchResult
    {
        public InformationAssetViewWithScore()
        {
        }

        public InformationAssetViewWithScore(InformationAssetView infoAssetView)
        {
            this.CatDocRef = infoAssetView.CatDocRef;
            this.ContextDescription = infoAssetView.ContextDescription;
            this.CorpBodys = infoAssetView.CorpBodys;
            this.CoveringDates = infoAssetView.CoveringDates;
            this.Description = infoAssetView.Description;
            this.DocReference = infoAssetView.DocReference;
            this.NUM_END_DATE = infoAssetView.NUM_END_DATE;
            this.NUM_START_DATE = infoAssetView.NUM_START_DATE;
            this.Person_FullName = infoAssetView.Person_FullName;
            this.Place_Name = infoAssetView.Place_Name;
            this.Series = infoAssetView.Series;
            this.Source = infoAssetView.Source;
            this.Subjects = infoAssetView.Subjects;
            this.Title = infoAssetView.Title;
        }
        public double Score { get; set; }
    }
}
