using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commands {
	public partial class CommandParser {
		//The group commands will basically follow this following format: 
		//group create The Ragtag Squad
		//group invite willy wonka
		//group disband
		//this will further parse the command line and call the appropriate group commands
		public static void Group(User.User player, List<string> commands) {
			bool inGroup = !string.IsNullOrEmpty(player.GroupName);
			string name = RemoveWords(commands[0]);

			if (commands.Count > 2) {
				switch (commands[2]) {
					case "create":
						Groups.Groups.GetInstance().CreateGroup(player.UserID, name);
						break;
					case "disband":
						Groups.Groups.GetInstance().DisbandGroup(player.UserID, player.GroupName);
						break;
					case "accept":
						Groups.Groups.GetInstance().AcceptDenyJoinRequest(name, player.UserID, true);
						break;
					case "deny":
						Groups.Groups.GetInstance().AcceptDenyJoinRequest(name, player.UserID, false);
						break;
					case "promote":
						string promotedID = MySockets.Server.GetAUserByFullName(name).UserID;
						Groups.Groups.GetInstance().PromoteToLeader(player.UserID, player.GroupName, promotedID);
						break;
					case "join":
						Groups.Groups.GetInstance().Join(player.UserID, name);
						break;
					case "invite":
						Groups.Groups.GetInstance().InviteToGroup(player.UserID, name, player.GroupName);
						break;
					case "decline":
						Groups.Groups.GetInstance().DeclineInvite(player.UserID, name);
						break;
					case "uninvite":
						Groups.Groups.GetInstance().Uninvite(player.UserID, name, player.GroupName);
						break;
					default:
						break;
				}
			}
			else {
				player.MessageHandler("Anything in particular you want to do with a group?");
			}
		}

		//Strips away the first two words which should be  "group" followed by the action word like "create" , "disband", "approve", "deny", etc.
		private static string RemoveWords(string fullCommand) {
			for (int currentWord = 1; currentWord <= 2; currentWord++) {
				fullCommand = fullCommand.Substring(fullCommand.IndexOf(' ')); 
			}
						
			return fullCommand;
		}
	}
}
