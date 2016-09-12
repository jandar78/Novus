using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketProgram {
	class Program {
		static void Main(string[] args) {
			StringBuilder sb = new StringBuilder();
			Sockets.Client client = new Sockets.Client("127.0.0.1", 1301);
			Console.Title = "IP: " + client.IPAddress.ToString() + "   Port: " + client.Port + "   Status : " + (client.Connected == true ? "CONNECTED" : "DISCONNECTED");

			while (true) {

				if (Console.KeyAvailable){
					ConsoleKeyInfo key = Console.ReadKey();
					
					if (key.Key == ConsoleKey.Backspace && sb.Length > 0) {
						sb.Remove(sb.Length - 1, 1);
					}
					else if (key.Key != ConsoleKey.Enter) {
						sb.Append(key.KeyChar);
					}

					if (key.Key == ConsoleKey.Enter){
						Console.WriteLine("<<< " + sb.ToString());
						client.Send(sb.ToString());
						if (sb.ToString().Contains("-exit")) {
							break;
						}
						sb.Clear();
					}
				}

				if (client.Connected) {
					client.GetResponse();
				}
				else {
					//atempt to connect again
					Console.WriteLine("No server to connect to.");
					Console.Title = "IP: " + client.IPAddress.ToString() + "   Port: " + client.Port + "   Status : " + (client.Connected == true ? "CONNECTED" : "DISCONNECTED");
					break;
				}

			}

			if (client.Connected) {
				client.EndConnection();
			}
			Console.ReadKey();
		}
	}
}
