using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {
	public interface IAttributes {
        int Rank { get; set; }
        double Value { get; set; }
        string Name { get; set; }                
        double Max { get; set; }
        double RegenRate { get; set; }

        void ApplyEffect(double amount);
        void ApplyPositive(double amount);
        void ApplyNegative(double amount);
        void IncreaseMax(double amount);
        void DecreaseMax(double amount);
        void IncreaseRegen(double amount);
        void DecreaseRegen(double amount);
        void IncreaseRank();
        bool ApplyRegen();
        string ToString();  
	}   
}
