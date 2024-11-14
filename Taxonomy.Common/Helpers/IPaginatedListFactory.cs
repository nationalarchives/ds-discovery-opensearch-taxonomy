using NationalArchives.Taxonomy.Common.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    public interface IPaginatedListFactory< TInput, T> where TInput : class
    {
        PaginatedList<T> CreatePaginatedList(long limit, long offset, double minScore);
    }
}
