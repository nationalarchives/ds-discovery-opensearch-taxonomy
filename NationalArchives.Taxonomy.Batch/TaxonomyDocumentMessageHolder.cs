using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal class TaxonomyDocumentMessageHolder
    {
        private List<string> _listOfDocReferences;
        private List<string> _listOfDocReferencesInError;

        public TaxonomyDocumentMessageHolder(IEnumerable<string> listOfDocReferences)
        {
            this._listOfDocReferences = new List<string>(listOfDocReferences);
            this._listOfDocReferencesInError = new List<String>();
        }

        public IList<string> ListOfDocReferences
        {
            get => new ReadOnlyCollection<string>(_listOfDocReferences);
        }

        public IList<string> ListOfDocReferencesInError
        {
            get => new ReadOnlyCollection<string>(_listOfDocReferencesInError);
        }

        public void AddDocReferenceInError(string docReferenceInError)
        {
            _listOfDocReferencesInError.Add(docReferenceInError);
        }

        public bool HasProcessingErrors
        {
            get => _listOfDocReferencesInError.Count > 0;
        }
    }
}
