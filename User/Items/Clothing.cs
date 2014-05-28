﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items {
    public sealed partial class Items : Iitem, Iweapon, Iedible, Icontainer, Iiluminate, Iclothing, Ikey {

        public Wearable EquippedOn { get; set; }

        public decimal MaxDefense { get; set; }

        public decimal CurrentDefense { get; set; }

        public Dictionary<string, double> TargetDefenseEffects { get; set; }

        public Dictionary<string, double> PlayerDefenseEffects { get; set; }

        public Dictionary<string, double> WearEffects { get; set; }

    }
}
