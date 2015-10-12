using ClientHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Character {
    interface Inpc {
        void Update();
        void CalculateXP();
        void IncreaseXPReward(string id, double damage);
		void DecreaseXPReward(double amount);
        void ParseMessage(Message message);
        AI.FSM Fsm { get; set; }
        DateTime NextAiAction { get; set; }
    }
}
