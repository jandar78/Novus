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

        public double MinDamage { get; set; }
        public double MaxDamage { get; set; }
        public double AttackSpeed { get; set; }
        public bool IsWieldable { get; set; }
        public double CurrentMinDamage { get; set; }
        public double CurrentMaxDamage { get; set; }
        
        //Contains what effects affect who and in which case
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> WieldEffects { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> TargetAttackEffects { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> PlayerAttackEffects { get; set; }

        //TODO: this method should return any bonuses or curses that will be applied to the player once equipped
        public Dictionary<String, double> Wield() {
            Dictionary<string, double> result = new Dictionary<string, double>();
            OnWielded(new ItemEventArgs(ItemEvent.WIELD, this.Id));
            return result;
        }
    }
}
