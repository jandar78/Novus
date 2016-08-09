using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoUtils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB;
using CharacterEnums;
using Interfaces;

namespace Character {
    /// <summary>
    /// This class can hold Bonus/Penalty for each type.  This way we can just offset bonuses and penalties at the core.
    /// We can display to the user for example that their DEX is -5% or Dodge is +10% instead of doing Bonus Dodge = 15% Penalty Dodge = -5%
    /// </summary>
   public class StatBonuses : IStatBonuses {
        
        public Dictionary<string, Tuple<double, DateTime>> _bonus {
            get {
                if (_bonus == null) {
                    return new Dictionary<string, Tuple<double, DateTime>>();
                }
                return _bonus;
            }
            set {
                _bonus = value;
            }
        }
        
        /// <summary>
        /// Adds an amount (positive or negative) to the type specified. Passing in zero or null for the time will make this bonus never expire.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        /// <param name="time"></param>
        public void Add(BonusTypes name, double amount, int time = 0) {
            if (_bonus.ContainsKey(name.ToString())) {
                _bonus[name.ToString()] = new Tuple<double, DateTime>(_bonus[name.ToString()].Item1 + amount, time == 0 ? DateTime.MaxValue : _bonus[name.ToString()].Item2.AddSeconds(time));
            }
            else {
                _bonus.Add(name.ToString(), new Tuple<double, DateTime>(amount, time == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(time)));
            }
        }

        public void Remove(BonusTypes name) {
            if (_bonus.ContainsKey(name.ToString())) {
                _bonus.Remove(name.ToString());
            }
        }

        public BsonArray GetBson() {
            BsonArray bonuses = new BsonArray();

            BsonDocument values = new BsonDocument(){
                {"Name", ""},
                {"Amount",""},
                {"Time",""}
            };

            foreach (KeyValuePair<string, Tuple<double, DateTime>> item in _bonus) {
                values["Name"] = item.Key;
                values["Amount"] = (double)item.Value.Item1;
                values["Time"] = (DateTime)item.Value.Item2;

                bonuses.Add(values);
            }
                        
            return bonuses;
        }

        public void LoadFromBson(BsonArray array) {
            foreach (BsonDocument doc in array) {
                if (doc.ElementCount > 0) {
                    this.Add((BonusTypes)Enum.Parse(typeof(BonusTypes), doc["Name"].AsString), doc["Amount"].AsDouble, doc["Time"].AsInt32);
                }
            }

        }

        public double GetBonus(BonusTypes type) {
            double bonus = 0.0d;
            if (_bonus.ContainsKey(type.ToString())) {
                bonus = _bonus[type.ToString()].Item1;
            }

            return bonus;
        }

        /// <summary>
        /// Calling this will remove any bonuses or penalties whose time has expired.
        /// </summary>
        /// <returns></returns>
        public IMessage Cleanup() {
            List<string> messages = new List<string>();
            MongoCollection<BsonDocument> bonusCollection = MongoUtils.MongoData.GetCollection("Messages", "Bonuses");
            BsonDocument found = null;
            BsonArray array = null;
            IMongoQuery query = null;
            IMessage message = new Message();
            foreach (KeyValuePair<string, Tuple<double, DateTime>> item in _bonus) {
                if (item.Value.Item2 != DateTime.MaxValue && DateTime.Now >= item.Value.Item2) {
                    query = Query.EQ("_id", item.Key);
                    found = bonusCollection.FindOneAs<BsonDocument>(query).AsBsonDocument;
                    //let's add the messages that removing the bonus/penalty could have
                    array = found["Messages"][0]["Self"].AsBsonArray;
                    int choice = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, array.Count());

                    message.Self = array[choice].AsString;

                    array = found["Messages"][0]["Target"].AsBsonArray;
                    if (array.Count >= choice - 1) {
                        message.Target = array[choice].AsString;
                    }

                    array = found["Messages"][0]["Others"].AsBsonArray;
                    if (array.Count == choice - 1) {
                        message.Room = array[choice].AsString;
                    }

                    //remove the bonus/penalty
                    Remove((BonusTypes)Enum.Parse(typeof(BonusTypes),item.Key));
                }
            }

            return message;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Tuple<double, DateTime>> item in _bonus) {
                sb.AppendLine(string.Format("{0}: {1:p}",item.Key, item.Value.Item1));
            }

            return sb.ToString();
        }
        
    }
}
