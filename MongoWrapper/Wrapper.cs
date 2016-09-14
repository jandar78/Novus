using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace MongoUtils
{

	public class MongoData {

		
		private static MongoClient _mongoClient;

		//Initializes the mongoDB object and connects to it
		static public void ConnectToDatabase() {
			if (_mongoClient == null) {
				MongoClient client = new MongoClient(); //connect to localhost
                _mongoClient = client;
			}
		}

		//if the _mongoDB object is null we have not instantiated it and connected to the database
		static public bool IsConnected() {
            IAsyncCursor<BsonDocument> cursor = null;
            if (_mongoClient != null) {
                //there's a chance we lost the connection but since no read/write occurred we are not aware yet so let's ping
                try {
                  cursor =  _mongoClient.ListDatabases();
                }
                catch {
                    return false;
                }
            }
            return (_mongoClient != null && cursor.Any());
		}

		static public IMongoDatabase GetDatabase(string dbName) {
			IMongoDatabase result = _mongoClient.GetDatabase(dbName);
			return result;
		}

        /// <summary>
        /// Evnetually the parameters for this should be converted to Enums and then we won't have any typos for the different collections
        /// and database names.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        static public IMongoCollection<T> GetCollection<T>(string dbName, string collection) {
            ConnectToDatabase();
            if (IsConnected()) {
                return GetDatabase(dbName).GetCollection<T>(collection);
            }

            return null;
        }

		static public async Task<List<BsonDocument>> GetCollections(string dbName) {
            IMongoDatabase db = GetDatabase(dbName);
            return await db.ListCollectionsAsync().Result.ToListAsync();
        }

        static public void RegisterMappings()
        {
            RegisterMappings();
        }

        static public Task<T> RetrieveObjectAsync<T>(string database, string collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            var db = GetDatabase(database);
            var col = db.GetCollection<T>(collection);
            return RetrieveObjectAsync<T>(col, filter);
        }

        static public Task<IEnumerable<T>> RetrieveObjectsAsync<T>(string database, string collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            var db = GetDatabase(database);
            var col = db.GetCollection<T>(collection);
            return RetrieveObjectsAsync<T>(col, filter);
        }

        static public T RetrieveObject<T>(string database, string collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            var db = GetDatabase(database);
            var col = db.GetCollection<T>(collection);
            return RetrieveObject<T>(col, filter);
        }

        static public IEnumerable<T> RetrieveObjects<T>(string database, string collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            var db = GetDatabase(database);
            var col = db.GetCollection<T>(collection);
            return RetrieveObjects<T>(col, filter);
        }

        static async public Task<T> RetrieveObjectAsync<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            return await collection.Find<T>(filter).FirstOrDefaultAsync();
        }

        static async public Task<IEnumerable<T>> RetrieveObjectsAsync<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            return await collection.Find<T>(filter).ToListAsync<T>();
        }

        static public T RetrieveObject<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            return collection.Find<T>(filter).FirstOrDefault();
        }

        static public IEnumerable<T> RetrieveObjects<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter) {
            return collection.Find<T>(filter).ToList<T>();
        }

        static async public Task<bool> SaveAsync<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter, T objectToSave) {
            var result = await collection.ReplaceOneAsync<T>(filter, objectToSave, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }

        static public bool Save<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, bool>> filter, T objectToSave) {
            var result = collection.ReplaceOne<T>(filter, objectToSave, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }

        static public void Insert<T>(IMongoCollection<T> collection, T objectToSave) {
            collection.InsertOne(objectToSave);
        }

        static async public void InsertAsync<T>(IMongoCollection<T> collection, T objectToSave) {
            await collection.InsertOneAsync(objectToSave);
        }

        static async public Task<IEnumerable<T>> FindAll<T>(IMongoCollection<T> collection) {
            return await collection.Find<T>(_ => true).ToListAsync<T>();
        }
    }
}
