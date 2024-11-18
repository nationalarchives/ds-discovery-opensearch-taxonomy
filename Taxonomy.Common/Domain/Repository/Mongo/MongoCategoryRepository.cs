using AutoMapper;
using MongoDB.Driver;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.DataObjects.Mongo;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.Mongo
{
    public sealed class MongoCategoryRepository : ICategoryRepository, IDisposable
    {
        private static IList<Category> _categories;
        private readonly IMapper _mapper;

        private IMongoCollection<CategoryFromMongo> m_MongoCollection = null;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public MongoCategoryRepository(MongoConnectionParams mongoConnectionParams, IMapper mapper)
        {
            _mapper = mapper;
            try
            {
                string connectionString = mongoConnectionParams.ConnectionString;
                string databaseName = mongoConnectionParams.DatabaseName;
                string collectionName = mongoConnectionParams.CollectionName;

                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                m_MongoCollection = database.GetCollection<CategoryFromMongo>(collectionName);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public long Count()
        {
            if (_categories == null)
            {
                var awaiter = FindAll().GetAwaiter();
                awaiter.GetResult();
            }
            return _categories.Count;
        }

        public async Task<IList<Category>> FindAll()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_categories != null)
                {
                    return _categories;
                }
                try
                {
                    FilterDefinition<CategoryFromMongo> filter = FilterDefinition<CategoryFromMongo>.Empty;
                    var categories = new List<Category>();

                    using (IAsyncCursor<CategoryFromMongo> mongoCategoriesCursor = await m_MongoCollection.FindAsync<CategoryFromMongo>(filter))
                    {
                        while (await mongoCategoriesCursor.MoveNextAsync())
                        {
                            IEnumerable<CategoryFromMongo> batch = mongoCategoriesCursor.Current;
                            foreach (CategoryFromMongo mongoCategory in batch)
                            {
                                Category category = _mapper.Map<Category>(mongoCategory);

                                foreach (string s in new string[] { category.Id, category.Query, category.Title })
                                {
                                    if (String.IsNullOrWhiteSpace(s))
                                    {
                                        throw new ApplicationException($"Error retreiving category data from Mongo Collection {m_MongoCollection.CollectionName()}, database {m_MongoCollection.DatabaseName()}, server {m_MongoCollection.Server()}.  Current Category: {category}.");
                                    }
                                }
                                categories.Add(category);
                            }
                        }
                    }

                    if (categories.Count > 0)
                    {
                        _categories = categories;
                    }
                    else
                    {
                        throw new ApplicationException($"Could not retrieve category information from Mongo collection {m_MongoCollection.CollectionName()}, database {m_MongoCollection.DatabaseName()}, server {m_MongoCollection.Server()}.");
                    }

                    return categories;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            finally
            {
                _semaphore.Release();
            }

        }

        public Category FindByCiaid(string ciaid)
        {
            var filter = $"{{ CIAID: '{ciaid}'}}";
            var awaiter = m_MongoCollection.FindAsync<CategoryFromMongo>(filter).GetAwaiter();
            CategoryFromMongo mongoCategory = awaiter.GetResult().Single();
            Category category = _mapper.Map<Category>(mongoCategory);
            return category;
        }

        public Category FindByTitle(string title)
        {
            var filter = $"{{ ttl: '{title}'}}";
            var awaiter = m_MongoCollection.FindAsync<CategoryFromMongo>(filter).GetAwaiter();
            CategoryFromMongo mongoCategory = awaiter.GetResult().Single();
            Category category = _mapper.Map<Category>(mongoCategory);
            return category;
        }

        public void Save(Category category)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
