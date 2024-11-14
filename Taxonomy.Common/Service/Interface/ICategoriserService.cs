using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Mongo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Service
{
    public interface ICategoriserService<T> where T : CategorisationResult
    {
        /**
         * Preview the categorisation of a document
         * 
         * @param docReference
         * @return {@link CategorisationResult}
         */
        Task<IList<T>> TestCategoriseSingle(string docReference);

        Task<IList<T>> TestCategoriseSingle(string docReference, bool includeScores);

        /**
         * Categorise a document and save the found categories
         * 
         * @param docReference
         * @return
         */
        Task<IList<T>> CategoriseSingle(string docReference, IList<Category> cachedCategories = null);

        Task<IList<T>> CategoriseSingle(string docReference);

        Task<IDictionary<string, List<T>>> CategoriseMultiple(string[] docReferences, IList<Category> cachedCategories = null);

        /**
         * Categorise a document and save the found categories
         * 
         * @param docReference
         * @param cachedCategories
         *            on batch processes, to avoid retrieving and parsing all
         *            category queries, provide cached categories
         */
        //Task<IList<T>> CategoriseSingle(String docReference, IList<Category> cachedCategories);

        /**
         * get new categorised documents from (including) date to nb of seconds in
         * past<br/>
         * if date is null, it will look for any document
         * 
         * @param date
         * @param nbOfSecondsInPast
         *            using this parameter we do not risk missing documents that
         *            were added from different servers
         * @param limit
         * @return
         */
        IList<IAViewUpdate> GetNewCategorisedDocumentsFromDateToNSecondsInPast(DateTime date, int nbOfSecondsInPast, int limit);

        /**
         * find last update on categories on iaviews from mongo db
         * 
         * @return
         */
        IAViewUpdate FindLastIAViewUpdate();

        /**
         * refresh the index used for categorisation.<br/>
         * implies to commit changes on Solr dedicated server AND update the index
         * reader on Lucene<br/>
         * It is necessary to call that method if the document to categorise was
         * indexed right before that call
         */
        void RefreshTaxonomyIndex();

        /**
         * get new categorised documents since document and up to nb of seconds in
         * past
         * 
         * @param afterIAViewUpdate
         * @param nbOfSecondsInPast
         *            using this parameter we do not risk missing documents that
         *            were added from different servers
         * @param limit
         * @return
         */
        IList<IAViewUpdate> GetNewCategorisedDocumentsAfterDocumentAndUpToNSecondsInPast(IAViewUpdate afterIAViewUpdate,
            int nbOfSecondsInPast, int limit);
    }
}
