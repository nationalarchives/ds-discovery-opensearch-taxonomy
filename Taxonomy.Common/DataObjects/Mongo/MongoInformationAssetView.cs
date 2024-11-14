using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Mongo
{
    class MongoInformationAssetView
    {
        public string DocReference { get; set; }

        public List<CategoryLight> Categories { get; set; }

        public string CatDocRef { get; set; }

        public DateTime CreationDate { get; set; }

        public string Series { get; set; }
    }
}
