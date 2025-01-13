using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 23 + _iaid.GetHashCode();
                foreach (string s in _categoryIds)
                {
                    hash = hash * 23 + s.GetHashCode(); 
                }

                return hash;
            }
        }

        public override bool Equals(object other)
        {
            IaidWithCategories otherIaidWithCategories = other as IaidWithCategories;

            if (otherIaidWithCategories == null || otherIaidWithCategories.Iaid != this.Iaid || otherIaidWithCategories.CategoryIds.Count != this.CategoryIds.Count) 
            { return false; }

            foreach (string s in this.CategoryIds) 
            {
                if (!otherIaidWithCategories.CategoryIds.Contains(s)) { return false; }
            }

            return true;
        }
    }
}
