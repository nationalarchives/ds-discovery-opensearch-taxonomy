namespace NationalArchives.Taxonomy.Common
{
    public enum TaxonomyErrorType
    {
        /**
  * if the category query to search is invalid
  */
        INVALID_CATEGORY_QUERY,

        /**
         * when trying to use a category that is currently being published (TSET
         * Based)
         */
        LOCKED_CATEGORY,

        /**
         * when an error occurs while accessing lucene index
         */
        LUCENE_IO_EXCEPTION,

        /**
         * when an invalid parameter was passed to a function (mostly on the front
         * side: WS, CLI, etc)
         */
        INVALID_PARAMETER,

        /**
         * when an error occurs while parsing a search query with Lucene
         */
        LUCENE_PARSE_EXCEPTION,

        /**
         * when an error occurs while creating the beans dedicated to Lucene:
         * parsing of the lucene version failed
         */
        LUCENE_PARSE_VERSION,

        /**
         * When an error occurs while using the Messaging Active MQ queue
         */
        JMS_EXCEPTION,

        /**
         * Document was not found in lucene Index
         */
        DOC_NOT_FOUND,

        ELASTIC_SCROLL_EXCEPTION,

        ELASTIC_INVALID_RESPONSE,

        ELASTIC_UPDATE_ERROR,

        ELASTIC_BULK_UPDATE_ERROR,

        FULL_REINDEX_WORKER_EXCEPTION,

        MISSING_OR_INVALID_DOC_REFERENCE,

        IAID_QUEUE_ADD_FAILURE
    }
}