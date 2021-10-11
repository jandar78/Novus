using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages {
	public partial class MessageBuffer  {
		#region Public Properties

		public string IncomingBuffer {
			get {
				string temp = "";
				if (_incomingBuffer.Count > 0) {
					temp = _incomingBuffer.Dequeue();
					temp = temp.Substring(0, temp.IndexOf('\n') + 1);
					temp = temp.Replace("\r\n", "");
				}

				return temp;
			}
			set {
				if (value.Contains("\b")) {
					if (_telnetBuffer.Length > 0) {
						string temp = value.Replace("\b", "");
						int bs = value.Length - temp.Length;
						if (bs > _telnetBuffer.Length) bs = _telnetBuffer.Length - 1;
                        _telnetBuffer.Remove(_telnetBuffer.Length - bs, bs);
                        
					}
					value = "";
				}
				_telnetBuffer.Append(value);
				if (_telnetBuffer.ToString().Contains("\r\n")) {
					Log(_telnetBuffer.ToString());
					_incomingBuffer.Enqueue(_telnetBuffer.ToString());
					_telnetBuffer.Clear();
				}
			}
		}

		public string IncomingBufferPeek {
			get {
				if (_incomingBuffer.Count > 0) {
					return _incomingBuffer.Peek();
				}
				else {
					return "";
				}
			}
		}

		public string IncomingTelnetBufferPeek {
			get {
				if (_telnetBuffer.Length > 0) {
					return _telnetBuffer.ToString();
				}
				else {
					return "";
				}
			}
		}

		public byte[] IncomingBytes {
			get {
				return System.Text.Encoding.ASCII.GetBytes(IncomingBuffer);
			}
			set {
				IncomingBuffer = System.Text.Encoding.ASCII.GetString(value);
			}
		}

		public bool IncomingReady {
			get {
				if (_incomingBuffer.Count > 0) {
					return true;
				}
				return false;
			}
			private set{
			}
		}

		public string LogId { get; set; }

		#endregion Public Properties

		#region Private Members
		private Queue<string> _incomingBuffer;
		private StringBuilder _telnetBuffer;
		#endregion Private Members

		public void Log(string message) {
		//	Server.GetServer().serverLogger.Log(this.LogId + ": " + message + "\t [" + DateTime.Now.ToLocalTime() + "]");
		}
	}
}
	

