using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items {
   public interface Iweapon {
        double MinDamage { get; set; }
        double MaxDamage { get; set; }
        double AttackSpeed { get; set; }
        bool IsWieldable { get; set; }
        double CurrentMinDamage { get; set; }
        double CurrentMaxDamage { get; set; }
        Dictionary<string, double> Wield();
       
       //even though these are called Bonus they may in fact do otherthings like drain wileding players endurance on each strike
       //or actually heal the target player (maybe for practice weapons?)
        Dictionary<string, double> TargetAttackEffects { get; set; }
        Dictionary<string, double> PlayerAttackEffects { get; set; }
        Dictionary<string, double> WieldEffects { get; set; }
    }
}
