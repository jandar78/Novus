using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Commands;

namespace ServerConsole {
	public class Program {
		static void Main(string[] args) {
			Console.Title = "Server";

			//start the Mongodatabase if the server is running
			//Todo: Update the path to where the DB will eventually be when this releases.

         System.Diagnostics.Process.Start(@"C:\MongoDB\bin\Mongod.exe");

			Console.WriteLine(">>> MongoDB initialized <<<");

			ClientHandling.MessageBuffer messageHandler = new ClientHandling.MessageBuffer();

			MySockets.Server server = MySockets.Server.GetServer();
			Scripts.Login loginScript = Scripts.Login.GetLoginScript();

			server.IPAddress = "127.0.0.1";
			server.Port = 1301;

			try {
				server.StartServer();
				StringBuilder sb = new StringBuilder();

				while (true) {
					if (Console.KeyAvailable) {
						ConsoleKeyInfo key = Console.ReadKey();
						
						if (key.Key == ConsoleKey.Backspace && sb.Length > 0){ 
							sb.Remove(sb.Length - 1, 1);
						}
						else if (key.Key != ConsoleKey.Enter) {
							sb.Append(key.KeyChar);
						}

						if (key.Key == ConsoleKey.Enter) {
							Console.WriteLine(">>> " + sb.ToString());
							if (sb.ToString().Contains("-exit")) {
								break;
							}
							else {
								server.SendToAllClients(sb.ToString());
							}
							sb.Clear();
						}
					}

					if (MySockets.Server.GetCurrentUserList().Count > 0) {
						int index = 0;
						foreach (User.User user in MySockets.Server.GetCurrentUserList()) {
							if (user.CurrentState == User.User.UserState.Talking && user.InBufferReady) {
								CommandParser.ParseCommands(user); 
							}

						   if (user.CurrentState == User.User.UserState.JustConnected) {
								loginScript.AddUserToLogin(MySockets.Server.GetCurrentUserList().ElementAt(index));
								user.CurrentState = User.User.UserState.LoggingIn; 
							}

							if (user.CurrentState == User.User.UserState.LoggingIn) {
								string temp = loginScript.ExecuteScript(user.UserID);
								if (!string.IsNullOrEmpty(temp)) {
									user.OutBuffer = temp;
								}
								 
								if (user.InBufferReady) {
									user.CurrentState = loginScript.InsertResponse(user.InBuffer, user.UserID);  
								}
							}
							
							if (user.CurrentState == User.User.UserState.CreatingCharacter) {
								//todo: add user to the character creation list to go through the wizard
							}
							server.SendToAllClients();
							index++;
						}
					}
				}

				Console.WriteLine(">>> Server shutting down <<<");
				server.ShutdownServer();
			}
			catch (SocketException se) {
				Console.WriteLine(se.Message);
			}
			catch (Exception ex) {
				Console.WriteLine("Whoa! : " + ex.Message);
			}
			Console.ReadLine();
		}
	}
}
