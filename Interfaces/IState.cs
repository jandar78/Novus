using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IState
    {
        void Execute(INpc actor, ITrigger trigger = null);
        void Enter(INpc actor);
        void Exit(INpc actor);
        string ToString();
    }
}
