using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items {
    //light source has a fuel source, torch would need cloth, a flashlight battery, etc.
    //a light would hold a maxcharge and the charge would go down as it's used.
    //by swapping out the fuel source the charge can be replenished
    //a source of ENDLESS means it never loses charge, a magic item or maybe a crystal that glows in the dark
    //Should the item have determine skill level necessary to recharge or should it be global for any items depending solely on fuel source type?
    //I like it per item honestly.  This could lead to better equipment needing higher skills to use properly.
    public interface Iiluminate {
        bool isLit { get; set; }
        bool isLightable { get; set; }
        bool isChargeable { get; set; }
        FuelSource fuelSource { get; set; }
        LightType lightType { get; set; }
        double maxCharge { get; set; }
        double currentCharge { get; set; }
        double chargeLowWarning { get; set; }
        double chargeDecayRate { get; set; } //per sec
        void Drain();
        List<string> Ignite(); //the commands will call these, but at command level the action has to be specific to the fuel source (ignite, activate, turn on, etc)
        List<string> Extinguish(); //same deal as Ignite
        void ReCharge(double chargeAmount); //replenishes fuelsource
        string ExamineCharge(); //this call should return information about the charge and fuel source and the value of isLit                          
    }

    public enum FuelSource { FUEL, BATTERY, SOLAR, HAND_CRANK, CLOTH, ENDLESS }
    public enum LightType { TORCH, FLASHLIGHT, LAMP, FLAME } //Flame type think of match or candle, non chargeable and goes quick or slow
}
