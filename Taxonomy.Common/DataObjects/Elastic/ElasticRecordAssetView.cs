using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.DataObjects.Elastic
{
    [ElasticsearchType(Name = "recordassetview")]
    public class ElasticRecordAssetView
    {
        public string ID { get; set; } = string.Empty;
        public double? Score { get; set; }
        public string CATALOGUE_REFERENCE { get; set; } = string.Empty;
        public int SOURCE { get; set; }
        public DateTime START_DATE { get; set; } = DateTime.MinValue;
        public DateTime END_DATE { get; set; } = DateTime.MaxValue;
        public string COVERING_DATES { get; set; } = string.Empty;
        public string TITLE { get; set; } = string.Empty;
        public string DESCRIPTION { get; set; } = string.Empty;
        public string[] CORPORATE_BODY { get; set; } = new string[] { };
        public string[] PERSON_FULL_NAME { get; set; } = new string[] { };
        public string[] PLACE_NAME { get; set; } = new string[] { };
        public string[] PLACE_ADDRESS { get; set; } = new string[] { };
        public string SERIES_CODE { get; set; } = string.Empty;
        public string[] SUBJECT { get; set; } = new string[] { };
        public string ADMINISTRATIVE_HISTORY { get; set; } = string.Empty;
        public string ALTERNATIVE_NAME { get; set; } = string.Empty;
        public string ARRANGEMENT { get; set; } = string.Empty;
        public string CLOSURE_CODE { get; set; } = string.Empty;
        public string CLOSURE_STATUS { get; set; } = string.Empty;
        public string CLOSURE_TYPE { get; set; } = string.Empty;
        public string CONTENT { get; set; } = string.Empty;
        public string CONTEXT { get; set; } = string.Empty;
        public string DEPARTMENT_CODE { get; set; } = string.Empty;
        public string FORMER_DEPARTMENT_REFERENCE { get; set; } = string.Empty;
        public string FORMER_PRO_REFERENCE { get; set; } = string.Empty;
        public string[] HELD_BY_CODE { get; set; } = new string[] { };
        public string MAP_DESIGNATION { get; set; } = string.Empty;
        public string MAP_SCALE { get; set; } = string.Empty;
        public string NOTE { get; set; } = string.Empty;
        public DateTime OPENING_DATE { get; set; } = DateTime.MinValue;
        public string PHYSICAL_CONDITION { get; set; } = string.Empty;
        public string[] REPOSITORY_NAME { get; set; } = new string[] { };
        public string SCHEMA { get; set; } = string.Empty;
        public int CATALOGUE_LEVEL { get; set; } = 0;
        public string[] TAXONOMY_NAME { get; set; }
        public string[] TAXONOMY_ID { get; set; }
    }
}
