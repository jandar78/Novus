using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Groups {
	//this class will hold all the pertinent information about a group, like who the leader is, what players are in it, which rules apply to it etc.
	public class Group {
		public string GroupName {
			get;
			private set;
		}

		public List<string> PlayerList {
			get;
			private set;
		}

		public List<string> PendingRequests {
			get;
			private set;
		}

		public string LeaderID {
			get;
			private set;
		}

		public GroupLootRule GroupRuleForLooting {
			get;
			private set;
		}

		public GroupJoinRule GroupRuleForJoining {
			get;
			private set;
		}

		public GroupVisibilityRule GroupRuleForVisibility {
			get;
			private set;
		}

		public Group(string groupName) {
			if (string.IsNullOrEmpty(groupName)) {
				groupName = "The filthy scallywags"; //eventually get a terrible random group name from a list in the DB
			}

			GroupName = groupName;
			PlayerList = new List<string>();
			PendingRequests = new List<string>();
		}

		public void AddPlayerToGroup(string playerID) {
			if (!PlayerList.Contains(playerID)) {
				InformPlayersInGroup(MySockets.Server.GetAUser(playerID).Player.FullName + " has joined the group.");
				PlayerList.Add(playerID);
				InformPlayerInGroup("You have joined '" + GroupName + "'.", playerID);
			}
		}

		public void RemovePlayerFromGroup(string playerID) {
			if (PlayerList.Contains(playerID)) {
				PlayerList.Remove(playerID);
				InformPlayersInGroup(MySockets.Server.GetAUser(playerID).Player.FullName + " has left the group.");
				InformPlayerInGroup("You have left '" + GroupName + "'.", playerID);
			}
		}

		public void ChangeLootingRule(GroupLootRule newRule) {
			GroupRuleForLooting = newRule;
			InformPlayersInGroup("Group looting rule has been changed to " + newRule.ToString().Replace("_", " "));
		}

		public void ChangeJoinRule(GroupJoinRule newRule) {
			GroupRuleForJoining = newRule;
			InformPlayersInGroup("Group join rule has been changed to " + newRule.ToString().Replace("_", " "));
		}

		public void ChangeVisibilityRule(GroupVisibilityRule newRule) {
			GroupRuleForVisibility = newRule;
			InformPlayersInGroup("Group visibility rule has been changed to " + newRule.ToString().Replace("_", " "));
		}

		public void PromoteToLeader(string newLeaderID) {
			if (PlayerList.Contains(newLeaderID)) {
				LeaderID = newLeaderID;
				InformPlayersInGroup(MySockets.Server.GetAUser(LeaderID).Player.FullName + " has been promoted to group leader.");
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(GroupName);
			foreach (string playerID in PlayerList) {
				sb.AppendLine("\t" + MySockets.Server.GetAUser(playerID).Player.FullName + (string.Equals(playerID, LeaderID, StringComparison.InvariantCultureIgnoreCase) ? " (Leader)" : ""));
			}

			return sb.ToString();
		}

		public void Disband(string message){
			foreach (string playerID in PlayerList) {
				MySockets.Server.GetAUser(playerID).MessageHandler(message);
			}

			PlayerList.Clear();
			LeaderID = null;
			GroupName = null;
		}

		public void RequestJoin(string playerID) {
			PendingRequests.Add(playerID);
			string message = string.Format("{0} requests permission to join the group.", MySockets.Server.GetAUser(playerID).Player.FullName);
			InformGroupLeader(message);
		}

		public void ApproveDenyRequest(string playerID, bool accepted) {
			if (PendingRequests.Contains(playerID)) {
				PendingRequests.Remove(playerID);
				string msg = "Your join request has been accepted by " + MySockets.Server.GetAUser(LeaderID).Player.FullName;
				if (accepted) {
					AddPlayerToGroup(playerID);
				}
				else {
					msg = "Your join request has been denied by " + MySockets.Server.GetAUser(LeaderID).Player.FullName;
				}

				InformPlayerInGroup(msg, playerID);
			}
			else {
				InformGroupLeader(MySockets.Server.GetAUser(LeaderID).Player.FullName + " was not found in the pending request list.");
			}
		}

		public void GetPendingRequests() {
		}

		public void CancelJoinRequest(string playerID) {
			if (PendingRequests.Contains(playerID)) {
				PendingRequests.Remove(playerID);
			}
			else {
				MySockets.Server.GetAUser(playerID).MessageHandler("You never submitted a request to join this group.");
			}
		}

		public bool HasPlayer(string playerID) {
			return PlayerList.Contains(playerID);
		}

		private void InformGroupLeader(string message) {
			MySockets.Server.GetAUser(LeaderID).MessageHandler(message);
		}

		private void InformPlayerInGroup(string message, string playerID) {
			MySockets.Server.GetAUser(playerID).MessageHandler(message);
		}

		private void InformPlayersInGroup(string message) {
			foreach (string playerID in PlayerList) {
				MySockets.Server.GetAUser(playerID).MessageHandler(message);
			}
		}
	}
}
