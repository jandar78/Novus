using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public enum UserState { NONE, JUST_CONNECTED, LOGGING_IN, CREATING_CHARACTER, LEVEL_UP, TALKING, LIMBO, DISCONNECT, };

    public interface IUser
    {
        bool HourFormat24 { get; set; }
        string UserID { get; set; }
        DateTime LastDisconnected { get; set; }
        string GroupName { get; set; }
        IActor Player { get; set; }
        UserState CurrentState { get; set; }
        bool LoginCompleted { get; set; }
        List<string> FriendsList { get; set; }
        string LogID { get; set; }
        string OutBuffer { get; set; }
        string InBuffer { get; set; }
        string InBufferPeek { get; }
        string telnetBufferPeek { get; }
        bool InBufferReady { get; }

        void MessageHandler(IMessage message);
        void MessageHandler(string message);
    }
}

