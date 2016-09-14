using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Commands;
using MudTime;
using Interfaces;
using Messages;

namespace ServerConsole {
	public class Program {

		//this shouldn't be a static class eventually.
		public static void Main(string[] args) {
			Console.Title = "Novus MUD Server 0.7";
			string databasePath;
            string ipAddress;
            string port;
			//start the Mongodatabase if the server is running
			try {
				ParseConfigFile(out databasePath, out ipAddress, out port);
			}
			catch (FileNotFoundException) {
				Console.WriteLine("Settings.cfg file not found in current directory.  Server shutting down.");
				return;
			}

			System.Diagnostics.Process.Start(databasePath);

			Console.WriteLine(">>> MongoDB initialized <<<");

			MessageBuffer messageHandler = new MessageBuffer("Server");

			Sockets.Server server = Sockets.Server.GetServer(ipAddress, int.Parse(port));

            //get script singletons
            Scripts.Login loginScript = Scripts.Login.GetScript();
			Scripts.CreateCharacter CreationScript = Scripts.CreateCharacter.GetScript();
			Scripts.LevelUpScript levelUpScript = Scripts.LevelUpScript.GetScript();

			Commands.CommandParser.LoadUpCommandDictionary();
			Character.NPCUtils npcUtils = Character.NPCUtils.GetInstance();
			//npcUtils.LoadNPCs();

			MudTimer.StartUpTimers();

			try {
				server.StartServer();

				Console.WriteLine(">>> Listening at IP: " + server.IPAddress + " <<<");
				Console.WriteLine(">>> Port: " + server.Port + " <<<");

				StringBuilder sb = new StringBuilder();
				double speed = 0.0D;

				while (true) {
					if (Console.KeyAvailable) {
						ConsoleKeyInfo key = Console.ReadKey();

						if (key.Key == ConsoleKey.Backspace && sb.Length > 0) {
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
					try {
						//run NPC AI on separate thread, yes even if no players are playing.
						npcUtils.ProcessAIForNPCs();
						
						if (Sockets.Server.GetCurrentUserList().Count > 0) {
							int index = 0;
							System.Diagnostics.Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();

                            //Players actively playing
							foreach (IUser user in Sockets.Server.GetCurrentUserList()) {
								if (user.CurrentState == UserState.TALKING || user.CurrentState == UserState.LIMBO) {
									CommandParser.ParseCommands(user);
								}

                                //Players who just connected
								else if (user.CurrentState == UserState.JUST_CONNECTED) {
									//just connected let's make them login
									loginScript.AddUserToScript(Sockets.Server.GetCurrentUserList().ElementAt(index));
									user.CurrentState = UserState.LOGGING_IN;
								}

								 //the player should not receive any messages while in the level up script
								else if (user.CurrentState == UserState.LEVEL_UP) {
									if (user.CurrentState == UserState.LEVEL_UP) {
										levelUpScript.AddUserToScript(Sockets.Server.GetCurrentUserList().ElementAt(index));
									}

									string temp = levelUpScript.ExecuteScript(user.UserID);

									user.MessageHandler(temp);


									if (user.InBufferReady && user.CurrentState != UserState.TALKING) {
										user.CurrentState = levelUpScript.InsertResponse(user.InBuffer, user.UserID);
									}									
								}

                                //Players singing in to their character
								else if (user.CurrentState == UserState.LOGGING_IN) {
									//they are in the middle of the login process
									string temp = loginScript.ExecuteScript(user.UserID);
									if (!string.IsNullOrEmpty(temp)) {
										if (temp.Contains("Welcome"))
											temp += "\n\n\r";
										user.MessageHandler(temp);
									}

									if (user.InBufferReady && user.CurrentState != UserState.TALKING) {
										user.CurrentState = loginScript.InsertResponse(user.InBuffer, user.UserID);
									}

									if (user.CurrentState == UserState.CREATING_CHARACTER) {
										CreationScript.AddUserToScript(Sockets.Server.GetCurrentUserList().ElementAt(index));
									}
								}

                                //Players creating a new character
								else if (user.CurrentState == UserState.CREATING_CHARACTER) {
									string temp = CreationScript.ExecuteScript(user.UserID);
									if (!string.IsNullOrEmpty(temp)) {
										if (temp.Contains("Welcome"))
											temp += "\n\n\r";
										user.MessageHandler(temp);
									}

									if (user.InBufferReady && user.CurrentState != UserState.TALKING) {
										user.CurrentState = CreationScript.InsertResponse(user.InBuffer, user.UserID);
									}
								}
								//on this method not sure if I want to display one line per each call of this or display everything to the user 
								//until their outbuffer is empty.  It works this way as of now.
								server.SendToAllClients();
								index++;
								stopWatch.Stop();
								if ((double)stopWatch.Elapsed.TotalSeconds > speed) {
									speed = (double)stopWatch.Elapsed.TotalSeconds;
									Console.WriteLine(String.Format("Slowest: {0}", stopWatch.Elapsed));
								}
							}
						}
					}
					catch (Exception ex) {
						IUser temp = new Sockets.User(true);
						temp.UserID = new MongoDB.Bson.ObjectId("Internal");
						// CommandParser.ReportBug(temp, new List<string>(new string[] { "Bug Internal Error: " + ex.Message + "\n" + ex.StackTrace }));
					}
				}

				Console.WriteLine(">>> Server shutting down <<<");
				server.ShutdownServer();
			}
			catch (SocketException se) {
				Console.WriteLine(se.Message);
			}
			catch (Exception ex) {
				Console.WriteLine("Whoa! : " + ex.Message + "\n\n" + ex.StackTrace);
			}
			Console.ReadLine();
		}

		/// <summary>
		/// This file should parse the config file for any preliminary settings that it may need.  As of now I can only think of the database path
		/// after that it should be able to get everything from the database.  If any thing else comes up we'll add it here and modify this parser.
		/// </summary>
		/// <returns></returns>
		private static void ParseConfigFile(out string databasePath, out string ipAddress, out string port) {
			databasePath = ipAddress = port = string.Empty;
			string filePath = Directory.GetCurrentDirectory() + "\\settings.cfg";
			if (File.Exists(filePath)) {
				string configFile = File.ReadAllText(filePath);
                if (!configFile.Contains("ServerPath=") || !configFile.Contains("IpAddress=") || !configFile.Contains("Port=")) {
                    throw new Exception("Config file is not properly formatted or missing settings");
                }
					//find the serverpath line, and then just get ther server path.
				databasePath = configFile.Substring(configFile.IndexOf("ServerPath="));
				databasePath = databasePath.Replace("ServerPath=", "");
				databasePath = databasePath.Substring(0, databasePath.IndexOf(";"));

                ipAddress = configFile.Substring(configFile.IndexOf("IpAddress="));
                ipAddress = ipAddress.Replace("IpAddress=", "");
                ipAddress = ipAddress.Substring(0, ipAddress.IndexOf(";"));

                port = configFile.Substring(configFile.IndexOf("Port="));
                port = port.Replace("Port=", "");
                port = port.Substring(0, port.IndexOf(";"));
            }
			else {
				throw new FileNotFoundException();
			}
		}
	}
}
