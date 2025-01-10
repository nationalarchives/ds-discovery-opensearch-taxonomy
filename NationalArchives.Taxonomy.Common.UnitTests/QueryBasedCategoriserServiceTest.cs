using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch;
using NationalArchives.Taxonomy.Common.Service;
using NSubstitute;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.UnitTests
{
    [TestClass]
    public class QueryBasedCategoriserServiceTest
    {
        private IIAViewRepository _iaViewRepository;
        private ICategoryRepository _categoryRepository;

        [TestInitialize]
        public void Init()
        {
            _iaViewRepository = Substitute.For<IIAViewRepository>();
            _categoryRepository = Substitute.For<ICategoryRepository>();

            _iaViewRepository.FindRelevantCategoriesForDocument(Arg.Any<InformationAssetView>(), 
                Arg.Any<IList<Category>>(), Arg.Any<bool>()).Returns(SubstituteCategorisationResults());

            _iaViewRepository.FindRelevantCategoriesForDocument(Arg.Any<InformationAssetView>(),
                Arg.Any<IList<Category>>()).Returns(SubstituteCategorisationResults());

            _iaViewRepository.SearchDocByDocReference(Arg.Any<string>())
                .Returns(n => new InformationAssetView() {CatDocRef = n.ArgAt<string>(0) });
        }


        [TestMethod]
        public void TestCategoriseSingle_CallWithIaidOnly_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            var result = categoriserService.TestCategoriseSingle(testAsset.CatDocRef).Result;

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IList<CategorisationResult>));
        }

        [TestMethod]
        public void TestCategoriseSingle_CallWithInformationAssetAndCachedCategoryInputList_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            IList<Category> cachedCategories = TestCategories();

            var result = categoriserService.TestCategoriseSingle(testAsset, true, cachedCategories).Result;

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IList<CategorisationResult>));
        }

        [TestMethod]
        public void TestCategoriseSingle_CallWithInformationAssetAndNullCategoryInputList_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            IList<Category> cachedCategories = TestCategories();

            var result = categoriserService.TestCategoriseSingle(testAsset, true, null).Result;

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IList<CategorisationResult>));
        }

        [TestMethod]
        [ExpectedException(typeof(TaxonomyException))]
        public void TestCategoriseSingle_CallWithNullInformationAssetAndCategoryInputList_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            IList<Category> cachedCategories = TestCategories();

            var awaiter = categoriserService.TestCategoriseSingle(null, true, cachedCategories).GetAwaiter();
            var result = awaiter.GetResult();
        }

        [TestMethod]
        [ExpectedException(typeof(TaxonomyException))]
        public void TestCategoriseSingle_CallWithNullIaid_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            var awaiter = categoriserService.TestCategoriseSingle(null).GetAwaiter();
            var result = awaiter.GetResult();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IList<CategorisationResult>));
        }

        [TestMethod]
        [ExpectedException(typeof(TaxonomyException))]
        public void TestCategoriseSingle_CallWithEmptyIaid_ReturnsListOfCategorisationResults()
        {
            var categoriserService = new QueryBasedCategoriserService(_iaViewRepository, _categoryRepository);

            InformationAssetView testAsset = TestInformationAsset();
            var awaiter = categoriserService.TestCategoriseSingle(string.Empty).GetAwaiter();
            var result = awaiter.GetResult();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(IList<CategorisationResult>));
        }

        private InformationAssetView TestInformationAsset()
        {
            return new InformationAssetView() { CatDocRef = "C12345", Title = "Test IAID" };
        }

        private IList<CategorisationResult> SubstituteCategorisationResults()
        {
            IList<Category> categories = TestCategories();

            var results = new List<CategorisationResult>();

            foreach(Category c in categories)
            {
                results.Add(new CategorisationResult(c, 1.5));
            }

            return results;
        }

        private IList<Category> TestCategories()
        {
            List<Category> TestCategories = new List<Category>()
            {
                { new Category() { Id = "C12345", Query = "cheese", Score = 1.0 } },
                { new Category() { Id = "C23456", Query = "wine", Score = 1.1 } },
                { new Category() { Id = "C34567", Query = "roses", Score = 1.2 } },
                { new Category() { Id = "C45678", Query = "chocolate", Score = 1.4 } }
            };
            return TestCategories;
        }
    }
}
