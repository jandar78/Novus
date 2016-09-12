using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;

namespace Interfaces {
   public interface IClothing {
        Wearable EquippedOn { get; set; }
        double MaxDefense { get; set; }
        double CurrentDefense { get; set; }
        void Wear();

        Dictionary<string, double> TargetDefenseEffects { get; set; }
        Dictionary<string, double> PlayerDefenseEffects { get; set; }
        Dictionary<string, double> WearEffects { get; set; }
    }
}
