namespace NationalArchives.Taxonomy.Common.Domain.Repository.Mongo
{
    class CategoryLight
    {
        public string Ciaid { get; set; }
        public string Title { get; set; }

        public string CiaidAndTitle
        {
            get { return $"{Ciaid} {Title}"; }
        }

        public override string ToString()
        {
            return $"CategoryLight [Ciaid={Ciaid}, Title={Title}]";
        }
    }
}