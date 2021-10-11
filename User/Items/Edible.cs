using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Interfaces;

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> AttributesAffected { get; set; }
    }
}