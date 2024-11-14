using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.DataObjects.Mongo
{
    [BsonIgnoreExtraElements]
    internal sealed class CategoryFromMongo
    {
        [BsonElement("TAXONOMY_ID")]
        public string CIAID { get; set; }
        [BsonElement("TAXONOMY")]
        public string Title { get; set; }
        [BsonElement("QUERY_TEXT")]
        public string QueryText { get; set; }
        public double SC {get; set;}
    }
}
