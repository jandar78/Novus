using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MySockets {
	public class Client {
		#region Public Properties
		public System.Net.IPAddress IPAddress {
			get {
				return _ipAddress;
			}
			set {
				_ipAddress = value;
			}
		}

		public int Port {
			get;
			set;
		}

		public Socket ClientSocket {
			get {
				return _clientSocket;
			}
			set {
				_clientSocket = value;
			}
		}

		public bool Connected {
			get;
			private set;
		}
		#endregion Public Properties

		#region Private Memebers
		private System.Net.IPAddress _ipAddress;
		private Socket _clientSocket;
		private byte[] _buffer;
		#endregion Private Members

		#region Constructors
		public Client(): this("192.168.1.1",8221) { }

		public Client(string address, int port) {
			System.Net.IPAddress.TryParse(address, out _ipAddress);
			this.Port = port;
			_buffer = new byte[1024];
			Connected = false;
			ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			ClientConnect();			
		}
		#endregion Cosntructors

		#region Public Methods
		public void EndConnection() {
			ClientSocket.Disconnect(true);
			Connected = false;
			Console.WriteLine(">>> Connection closed <<<");
		}

		public void Send(string message) {
			byte[] sendMessage = System.Text.Encoding.ASCII.GetBytes(message);
			try {
				ClientSocket.BeginSend(sendMessage, 0, sendMessage.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
			}
			catch (SocketException se) {
				if (se.SocketErrorCode  == SocketError.ConnectionReset) {
					Console.WriteLine(">>> Server is unavailable. Disconnecting <<<");
					EndConnection();
					return;
				}
			}
		}

		public void GetResponse() {
			byte[] buffer = new byte[1024];
			int received = 0;
				try {
					if (_clientSocket.Connected && _clientSocket.Available != 0) {
						received = _clientSocket.Receive(buffer, SocketFlags.None);
					}
				}
				catch (SocketException se) {
					if (se.SocketErrorCode == SocketError.ConnectionReset) {
						Console.WriteLine(">>> Server is unavailable. Disconnecting <<<");
						EndConnection();
						return;
					}
				}

				if (received == 0) {
					return;
				}

				byte[] data = new byte[received];
				Array.Copy(buffer, data, received);
				string text = System.Text.Encoding.ASCII.GetString(data);
				Console.WriteLine(">>> " + text);
		}
		#endregion Public Methods

		#region Private Methods
		private void ClientConnect() {
			
			ClientSocket.BeginConnect(new System.Net.IPEndPoint(IPAddress, Port), new AsyncCallback(ConnectCallback), null);
			Console.WriteLine(">>>Client connected to IP:" + IPAddress.ToString() + "   Port: " + Port + " <<<");
			Connected = true;
		}

		private void SendCallback(IAsyncResult Args) {
			try {
				ClientSocket.EndSend(Args);
			}
			catch (SocketException se) {
				if (se.SocketErrorCode == SocketError.ConnectionReset) {
					Console.WriteLine(">>>Server is unavailable. Disconnecting.<<<");
				}
			}
		}

		private void ConnectCallback(IAsyncResult Args) {
			try {
				ClientSocket.EndConnect(Args);
			}
			catch (SocketException se) {
				if (se.SocketErrorCode == SocketError.ConnectionRefused) {
					Console.WriteLine(">>>Server is unavailable<<<");
				}
			}
		}
		#endregion Private Methods
	}
}
