using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalArchives.Taxonomy.Common.Helpers;

namespace NationalArchives.Taxonomy.Common.UnitTests
{
    [TestClass]
    public class SerialisationTests
    {
        [TestMethod]
        public void IaidWithCategories_Serialisation()
        {
            var iaidWithCategories1 = new IaidWithCategories("C12345", new List<string>() {"C10161", "C10272", "C10383", "C10494", "C10505" });
            var iaidWithCategories2 = new IaidWithCategories("C54321", new List<string>() { "C76757" });
            var iaidWithCategories3 = new IaidWithCategories("C67890", new List<string>() {});
            var iaidWithCategories4 = new IaidWithCategories("C90818", new List<string>() { "C40303" });

            var categorisationResults = new List<IaidWithCategories>() { iaidWithCategories1, iaidWithCategories2, iaidWithCategories3, iaidWithCategories4 };

            byte[] serialisedResults = categorisationResults.ToByteArray();
            List<IaidWithCategories> deserialisedResults = IaidWithCategoriesSerialiser.IdxMessageToListOfIaidsWithCategories(serialisedResults);

            Assert.AreEqual(categorisationResults.Count, deserialisedResults.Count);

            foreach (IaidWithCategories categorisation in categorisationResults)
            {
                Assert.IsTrue(deserialisedResults.Contains(categorisation));
            }
        }
    }
}
