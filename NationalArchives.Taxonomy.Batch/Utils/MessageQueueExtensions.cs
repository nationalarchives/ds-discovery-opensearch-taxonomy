using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NationalArchives.Taxonomy.Batch.Utils
{
    internal static class MessageQueueExtensions
    {
        private static readonly Regex informationAssetRegex = new Regex(@"^(C\d{2,8}|D\d{2,8}|\w{32})$", RegexOptions.IgnoreCase);

        public static IList<string> GetListOfDocReferencesFromMessage(this string message)
        {
            string[] listOfIaids = message.Split(";");
            return listOfIaids.Where(s => informationAssetRegex.IsMatch(s)).ToList();
        }
    }
}
