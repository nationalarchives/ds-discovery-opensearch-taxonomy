using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Elastic
{
    internal static class ElasticFieldConstants
    {
        public const string DESCRIPTION = "DESCRIPTION";
        public const string CATALOGUE_REFERENCE = "CATALOGUE_REFERENCE";
        public const string CONTEXT = "CONTEXT";
        public const string TITLE = "TITLE";
        public const string COVERING_DATES = "COVERING_DATES";
        public const string PERSON = "Person_Full_Name";
        public const string PLACE_NAME = "Place_Name";
        public const string CORPORATE_BODY = "Corporate_Body";
        public const string SUBJECT = "Subjects";

        public const string TEXT_GEN = "text_gen";
        public const string TEXT_CAS_PUNC = "textcaspunc";
        public const string TEXT_CAS_NO_PUNC = "textcasnopunc";
        public const string TEXT_NO_CAS_NO_PUNC = "textnocasnopunc";

        public const string ES_HELD_BY_CODE = "HELD_BY_CODE";
    }
}
