using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using User;
using Character;

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
		public Logger.Logger serverLogger;
		private System.Net.IPAddress _ipAddress;
		private static ConcurrentDictionary<Socket, User.User> clientSocketList;
		private Socket _serverSocket;
		private byte[] _receiveBuffer, _sendBuffer;
	   private static Server _server = null;
		#endregion Private Members

		#region Constructors
		//Main constructor
		private Server(string address, int port) {
			serverLogger = new Logger.Logger("C:\\ServerLogs\\");
			this.IPAddress = address;
			this.Port = port;
			ReceiveBuffer = new byte[1024];
			clientSocketList = new ConcurrentDictionary<Socket, User.User>();
			_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);	
		}
		#endregion Constructors

		#region Public Methods
		public static Server GetServer() {
			return _server ?? (_server = new Server("192.168.1.1", 8814));
		}

		public static List<User.User> GetCurrentUserList() {
			List<User.User> userList = new List<User.User>();
			Dictionary<Socket, User.User> tempList = new Dictionary<Socket,User.User>(clientSocketList); //this should prevent "Collection was modified" exception
			foreach (KeyValuePair<Socket, User.User> user in tempList) {
				userList.Add(user.Value); 
			}

			return userList;
		}

		public static bool UpdateUserSocket(string userID) {
			//this is messy but basically we are grabbing all the socket that have a value whos user ID is equal to the one passed in
			//then we are going to update the value of the socket who's user is currently logging in with the user that is sitting in limbo
			//and then we are going to get rid of the socket/user pair that are still in limbo
			List<KeyValuePair<Socket, User.User>> check = clientSocketList.Where(u => u.Value.UserID == userID).ToList();
			KeyValuePair<Socket, User.User> newClient = check.Where(c => c.Value.CurrentState == User.User.UserState.LOGGING_IN).SingleOrDefault();
			KeyValuePair<Socket, User.User> oldClient = check.Where(c => c.Value.CurrentState == User.User.UserState.LIMBO).SingleOrDefault();
			if (!clientSocketList.TryUpdate(newClient.Key, oldClient.Value, newClient.Value)){
				return false;
			}

			GetServer().CloseThisConnection(oldClient);
			clientSocketList[newClient.Key].CurrentState = User.User.UserState.LOGGING_IN;
	
			return true;
		}

		public static User.User GetAUserPlusState(string id, User.User.UserState state = User.User.UserState.TALKING) {
			Dictionary<Socket, User.User> tempList = new Dictionary<Socket, User.User>(clientSocketList);
			return tempList.Where(c => c.Value.UserID == id && c.Value.CurrentState == state).FirstOrDefault().Value;
		}

		public static User.User GetAUser(string id) {
            if (clientSocketList != null) {
                Dictionary<Socket, User.User> tempList = new Dictionary<Socket, User.User>(clientSocketList);
                return tempList.Where(c => c.Value.UserID == id).FirstOrDefault().Value;
            }

            return null;
		}

        public static User.User GetAUserFromList(List<string> id) {
            if (id.Count > 0) {
                return GetAUser(id[0]);
            }

            return null;
        }

        public static List<User.User> GetAUserByName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            Dictionary<Socket, User.User> tempList = new Dictionary<Socket, User.User>(clientSocketList);
            List<KeyValuePair<Socket, User.User>> temp = tempList.Where(c => c.Value.Player.FirstName.ToLower() == name.ToLower()).ToList();
            List<User.User> result = new List<User.User>();
            
            foreach (KeyValuePair<Socket, User.User> user in temp) {
                result.Add(user.Value);
            }
            return result;
        }

		public void StartServer() {
			try {
				this.ServerSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(IPAddress), Port));
				this.ServerSocket.Listen(1);
				serverLogger.Log(">>>Server started at IP: " + _ipAddress + "   Port: " + Port + " <<<");
				this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
				serverLogger.Log(">>>Server is listening for incoming connections<<<");
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
		//this method is not responsible for checking a characters state, that is up to the method that calls this
		private void SendToClients(User.User.UserState state, byte[] message) {
			List<Socket> matchesState = clientSocketList.Where(s => s.Value.CurrentState == state).Select(s => s.Key).ToList();
			foreach (Socket client in matchesState) {
				client.Send(message);
				serverLogger.Log(client.RemoteEndPoint.ToString() + " >>> " + System.Text.Encoding.ASCII.GetString(message));
			}
			this.ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}

		private void SendToAllClients(byte[] message = null) {
			if (clientSocketList.Count > 0) {
				Dictionary<Socket, User.User> temp = new Dictionary<Socket, User.User>(clientSocketList);
				foreach (KeyValuePair<Socket, User.User> client in temp) {
					Socket currentSocket = client.Key;
					//let's get rid of any pending sockets that did not get removed from the clientSocketList
					if (client.Value.CurrentState == User.User.UserState.DISCONNECT) {
						CloseThisConnection(client);
						continue;
					}

					//poll the socket to see if the user is still connected 

					if ((currentSocket.Poll(1000, SelectMode.SelectRead) && currentSocket.Available == 0) || !currentSocket.Connected || client.Value.CurrentState == User.User.UserState.LIMBO) {
						//ok this guy disconnected we'll apply a time stamp and if it's been XX minutes since he was last disconnected
						//then we'll save his character and drop this socket from the clientSocketList
						if (client.Value.LastDisconnected == DateTime.MinValue) {
							client.Value.LastDisconnected = DateTime.Now;
							client.Value.CurrentState = User.User.UserState.LIMBO;
						}
						else {
							TimeSpan idle = client.Value.LastDisconnected - DateTime.Now;
							if (idle.Minutes < -10) { //Todo: we should get this value from the database
								client.Value.Player.Save(); 
								CloseThisConnection(client);
								continue;
							}
						}
					}

					//this socket is still connected let's send a message 
					if (message.Length == 0) {
						//this message is coming from the user OutBuffer
						string text = client.Value.OutBuffer;
						if (!String.IsNullOrEmpty(text)) {
							string prompt = "";
							if (client.Value.CurrentState == User.User.UserState.TALKING) {
								prompt = "HP:" + client.Value.Player.GetAttributeColorized("Hitpoints") + "\\EN:" + client.Value.Player.GetAttributeColorized("Endurance") + ">" + client.Value.telnetBufferPeek;
							}
							currentSocket.Poll(1, SelectMode.SelectRead); 
							if (currentSocket.Connected) {
								try {
									currentSocket.Send(System.Text.Encoding.ASCII.GetBytes("\r" + text + prompt));
								}
								catch (SocketException) {
									//so somehow the previous polls didn't let us know the socket was closed but now we know for sure
									client.Value.CurrentState = User.User.UserState.LIMBO;
								}
							}
						}
					}
					else {
						//this message is coming from the server
						currentSocket.Send(message);
						serverLogger.Log(currentSocket.RemoteEndPoint.ToString() + " >>> " + System.Text.Encoding.ASCII.GetString(message));
					}
				}
			}
		}

		private void AcceptCallback(IAsyncResult AR) {
			try {
				Socket socket = this.ServerSocket.EndAccept(AR);
				if (!clientSocketList.TryAdd(socket, new User.User())) { //let's try to add to the dictionary and then try again
					clientSocketList.TryAdd(socket, new User.User());
				}
				serverLogger.Log("*** Client connected from IP: " + socket.RemoteEndPoint.ToString() + " ***");
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
			try {
				socket.BeginReceive(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
			}
			catch (SocketException se) {
				if (se.SocketErrorCode == SocketError.ConnectionReset) {
					CloseThisConnection(socket);
				}
			}
		}

		private void CloseThisConnection(Socket socket) {
			serverLogger.Log("*** Client " + socket.RemoteEndPoint.ToString() + " disconnected. ***");
			if (clientSocketList.Count > 0) {
				if (clientSocketList.ContainsKey(socket)) {//we want to prevent silly exceptions
					clientSocketList[socket].CurrentState = User.User.UserState.NONE; //set it to none when exiting  
					CloseThisConnection(new KeyValuePair<Socket, User.User>(clientSocketList.Keys.Where(k => k == socket).Single(), clientSocketList[socket]));
				}
			}
		}

		private void CloseThisConnection(KeyValuePair<Socket, User.User> socket) {
			try {
				User.User user;
				serverLogger.Log("*** Client " + socket.Value + " disconnected. ***");
				
				if (!clientSocketList.TryRemove(socket.Key, out user)) {
					user.CurrentState = User.User.UserState.DISCONNECT;
					return;
				}
				socket.Key.Close();
				socket.Key.Dispose();
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
			serverLogger.Log(">>>All connections closed<<<");
		}
		#endregion Private methods
	}
}
