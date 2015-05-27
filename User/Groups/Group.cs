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

		public Group(string groupName) {
			if (string.IsNullOrEmpty(groupName)) {
				groupName = "The filthy scallywags"; //eventually get a random group name from a list in the DB
			}

			GroupName = groupName;
			PlayerList = new List<string>();
		}

		public void AddPlayerToGroup(string playerID) {
			if (!PlayerList.Contains(playerID)) {
				PlayerList.Add(playerID);
			}
		}

		public void RemovePlayerFromGroup(string playerID) {
			if (PlayerList.Contains(playerID)) {
				PlayerList.Remove(playerID);
			}
		}

		//leader commands
		public void ChangeLootingRule(GroupLootRule newRule) {
			GroupRuleForLooting = newRule;
		}

		public void ChangeJoinRule(GroupJoinRule newRule) {
			GroupRuleForJoining = newRule;
		}


		public void PromoteToLeader(string newLeaderID) {
			if (PlayerList.Contains(newLeaderID)) {
				LeaderID = newLeaderID;
			}
		}


		//helper method
		private void InformPlayersInGroup(string message) {
			string leaderName = MySockets.Server.GetAUser(LeaderID).Player.FirstName;
			foreach (string playerID in PlayerList) {
				MySockets.Server.GetAUser(LeaderID).MessageHandler(string.Format(message, (playerID != LeaderID ? leaderName : "You")));
			}
		}
	}
}
