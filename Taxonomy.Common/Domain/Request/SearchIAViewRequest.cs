using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common
{
    public class SearchIAViewRequest
    {
        private string _categoryQuery;

        private double _score;

        private int _offset;

        private int _limit;

        public string CategoryQuery
        {
            get => _categoryQuery;
            set => _categoryQuery = value;
        }
        public double Score
        {
            get => _score;
            set => _score = value;
        }

        public int Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public int Limit
        {
            get => _limit;
            set => _limit = value;
        }
    }
}
