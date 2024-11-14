using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
[assembly: InternalsVisibleTo("NationalArchives.Taxonomy.Common.UnitTests")]

namespace NationalArchives.Taxonomy.Common
{
    public class TaxonomyTest
    {
        private string _first;
        private string _last;
        private double _age;

        public string FirstName { get => _first; set => _first = value; }
        public string LastName { get => _last; set => _last = value; }
        public double Age { get => _age; set => _age = value; }
    }
}
