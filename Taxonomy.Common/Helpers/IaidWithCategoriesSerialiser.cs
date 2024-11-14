using Apache.NMS;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NationalArchives.Taxonomy.Common.Helpers
{
    internal static class IaidWithCategoriesSerialiser
    {
        public static byte[] ToByteArray(this List<IaidWithCategories> categorisationResult)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, categorisationResult);
                return ms.ToArray();
            }
        }

        public static byte[] ToByteArray(this IaidWithCategories categorisationResult)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, categorisationResult);
                return ms.ToArray();
            }
        }


        internal static List<IaidWithCategories> IdxMessageToListOfIaidsWithCategories(IBytesMessage msg)
        {
            List<IaidWithCategories> returnList = null;

            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(msg.Content, 0, msg.Content.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);


                switch(obj)
                {
                    case List<IaidWithCategories> lc:
                        returnList = lc;
                        break;
                    case IaidWithCategories singleResult:
                        returnList = new List<IaidWithCategories>() { singleResult };
                        break;
                    default:
                        throw new TaxonomyException("Unable to deserialise categorisation result(s) from queue message.");

                }
                return returnList;
            }
        }
    }
}
