using System;
using System.Text;

namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    //TODO Possibly data object with transform?
    public class Category
    {
        private const string MISSING_FIELD = "<Missing>";

        public string Id { get; set; }

        public string Query { get; set; }

        public string Title { get; set; }

        public bool Lock { get; set; }

        public double Score { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Category [Id=");
            builder.Append(Id ?? MISSING_FIELD);
            builder.Append(", Query=");
            builder.Append(Query ?? MISSING_FIELD);
            builder.Append(", Title=");
            builder.Append(Title ?? MISSING_FIELD);
            builder.Append(", Score=");
            builder.Append(Score);
            builder.Append(", Lock=");
            builder.Append(Lock);
            builder.Append("]");
            return builder.ToString();
        }
    }
}
