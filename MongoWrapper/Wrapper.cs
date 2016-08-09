using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
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
        static public IMongoCollection<BsonDocument> GetCollection(string dbName, string collection) {
            ConnectToDatabase();
            if (IsConnected()) {
                return GetDatabase(dbName).GetCollection<BsonDocument>(collection);
            }

            return null;
        }

		static public List<IMongoCollection<BsonDocument>> GetCollections(string dbName) {
			List<IMongoCollection<BsonDocument>> collections = new List<IMongoCollection<BsonDocument>>();
			IMongoDatabase db = GetDatabase(dbName);
            IAsyncCursor<BsonDocument> collectionNames = db.ListCollections();
			foreach (var collectionName in collectionNames.ToList()) {
				collections.Add(db.GetCollection<BsonDocument>(collectionName.Names.First().ToString()));
			}

			return collections;
		}

        static public void RegisterMappings()
        {
            ClassMapper.RegisterMappings();
        }
	}
}
