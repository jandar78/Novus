using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Sockets;

namespace Commands {
	public partial class CommandParser {
		//The group commands will basically follow this following format: 
		//group create The Ragtag Squad
		//group invite willy wonka
		//group disband
		//this will further parse the command line and call the appropriate group commands
		public static void Group(IUser player, List<string> commands) {
			bool inGroup = !string.IsNullOrEmpty(player.GroupName);
			string name = RemoveWords(commands[0]);
			IUser user = null;
			if (commands.Count > 2) {
				switch (commands[2]) {
					case "create":
						Groups.Groups.GetInstance().CreateGroup(player.UserID, name);
						break;
					case "disband":
						Groups.Groups.GetInstance().DisbandGroup(player.UserID, player.GroupName);
						break;
					case "accept":
						Groups.Groups.GetInstance().AcceptDenyJoinRequest(player.UserID, name, true);
						break;
					case "deny":
						Groups.Groups.GetInstance().AcceptDenyJoinRequest(player.UserID, name, false);
						break;
					case "promote":
						user = Server.GetAUserByFullName(name); 
						if (user == null){
							user = Server.GetAUserByFirstName(name).FirstOrDefault(); 
						}

						if (user != null) {
							Groups.Groups.GetInstance().PromoteToLeader(player.UserID, player.GroupName, user.UserID);
						}
						else {
							player.MessageHandler("No player by that name was found.  If you only used a first name try including the last name as well.");
						}
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
					case "kick":
					case "remove":
						Groups.Groups.GetInstance().RemovePlayerFromGroup(player.UserID, name, player.GroupName);
						break;
					case "list":
						if (string.IsNullOrEmpty(name) || name.ToLower() == "all") {
							player.MessageHandler(Groups.Groups.GetInstance().GetGroupNameOnlyList());
						}
						else {
							player.MessageHandler(Groups.Groups.GetInstance().GetAllGroupInfo(name));
						}
						break;
					case "request":
						Groups.Groups.GetInstance().RequestGroupJoin(player.UserID, name);
						break;
					case "master":
						user = Server.GetAUserByFullName(name);
						if (user == null) {
							user = Server.GetAUserByFirstName(name).FirstOrDefault();
						}
						if (user == null) {
							player.MessageHandler("No player by that name was found.  If you only used a first name try including the last name as well.");
						}
						else {
							Groups.Groups.GetInstance().AssignMasterLooter(player.UserID, name, player.GroupName);
						}
						break;
					case "lootrule":
						Groups.GroupLootRule newRule = Groups.GroupLootRule.Leader_only;
						switch (commands[3]) {
							case "master":
								newRule = Groups.GroupLootRule.Master_Looter;
								break;
							case "leader":
								newRule = Groups.GroupLootRule.Leader_only;
								break;
							case "chance":
								newRule = Groups.GroupLootRule.Chance_Loot;
								break;
							case "first":
								newRule = Groups.GroupLootRule.First_to_loot;
								break;
							case "next":
								newRule = Groups.GroupLootRule.Next_player_loots;
								break;
						}
						Groups.Groups.GetInstance().ChangeLootingRule(player.UserID, player.GroupName, newRule);
						break;
					case "joinrule":
						Groups.GroupJoinRule joinRule = Groups.GroupJoinRule.Friends_only;
						if (commands.Count > 3) {
							switch (commands[3]) {
								case "friends":
									joinRule = Groups.GroupJoinRule.Friends_only;
									break;
								case "open":
									joinRule = Groups.GroupJoinRule.Open;
									break;
								case "request":
									joinRule = Groups.GroupJoinRule.Request;
									break;
							}
							Groups.Groups.GetInstance().ChangeJoiningRule(player.UserID, player.GroupName, joinRule);
						}
						else {
							player.MessageHandler("What rule do you want to change to?");
						}
						break;
					case "visibility":
						Groups.GroupVisibilityRule visibilityRule = Groups.GroupVisibilityRule.Private;
						switch (commands[3]) {
							case "private":
								visibilityRule = Groups.GroupVisibilityRule.Private;
								break;
							case "public":
								visibilityRule = Groups.GroupVisibilityRule.Public;
								break;
						}
						Groups.Groups.GetInstance().ChangeVisibilityRule(player.UserID, player.GroupName, visibilityRule);
						break;
					case "say":
						Groups.Groups.GetInstance().Say(name, player.GroupName, player.UserID);
						break;
				}
			}
			else {
				player.MessageHandler("Anything in particular you want to do with a group?");
			}
		}

		//Strips away the first two words which should be  "group" followed by the action word like "create" , "disband", "approve", "deny", etc.
		//not doing a replace because the group name could contain those words or even a player name.
		private static string RemoveWords(string fullCommand) {
			string[] splitString = fullCommand.Split(' ');
			splitString = splitString.Skip(2).ToArray();

			fullCommand = "";
			foreach (string words in splitString) {
				fullCommand += words + " ";
			}
						
			return fullCommand.Trim();
		}
	}
}
