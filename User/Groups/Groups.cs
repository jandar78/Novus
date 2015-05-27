using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Groups {
	//The goal of this class is to provide an interface for players in whihc they can start a group, other players can join, leader can kick people from it,
	//set up group rules, like how the loot gets divided (we may implement different systems for this, like first-to-loot, dice-roll, Next-Player-Loots, only-leader-loots, etc.
	//Will have to implement a system so that new rules can be added easily


	public class Groups {

		private static Groups _groupInstance = null;
		private static List<Group> _groupList = new List<Group>();

		public static Groups GetInstance() {
			return _groupInstance ?? new Groups();
		}

		public void CreateGroup(string groupName) {
			_groupList.Add(new Group(groupName));
		}

		public void RemoveGroup(string groupName, string leaderID) {
			Group group = GetGroup(groupName);
			if (string.Equals(group.LeaderID, leaderID, StringComparison.InvariantCultureIgnoreCase)) {
				_groupList.Remove(group);
			}
		}

		public string GetGroupNameOnlyList() {
			StringBuilder sb = new StringBuilder();
			foreach (Group group in _groupList) {
				if (group.GroupRuleForJoining != GroupJoinRule.Private) {
					sb.AppendLine(group.GroupName);
				}
			}
			return sb.ToString();
		}

		//should look something like this:
		//The Filthy Scallywags 
		//	Jandar Onerom (Leader)
		//	Murder Bot
		//	Guy Redshirt
		public string GetAllGroupInfo(string groupName) {
			string groupInfo = null;
			foreach (Group group in _groupList) {
				if (string.Equals(group.GroupName, groupName, StringComparison.InvariantCultureIgnoreCase)) {
					groupInfo = GetPlayersInGroup(null, group);
					break;
				}
			}

			return groupInfo;
		}

		private Group GetGroup(string groupName) {
			Group groupFound = null;
			foreach (Group group in _groupList) {
				if (string.Equals(group.GroupName, groupName, StringComparison.InvariantCultureIgnoreCase)) {
					groupFound = group;
				}
			}

			return groupFound;
		}

		public string GetPlayersInGroup(string groupName, Group group = null) {
			if (group == null) {
				group = GetGroup(groupName);
			}

			StringBuilder sb = new StringBuilder();
			foreach (string playerID in group.PlayerList) {
				bool isLeader = (playerID == group.LeaderID);
				sb.AppendLine("\t" + MySockets.Server.GetAUser(playerID).Player.FirstName + (isLeader == true ? " (Leader)" : ""));
			}

			return sb.ToString();
		}

		public void ChangeLootingRule(string groupName, string leaderID, GroupLootRule newRule) {
			Group group = GetGroup(groupName);
			if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
				group.ChangeLootingRule(newRule);
			}
		}

		public void ChangeJoiningRule(string groupName, string leaderID, GroupJoinRule newRule) {
			Group group = GetGroup(groupName);
			if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
				group.ChangeJoinRule(newRule);
			}
		}

		public void RemovePlayerFromGroup(string groupName, string leaderID, string playerID) {
			Group group = GetGroup(groupName);
			if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
				group.RemovePlayerFromGroup(playerID);
			}
		}

		public void AddPlayerToGroup(string groupName, string leaderID, string playerID) {
			Group group = GetGroup(groupName);
			if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
				group.AddPlayerToGroup(playerID);
			}
		}

		public void PromoteToLeader(string groupName, string leaderID, string newLeaderID) {
			Group group = GetGroup(groupName);
			if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
				group.PromoteToLeader(newLeaderID);
			}
		}
	}



	//Leader only = only the group leader can loot from a corpse someone in the group has killed
	//First to loot = first person in the group to loot the corpse keeps it, basically free for all.
	//Next player loots = Each player in order is the only player that can loot a corpse that was killed by someone in the group.
	//Chance loot = Each player rolls a dice and whoever has the highest roll wins, ties keep rolling until only a winner remains.
	//Chance loot vote = players can choose whether to participate in the loot chance by passing or rolling. Toggle type so we don't always prompt the players.
	public enum GroupLootRule {
		Leader_only,
		First_to_loot,
		Next_player_loots,
		Chance_Loot,
		Chance_vote
	};

	//public = open to anyone, no group request is sent to the leader, players can just join.
	//private = to join leader must invite player (hidden from Group List, unless player is in group)
	//friends only = only players on the leaders friend list can join the group
	//request = Players can join if leader approves request or leader sends invite.
	public enum GroupJoinRule {
		Public,
		Private,
		Friends_only,
		Request
	};

}
