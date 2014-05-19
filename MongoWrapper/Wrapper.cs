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
			catch (System.Net.Sockets.SocketException se) {
				Console.WriteLine("It would seem the Mongo Database is not running");
				throw;
			}
		}

		//if the _mongoDB object is null we have not instantiated it and connected to the database
		static public bool IsConnected() {
			return (_mongoDB != null);
		}

		static public MongoDatabase GetDatabase(string dbName) {
			MongoDatabase result = _mongoDB.GetDatabase(dbName);
			return result;
		}

        static public MongoCollection GetCollection(string dbName, string collection) {
            ConnectToDatabase();
            return GetDatabase(dbName).GetCollection(collection);
        }
	}
}
