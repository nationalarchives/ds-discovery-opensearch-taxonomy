namespace NationalArchives.Taxonomy.Common.BusinessObjects
{
    public class CategorisationResult
    {
        protected double? _score;
        protected Category _category;

        public CategorisationResult(Category category, double? score = null)
        {
            _category = category;
            _score = score;
        }

        public string CategoryID
        {
            get => _category.Id;
        }

        public string CategoryName
        {
            get => _category.Title;
        }

        public double? Score
        {
            get => _score;
        }

        public override string ToString()
        {
            string result = $"Category ID: {CategoryID}, Category Name: {CategoryName}, Score: {Score?.ToString() ?? "N/A"} ";
            return result;
        }
    }
}
