using Character;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IStatBonuses
    {
        Dictionary<BonusTypes, Bonus> Bonuses { get; set; }

        void Add(BonusTypes name, double amount, int time = 0);
        void Remove(BonusTypes name);
        //BsonArray GetBson();
        //void LoadFromBson(BsonArray array);
        double GetBonus(BonusTypes type);
        IMessage Cleanup();
        string ToString();
     }
}
