using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Interfaces;

namespace Character {
    /// <summary>
    /// This class can hold Bonus/Penalty for each type.  This way we can just offset bonuses and penalties at the core.
    /// We can display to the user for example that their DEX is -5% or Dodge is +10% instead of doing Bonus Dodge = 15% Penalty Dodge = -5%
    /// </summary>
   public class StatBonuses : IStatBonuses {

        public Dictionary<BonusTypes, StatBonus> Bonus { get; private set; }
        
        public StatBonuses()
        {
            Bonus = new Dictionary<BonusTypes, StatBonus>();
        }

        /// <summary>
        /// Adds an amount (positive or negative) to the type specified. Passing in zero or null for the time will make this bonus never expire.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        /// <param name="time"></param>
        public void Add(BonusTypes name, double amount, int time = 0) {
            if (Bonus.ContainsKey(name)) {
                Bonus[name].Amount += amount;
                Bonus[name].Time = time == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(time);
            }
            else {
                Bonus.Add(name, new StatBonus() { Name = name.ToString(), Amount = amount, Time = time == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(time) });
            }
        }

        public void Remove(BonusTypes name) {
            if (Bonus.ContainsKey(name)) {
                Bonus.Remove(name);
            }
        }

         public double GetBonus(BonusTypes type) {
            double bonus = 0.0d;
            if (Bonus.ContainsKey(type)) {
                bonus = Bonus[type].Amount;
            }

            return bonus;
        }

        /// <summary>
        /// Calling this will remove any bonuses or penalties whose time has expired.
        /// </summary>
        /// <returns></returns>
        public IMessage Cleanup() {
            List<string> messages = new List<string>();
            var bonusCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("Messages", "Bonuses");
            BsonDocument found = null;
            BsonArray array = null;
            IMongoQuery query = null;
            IMessage message = new Message();

            foreach (var item in Bonus) {
                if (item.Value.Time != DateTime.MaxValue && DateTime.Now >= item.Value.Time) {
                    query = Query.EQ("_id", item.Key);
                    found = MongoUtils.MongoData.RetrieveObject<BsonDocument>(bonusCollection, b => b["_id"] == item.Key);
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
                    Remove(item.Key);
                }
            }

            return message;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Bonus) {
                sb.AppendLine(string.Format("{0}: {1:p}",item.Key, item.Value.Amount));
            }

            return sb.ToString();
        }
        
    }

    public class StatBonus {
        public string Name { get; set; }
        public double Amount { get; set; }
        public DateTime Time { get; set; }
    }
}
