using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public interface IAmazonSqsMessageReader<T>
    {
        IList<T> ReadMessage(string messageBody);
    }

    public class AmazonSqsJsonMessageReader<T> : IAmazonSqsMessageReader<T>
    {
        public IList<T> ReadMessage(string messageBody)
        {
            List<T> result = JsonConvert.DeserializeObject<List<T>>(messageBody);
            return result;
        }
    }

    public class AmazonSqsStringMessageReader : IAmazonSqsMessageReader<string>
    {
        public IList<string> ReadMessage(string messageBody)
        {
            char[] delimiterChars = { ' ', ',', '.', ':',';','\t' };
            string[] result = messageBody.Trim('\"').Split(delimiterChars);
            return result;
        }
    }
}
