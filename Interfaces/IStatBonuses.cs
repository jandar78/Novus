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
        Dictionary<string, Tuple<double, DateTime>> _bonus { get; set; }

        void Add(BonusTypes name, double amount, int time = 0);
        void Remove(BonusTypes name);
        BsonArray GetBson();
        void LoadFromBson(BsonArray array);
        double GetBonus(BonusTypes type);
        IMessage Cleanup();
        string ToString();
     }

    public class StatBonuses : IStatBonuses
    {
        public Dictionary<string, Tuple<double, DateTime>> _bonus
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(BonusTypes name, double amount, int time = 0)
        {
            throw new NotImplementedException();
        }

        public IMessage Cleanup()
        {
            throw new NotImplementedException();
        }

        public double GetBonus(BonusTypes type)
        {
            throw new NotImplementedException();
        }

        public BsonArray GetBson()
        {
            throw new NotImplementedException();
        }

        public void LoadFromBson(BsonArray array)
        {
            throw new NotImplementedException();
        }

        public void Remove(BonusTypes name)
        {
            throw new NotImplementedException();
        }

        public override string ToString() { return String.Empty; }
    }
}
