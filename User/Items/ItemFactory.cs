using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using Extensions;
using MongoDB.Bson.Serialization;

namespace Items {
    class ItemFactory {

        public static Iitem CreateItem(ObjectId id){
            BsonDocument tempItem = null;

            if (id != null) { //id got passed in so we are looking for a specific edible item
                MongoUtils.MongoData.ConnectToDatabase();
                MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
                MongoCollection itemCollection = db.GetCollection("Items");
                tempItem = itemCollection.FindOneAs<BsonDocument>(Query.EQ("_id", id));
            }

            //all items inherit Iitem, but some items inherit a second interface the factory should figure this all out and just return the item
            //as an Iitem than can be cast to whatever needs be by the caller.
            Iitem result = null;

            result = BsonSerializer.Deserialize<Items>(tempItem);       

            return result;
        }

        //public static Iitem CreateItem(ObjectId id) {
        //    ItemType type = (ItemType)Enum.Parse(typeof(ItemType), typeIn);
        //    return CreateItem(type, id);
        //}
    }
}
