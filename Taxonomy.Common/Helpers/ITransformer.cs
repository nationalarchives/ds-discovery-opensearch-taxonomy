using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    public interface ITransformer<TInput,TOutput>
    {
         TOutput Transform(TInput inputType);
    }
}
