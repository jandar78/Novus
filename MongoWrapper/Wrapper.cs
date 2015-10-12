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

		
		private static MongoServer _mongoDB;

		//Initializes the mongoDB object and connects to it
		static public void ConnectToDatabase() {
			if (_mongoDB == null) {
				MongoClient client = new MongoClient(); //connect to localhost

				_mongoDB = client.GetServer();
			}
            try {
                _mongoDB.Connect();
            }
            catch (Exception se) {//no reason to throw here really 
            }
		}

		//if the _mongoDB object is null we have not instantiated it and connected to the database
		static public bool IsConnected() {
            if (_mongoDB != null) {
                //there's a chance we lost th eocnnection but since no read/write occurred we ar enot aware yet so let's ping
                try {
                    _mongoDB.Ping();
                }
                catch {
                    return false;
                }
            }
            return (_mongoDB != null && _mongoDB.State == MongoServerState.Connected);
		}

		static public MongoDatabase GetDatabase(string dbName) {
			MongoDatabase result = _mongoDB.GetDatabase(dbName);
			return result;
		}

        /// <summary>
        /// Evnetually the parameters for this should be converted to Enums and then we won't have any typos for the different collections
        /// and database names.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        static public MongoCollection GetCollection(string dbName, string collection) {
            ConnectToDatabase();
            if (IsConnected()) {
                return GetDatabase(dbName).GetCollection(collection);
            }

            return null;
        }

		public static List<MongoCollection> GetCollections(string dbName) {
			List<MongoCollection> collections = new List<MongoCollection>();
			MongoDatabase db = GetDatabase(dbName);
			foreach (var collectionName in db.GetCollectionNames()) {
				collections.Add(db.GetCollection(collectionName));
			}

			return collections;
		}
	}
}
