using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {

        public Wearable EquippedOn { get; set; }

        public double MaxDefense { get; set; }

        public double CurrentDefense { get; set; }

        public Dictionary<string, double> TargetDefenseEffects { get; set; }

        public Dictionary<string, double> PlayerDefenseEffects { get; set; }

        public Dictionary<string, double> WearEffects { get; set; }

    }
}
