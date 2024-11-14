using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    public class OpenSearchParameters
    {

        private HeldByCode _helByCode;
        /// <summary>
        /// Search query
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Search query where search terms are restricted by the field name
        /// <para>Note: <remarks>Key = field name, Value = search query. If the search query is type of DateTime, use <see cref="DateMatchQueries"/>
        /// </remarks></para>
        /// </summary>
        public List<KeyValuePair<string, string>> FieldQueries { get; set; } = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// Search query where search terms are restricted by the nested field name
        /// <para>Note: <remarks>Key = field name, Value = search query. If the search query is type of DateTime, use <see cref="DateMatchQueries"/>
        /// </remarks></para>
        /// </summary>
        public List<KeyValuePair<string, string>> NestedFieldQueries { get; set; } = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// List of fields the search will be limited to and the "boosts" to associate with each field when building a query.
        /// <para>Use the following format: fieldName^boost</para>
        /// </summary>
        /// <example>catalogue_reference^10</example>
        public List<string> SearchFields { get; set; } = new List<string>();

        /// <summary>
        /// Search query where search terms are restricted by the field name and the type of the value of the field is a Date
        /// <para><remarks>Key = field name, Value = DateTime </remarks></para>
        /// </summary>
        public List<KeyValuePair<string, DateTime>> DateMatchQueries { get; set; } = new List<KeyValuePair<string, DateTime>>();

        /// <summary>
        /// Date range query
        /// <remarks>Tuple object values: Item1 = field name, Item2 = From date, Item3 = To date</remarks>
        /// </summary>
        public List<(string fieldName, DateTime fromDate, DateTime toDate)> DateRange { get; set; }
            = new List<(string fieldName, DateTime fromDate, DateTime toDate)>();

        /// <summary>
        /// Filter queries.
        /// <para><remarks>Created when filters have been applied and does not influence the score of the result,
        /// i.e. results returned in the score order defined/applied by main query</remarks></para>
        /// </summary>
        public List<KeyValuePair<string, IEnumerable<object>>> FilterQueries { get; set; } = new List<KeyValuePair<string, IEnumerable<object>>>();

        /// <summary>
        /// List of fields which is used for filtering search results initially
        /// </summary>
        public List<string> FacetFields { get; set; } = new List<string>();

        /// <summary>
        /// Paging offset
        /// </summary>
        public int PagingOffset { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Group results by the fields
        /// </summary>
        public List<string> GroupResultsBy { get; set; } = new List<string>();

        /// <summary>
        /// Results sorting fields
        /// </summary>
        public IDictionary<string, ResultsSortOrder> Sort { get; set; } = new Dictionary<string, ResultsSortOrder>();

        /// <summary>
        /// Deep pagination cursor
        /// </summary>
        public string CursorMark { get; set; } = string.Empty;

        public int? Scroll { get; set; } = null;

        public bool? IncludeSource { get; set; } = null;

        public HeldByCode HeldByCode { get => _helByCode; set => _helByCode = value; }
    }

    /// <summary>
    /// Sort search results enum
    /// </summary>
    public enum ResultsSortOrder { Ascending, Descending }
}