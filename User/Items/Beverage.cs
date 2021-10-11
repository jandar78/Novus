using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Interfaces;

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {
        public EdibleType EdibleType { get; set; }
        
        public void GetAttributesAffected(BsonArray attributesToAffect) {
            foreach (BsonDocument affect in attributesToAffect) {
                AttributesAffected.Add(affect["k"].AsString, affect["v"].AsDouble);
            }
        }

        public Dictionary<string, double> Consume() {
            OnConsumed(new ItemEventArgs(ItemEvent.CONSUME, this.Id));
            return AttributesAffected;
        }
    }
}
