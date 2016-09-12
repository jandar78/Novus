using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IState
    {
        void Execute(IActor actor, ITrigger trigger = null);
        void Enter(IActor actor);
        void Exit(IActor actor);
        string ToString();
    }
}
