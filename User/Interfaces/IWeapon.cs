using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IWeapon
    {
        double MinDamage { get; set; }
        double MaxDamage { get; set; }
        double AttackSpeed { get; set; }
        bool IsWieldable { get; set; }
        double CurrentMinDamage { get; set; }
        double CurrentMaxDamage { get; set; }
              
        Dictionary<string, double> WieldEffects { get; set; }
        Dictionary<string, double> TargetAttackEffects { get; set; }
        Dictionary<string, double> PlayerAttackEffects { get; set; }
        Dictionary<String, double> Wield();
    }

    public enum WeaponType { BLADE, BLUNT, POLE } //a few type of weapons


}

