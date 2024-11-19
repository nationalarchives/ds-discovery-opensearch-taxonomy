using Apache.NMS;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    internal static class IaidWithCategoriesSerialiser
    {
        private const string CATEGORISATION_RESULTS_START = "Start of Categorisation Results.";
        private const string CATEGORISATION_RESULT_START = "Categorisation Result:";
        private const string CATEGORISATION_RESULT_END = "Result End.";
        private const string CATEGORISATION_RESULTS_END = "End of Categorisation Results.";

        private const string UNEXPECTED_READER_OUTPUT = "Unexpected ouput when deserialising Categorisation Results from queue.";
        public static byte[] ToByteArray(this List<IaidWithCategories> categorisationResult)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(CATEGORISATION_RESULTS_START);
                    foreach (IaidWithCategories item in categorisationResult)
                    {
                        writer.Write(CATEGORISATION_RESULT_START);
                        writer.Write(item.Iaid);
                        foreach (string s in item.CategoryIds)
                        {
                            writer.Write(s);
                        }
                        writer.Write(CATEGORISATION_RESULT_END);
                    }
                    writer.Write(CATEGORISATION_RESULTS_END);
                }
                return stream.ToArray();
            }       
        }
    
        [Obsolete]
        public static byte[] ToByteArray(this IaidWithCategories categorisationResult)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(categorisationResult.Iaid);
                    foreach(string s in categorisationResult.CategoryIds)
                    {
                        writer.Write(s);
                    }
                }
                return stream.ToArray();
            }
        }

        internal static List<IaidWithCategories> IdxMessageToListOfIaidsWithCategories(byte[] bytes)
        {

            var deserialisedResults = new List<IaidWithCategories>();

            using (var stream = new MemoryStream(bytes))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    string resultsStart = reader.ReadString();
                    {
                        if(resultsStart != CATEGORISATION_RESULTS_START)
                        {
                            throw new TaxonomyException(UNEXPECTED_READER_OUTPUT);
                        }
                    }

                    while(true)
                    {
                        string next = reader.ReadString();

                        if (next == CATEGORISATION_RESULTS_END || String.IsNullOrEmpty(next))
                        {
                            break;
                        }
                        else
                        {
                            if (next == CATEGORISATION_RESULT_START)
                            {
                                var nextCatResult = GetResult(reader);
                                deserialisedResults.Add(nextCatResult);
                            }
                            else
                            {
                                throw new TaxonomyException(UNEXPECTED_READER_OUTPUT);
                            }
                        }
                    }
                }

            }

            return deserialisedResults;
        }

        private static IaidWithCategories GetResult(BinaryReader reader)
        {
            string iaid = reader.ReadString();
            List<string> categories = new List<string>();

            string next;

            while (true)
            {   
                next = reader.ReadString();

                if (next == CATEGORISATION_RESULT_END || String.IsNullOrEmpty(next)) 
                { 
                    break; 
                }  
                categories.Add(next);
            };

            return new IaidWithCategories(iaid, categories);
         }
    }
}
