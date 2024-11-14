
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using el = NationalArchives.Taxonomy.Common.Domain.Repository.Elastic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using lnu = Lucene.Net.Util;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Lucene
{
    public class LuceneHelperTools
    {
        private const string DEFAULT_FIELD = "description";
        private readonly string _defaultTaxonomyField;
        private readonly string[] _queryFields;

        private readonly Query _catalogueFilter;
        
        private readonly lnu.LuceneVersion _luceneVersion = lnu.LuceneVersion.LUCENE_CURRENT;

        private readonly Analyzer _iaViewSearchAnalyser;
        private readonly Analyzer _standardAnalyserForId = new StandardAnalyzer(LuceneVersion.LUCENE_CURRENT);

        public LuceneHelperTools()
        {
            this._defaultTaxonomyField = DEFAULT_FIELD;
            this._iaViewSearchAnalyser = new StandardAnalyzer(LuceneVersion.LUCENE_CURRENT);
            this._queryFields = new string[] { DEFAULT_FIELD };

        }


        public LuceneHelperTools(string defaultTaxonomyField, Analyzer iaViewSearchAnalyser, string[] queryFields)
        {
            this._defaultTaxonomyField = defaultTaxonomyField;
            this._iaViewSearchAnalyser = iaViewSearchAnalyser;
            this._queryFields = queryFields;
        }

        public LuceneHelperTools(string defaultTaxonomyField, Analyzer iaViewSearchAnalyser, string[] queryFields, Query catalogueFilter) : this(defaultTaxonomyField, iaViewSearchAnalyser, queryFields)
        {
            this._catalogueFilter = catalogueFilter;
        }

        ///**
        // * Release an an object from a manager without throwing any error<br/>
        // * log if any error occurs
        // *
        // * @param searcherManager
        // * @param searcher
        // */
        public static void ReleaseSearcherManagerQuietly(SearcherManager searcherManager, IndexSearcher searcher)
        {
            try
            {
                if (searcher != null)
                {
                    searcherManager.Release(searcher);
                    searcher = null;
                }
            }
            catch (Exception e)
            {
                //TODO: Logging...
                //logger.error("releaseSearcherManagerQuietly failed", e);
            }
        }

        /**
        * Close an object without throwing any error<br/>
        * log if any error occurs
        *
        * @param object
        */
        public static void DisposeQuietly(IDisposable objectToDispose)
        {
            try
            {
                objectToDispose?.Dispose();
            }
            catch (IOException ioe)
            {
                // TODO: Log / throw..
                // logger.error("closeCloseableObjectQuietly failed", ioe);
            }
        }

        public static string RemovePunctuation(string inputString)
        {
            return Regex.Replace(inputString, "[^a-zA-Z ]", String.Empty);
        }

        public Query BuildSearchQueryWithFiltersIfNecessary(string queryString, Query filter)
        {
            Query searchQuery = BuildSearchQuery(queryString);

            if (filter == null)
            {
                filter = this._catalogueFilter;
            }

            Query finalQuery;
            if (filter != null)
            {
                var booleanQuery = new BooleanQuery();

                booleanQuery.Add(searchQuery, Occur.MUST);
                booleanQuery.Add(filter, Occur.MUST);
                finalQuery = booleanQuery;
            }
            else
            {
                finalQuery = searchQuery;
            }
            return finalQuery;
        }

        public Query BuildSearchQuery(string queryString)
        {


            QueryParser parser = new TaxonomyQueryParser(_luceneVersion, _defaultTaxonomyField, _iaViewSearchAnalyser)
            {
                AllowLeadingWildcard = true
            };  // QueryParser from org.apache.lucene.queryparser.classic; 

            Query searchQuery;
            try
            {
                searchQuery = parser.Parse(queryString);
            }
            catch (ParseException e)
            {
                throw new TaxonomyException(TaxonomyErrorType.INVALID_CATEGORY_QUERY, e);
            }
            return searchQuery;
        }

        public BooleanQuery BuildBooleanQuery(InformationAssetView infoAsset, Query searchQuery)
        {
            return BuildBooleanQuery(infoAsset.DocReference, searchQuery);
        }

        public BooleanQuery BuildBooleanQuery(string infoAssetId, Query searchQuery)
        {

            // Use the Standard Analyzer for the ID query - the main query supplied via searchQuery will alreday have been parses
            // using whichichever Analyzer is specified in _iaViewSearchAnalyser
            TaxonomyQueryParser parser = new TaxonomyQueryParser(_luceneVersion, _defaultTaxonomyField,  _standardAnalyserForId);
            
            Query queryToMatchId = parser.Parse($"id:{infoAssetId.ToUpperInvariant()}");

            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(queryToMatchId, Occur.MUST);
            booleanQuery.Add(searchQuery, Occur.MUST);

            return booleanQuery;
        }

        public IReadOnlyCollection<string> QueryFields
        {
            get { return new ReadOnlyCollection<string>(_queryFields); }
        }

        public string DefaultTaxonomyField
        {
            get => _defaultTaxonomyField;
        }

        public static void ConfigureLuceneServices(CategoriserLuceneParams categoriserLuceneParams, IServiceCollection services)
        {
            if(categoriserLuceneParams == null || string.IsNullOrEmpty(categoriserLuceneParams.DefaultTaxonomyField) || categoriserLuceneParams.QueryFields.Length == 0)
            {
                throw new TaxonomyException("Invalid config!");
            }

            string[] queryFields = categoriserLuceneParams.QueryFields;
            string defaultTaxonomyField =  categoriserLuceneParams.DefaultTaxonomyField.ToLowerInvariant();
            // Lucene helper tools
            services.AddSingleton<LuceneHelperTools>((ctx) =>
            {
                var analyser = ctx.GetRequiredService<Analyzer>();
                return new LuceneHelperTools(defaultTaxonomyField.ToString(), analyser, queryFields);
            });

            // IAViewTextGenAnalyser
            var synonymFilterFactoryArgs = new Dictionary<string, string>()
            {
                { "synonyms", "synonyms.txt" },
                { "expand", "true" },
                { "ignoreCase", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var synonymFilterFactory = new SynonymFilterFactory(synonymFilterFactoryArgs);

            var wordDelimiterFilterArgs = new Dictionary<string, string>()
            {
                {"preserveOriginal", "1" },
                { "generateWordParts", "1" },
                { "catenateWords", "1" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var wordDelimiterFilterFactory = new WordDelimiterFilterFactory(wordDelimiterFilterArgs);

            var stopFilterArgs = new Dictionary<string, string>()
            {
                { "words", "stopwords.txt" },
                { "enablePositionIncrements", "true" },
                { "luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.ToString() }
            };
            var stopFilterFactory = new StopFilterFactory(stopFilterArgs);

            // Anaylser

            switch (defaultTaxonomyField)
            {
                case el.ElasticFieldConstants.TEXT_NO_CAS_NO_PUNC:
                    // IAViewTextNoCasNoPuncAnalyser
                    services.AddTransient<Analyzer>((ctx) =>
                    {
                        ILogger<Analyzer> logger = ctx.GetRequiredService<ILogger<Analyzer>>();
                        return new IAViewTextNoCasNoPuncAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, logger);
                    });
                    break;
                case el.ElasticFieldConstants.TEXT_CAS_NO_PUNC:
                    // IAViewTextCasNoPuncAnalyser
                    services.AddTransient<Analyzer>((ctx) =>
                    {
                        ILogger<Analyzer> logger = ctx.GetRequiredService<ILogger<Analyzer>>();
                        return new IAViewTextCasNoPuncAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, logger);
                    });
                    break;
                case el.ElasticFieldConstants.TEXT_CAS_PUNC:
                    // IAViewTextCasPuncAnalyser
                    services.AddTransient<Analyzer>((ctx) =>
                    {
                        ILogger<Analyzer> logger = ctx.GetRequiredService<ILogger<Analyzer>>();
                        return new IAViewTextCasPuncAnalyser(stopFilterFactory, synonymFilterFactory, AnalyzerType.INDEX, logger);
                    });
                    break;
                case el.ElasticFieldConstants.TEXT_GEN:
                    services.AddTransient<Analyzer>((ctx) =>
                    {
                        ILogger<Analyzer> logger = ctx.GetRequiredService<ILogger<Analyzer>>();
                        return new IAViewTextGenAnalyser(synonymFilterFactory, wordDelimiterFilterFactory, AnalyzerType.INDEX, logger);

                    });
                    break;
                default:
                    services.AddTransient<Analyzer>((ctx) =>
                    {
                        ILogger<Analyzer> logger = ctx.GetRequiredService<ILogger<Analyzer>>();
                        return new StandardAnalyzer(LuceneVersion.LUCENE_CURRENT);
                    });
                    break;
                //}
            }
        }
    }
}
