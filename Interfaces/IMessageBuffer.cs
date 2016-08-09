using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IMessageBuffer
    {
        string IncomingBuffer { get; set; }
        string IncomingBufferPeek { get; set; }
        string IncomingTelnetBufferPeek { get; set; }
        byte[] IncomingBytes { get; set; }
        bool IncomingReady { get; set; }
        string LogId { get; set; }
        Queue<string> _incomingBuffer { get; set; }
        StringBuilder _telnetBuffer { get; set; }
        void Log(string message);

        string OutgoingBuffer { get; set; }
        byte[] OutgoingBytes { get; set; }
        Queue<string> _outgoingBuffer { get; set; }
        void MessageBuffer(string id);
        string Format(string input);
    }
}
