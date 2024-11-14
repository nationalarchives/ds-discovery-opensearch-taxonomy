using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    [Serializable]
    public class IaidWithCategories
    {
        private string _iaid;
        private IList<string> _categoryIds;

        public IaidWithCategories(string iaid, IList<string> categoryIds)
        {
            _iaid = iaid;
            _categoryIds = categoryIds;
        }

        public string Iaid
        {
            get => _iaid;
        }

        public IReadOnlyCollection<string> CategoryIds
        {
            get => new ReadOnlyCollection<string>(_categoryIds);
        }

        public override string ToString()
        {
            string singularOrPlural = CategoryIds.Count == 1 ? "category" : "categories";

            StringBuilder sb = new StringBuilder($"{Iaid} has {CategoryIds.Count} matching {singularOrPlural}: ");
            foreach(string s in CategoryIds)
            {
                sb.Append(s + ", " );
            }

            return sb.ToString().TrimEnd(new char[] { ',', ' ' });
        }
    }
}
