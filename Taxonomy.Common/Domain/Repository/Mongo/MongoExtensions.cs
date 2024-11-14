using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Mongo
{
    internal static class MongoExtensions
    {
        public static string Server<T>(this IMongoCollection<T> mongoCollection)
        { 
            IEnumerable<MongoServerAddress>  serverList = mongoCollection.Database.Client.Settings.Servers.Select(s => s);
            string servers = String.Join(",", serverList);
            return servers;
        }

        public static string DatabaseName<T>(this IMongoCollection<T> mongoCollection)
        {
            return mongoCollection.Database.DatabaseNamespace.DatabaseName;
        }

        public static string CollectionName<T>(this IMongoCollection<T> mongoCollection)
        {
            return mongoCollection.CollectionNamespace.CollectionName;
        }

    }
}
