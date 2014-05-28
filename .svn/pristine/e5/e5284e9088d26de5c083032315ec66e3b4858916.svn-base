using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using User;

namespace MySockets
{
	public class Server {

		#region Public Properties
		public string IPAddress {
			get {
				return _ipAddress.ToString();
			}
		   set {
				System.Net.IPAddress.TryParse(value, out _ipAddress);
			}
		}

		public int Port {
			get;
			set;
		}

		private Socket ServerSocket {
			get {
				return _serverSocket;
			}
		 set {
				_serverSocket = value;
			}
		}

		private byte[] ReceiveBuffer {
			get {
				return _receiveBuffer;
			}
			set {
				_receiveBuffer = value;
			}
		}

		private byte[] SendBuffer {
			get {
				return _sendBuffer;
			}
			set {
				_sendBuffer = value;
			}
		}
		#endregion Public Properties

		#region Private members
		private Logger.Logger logger;
		private System.Net.IPAddress _ipAddress;
		private static Dictionary<Socket, User.User> clientSocketList;
		private Socket _serverSocket;
		private byte[] _receiveBuffer, _sendBuffer;
	   private static Server _server = null;
		#endregion Private Members

		#region Constructors
		//Main constructor
		private Server(string address, int port) {
			logger = new Logger.Logger("C:\\ServerLogs\\");
			this.IPAddress = address;
			this.Port = port;
			ReceiveBuffer = new byte[1024];
			clientSocketList = new Dictionary<Socket, User.User>();
			_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);	
		}
		#endregion Constructors

		#region Public Methods
		public static Server GetServer() {
			return _server ?? (_server = new Server("198.162.1.1", 8814));
		}


		//meh this method will probably have to change due to the async state that we want to take things
		public static List<User.User> GetCurrentUserList() {
			List<User.User> userList = new List<User.User>();
			foreach (KeyValuePair<Socket, User.User> user in clientSocketList) {
				userList.Add(user.Value); 
			}

			return userList;
		}

		public static User.User GetAUser(string id) {
			return clientSocketList.Where(c => c.Value.UserID == id).SingleOrDefault().Value;
		}

		public void StartServer() {
			try {
				this.ServerSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, Port));
				this.ServerSocket.Listen(1);
				logger.Log(">>>Server started at IP: " + IPAddress.ToString() + "   Port: " + Port + " <<<");
				this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
				logger.Log(">>>Server is listening for incoming connections<<<");
			}
			catch (SocketException se) {
				throw se;
			}
			catch (Exception ex) {
				throw ex; 
			}

		}

		public void ShutdownServer() {
			CloseAllConnections();
		}

		public void SendToAllClients(string message = null) {
			if (!string.IsNullOrEmpty(message)) {
				SendToAllClients(System.Text.Encoding.ASCII.GetBytes(message));
			}
			else {
				SendToAllClients(new byte[0]);
			}
		}

		public void SendToClients(User.User.UserState state, string message) {
			SendToClients(state, System.Text.Encoding.ASCII.GetBytes(message));
		}

		public void SendToClient(Socket socket, string message) {
			socket.Send(Encoding.ASCII.GetBytes(message));
			this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}
		#endregion Public Methods

		#region Private methods
		//this method will only send the message to users in a certain state (logging in, creating character, playing, etc.)
		//this will come in handy for the login and character creation portions
		private void SendToClients(User.User.UserState state, byte[] message) {
			List<Socket> matchesState = clientSocketList.Where(s => s.Value.CurrentState == state).Select(s => s.Key).ToList();
			foreach (Socket client in matchesState) {
				client.Send(message);
				logger.Log(client.RemoteEndPoint.ToString() + " >>> " + System.Text.Encoding.ASCII.GetString(message));
			}
			this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}

		private void SendToAllClients(byte[] message = null) {
			if (clientSocketList.Count > 0) {
				foreach (KeyValuePair<Socket, User.User> client in clientSocketList) {
					Socket currentSocket = client.Key;
					//	if (client.Value.CurrentState == User.User.UserState.Talking) { //we don't want to send system messages to users not already in the game
					if (message.Length == 0) {
						message = System.Text.Encoding.ASCII.GetBytes(client.Value.OutBuffer);
					}
					if (message.Length > 0) {
						currentSocket.Send(message);
						//	}
						logger.Log(currentSocket.RemoteEndPoint.ToString() + " >>> " + System.Text.Encoding.ASCII.GetString(message));
					}
				}
			}
			this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}

		private void AcceptCallback(IAsyncResult AR) {
			try {
				Socket socket = this.ServerSocket.EndAccept(AR);
				clientSocketList.Add(socket, new User.User());

				logger.Log("*** Client connected from IP: " + socket.RemoteEndPoint.ToString() + " ***");
				socket.BeginReceive(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
				this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
			}
			catch (SocketException se) {
				//just catch it and move on
			}
		}

		private void ReceiveCallback(IAsyncResult AR) {
			Socket socket = (Socket)AR.AsyncState;

			int received = 0;

			try {
				if (socket.Connected) {
					received = socket.EndReceive(AR);
				}
				else {
					CloseThisConnection(socket);
				}
			}
			catch (SocketException) {
				CloseThisConnection(socket);
				return;
			}
			catch (ObjectDisposedException) {
				//if it's been disposed then we are good to go continue on
				return;
			}

			if (received == 0) {
				return;
			}

			byte[] buffer = new byte[received];
			Array.Copy(ReceiveBuffer, buffer, received);

			clientSocketList[socket].InBuffer = System.Text.Encoding.ASCII.GetString(buffer);

			string temp = System.Text.Encoding.ASCII.GetString(buffer); //once you read it from the incoming buffer it's no longer there
			Console.WriteLine("<<< " + temp);
			logger.Log("<<< " + socket.RemoteEndPoint.ToString() + ":" + temp);  //need to fix issue where an extra newline is making it into the file
			//send server reply
			//	byte[] data = System.Text.Encoding.ASCII.GetBytes("Received: " + text);
			try {
				//		socket.Send(data);
				socket.BeginReceive(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
			}
			catch (SocketException se) {
				if (se.SocketErrorCode == SocketError.ConnectionReset) {
					CloseThisConnection(socket);
				}
			}
		}

		private void CloseThisConnection(Socket socket) {
			logger.Log("*** Client " + socket.RemoteEndPoint.ToString() + " disconnected. ***");
			if (clientSocketList.Count > 0) {
				if (clientSocketList.ContainsKey(socket)) {//we want to prevent silly exceptions
					CloseThisConnection(new KeyValuePair<Socket, User.User>(clientSocketList.Keys.Where(k => k == socket).Single(), clientSocketList[socket]));
				}
			}
		}

		private void CloseThisConnection(KeyValuePair<Socket, User.User> socket) {
			logger.Log("*** Client " + socket.Value + " disconnected. ***");
			try {
				socket.Key.Shutdown(SocketShutdown.Receive);
				socket.Key.Close();
			}
			catch (ObjectDisposedException) {
				//catch it and move on
			}
		}

		private void CloseAllConnections() {
			foreach (KeyValuePair<Socket, User.User> socket in clientSocketList) {
				CloseThisConnection(socket);
			}

			clientSocketList.Clear();
			logger.Log(">>>All connections closed<<<");
		}
		#endregion Private methods
	}
}
