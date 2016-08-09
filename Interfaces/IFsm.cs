using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IFsm
    {

        Dictionary<string, IState> cachedStates { get; set; }
        IState state { get; set; }
        IState globalState { get; set; }
        IState previousState { get; set; }

        void CacheStates();
        IState GetStateFromName(string stateName);
        void ChangeState(IState newState, INpc Actor);
        void RevertState();
        void Update(INpc Actor);
        void InterpretMessage(IMessage message, IActor actor);
    }
}

