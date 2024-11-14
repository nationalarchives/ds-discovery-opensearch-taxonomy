using System;
using System.Text;

namespace NationalArchives.Taxonomy.Common
{
    public class TaxonomyException : Exception
    {
        private TaxonomyErrorType _taxonomyErrorType;

        public TaxonomyException() : base()
        {
        }

        public TaxonomyException(string message) : base(message)
        {
        }

        public TaxonomyException(TaxonomyErrorType taxonomyErrorType) : this()
        {
            this._taxonomyErrorType = taxonomyErrorType;
        }

        public TaxonomyException(TaxonomyErrorType taxonomyErrorType, String message) : this(message)
        {
            this._taxonomyErrorType = taxonomyErrorType;
        }

        public TaxonomyException(TaxonomyErrorType taxonomyErrorType, Exception inner) : base("Taxonomy Exception", inner)
        {
            this._taxonomyErrorType = taxonomyErrorType;
        }

        public TaxonomyException(TaxonomyErrorType taxonomyErrorType, string message, Exception inner) : base(message, inner)
        {
            this._taxonomyErrorType = taxonomyErrorType;
        }

        public TaxonomyErrorType TaxonomyError
        {
            get => _taxonomyErrorType;
            set => _taxonomyErrorType = value;
        }

        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("TaxonomyException [taxonomyErrorType=");
            builder.Append(_taxonomyErrorType);
            builder.Append(", getMessage()=");
            builder.Append(this.Message);
            builder.Append("]");
            return builder.ToString();
        }
    }
}
