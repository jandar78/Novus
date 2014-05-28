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
        void ParseMessage(string message);
        AI.FSM Fsm { get; set; }
        DateTime NextAiAction { get; set; }
    }
}
