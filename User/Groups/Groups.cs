using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Groups {
	//The goal of this class is to provide an interface for players in whihc they can start a group, other players can join, leader can kick people from it,
	//set up group rules, like how the loot gets divided (we may implement different systems for this, like first-to-loot, dice-roll, Next-Player-Loots, only-leader-loots, etc.
	//Will have to implement a system so that new rules can be added easily

	//For XP I think what we will do is everyone gets the same XP amount in the group but at or below 50% of what they could get individually.  Basically as a group they
	//should be killing things much quicker and easier than soloing therefore the reduced XP amount.


	public class Groups {

		private static Groups _groupInstance = null;
		private static List<Group> _groupList = new List<Group>();

		public static Groups GetInstance() {
			return _groupInstance ?? new Groups();
		}

		private string GetMessageFromDB(string messageID) {
			MongoCollection collection = MongoUtils.MongoData.GetCollection("Messages", "Groups");
			return collection.FindOneAs<BsonDocument>(Query.EQ("_id", messageID))["Message"].AsString;
		}

		public void CreateGroup(string leaderID, string groupName) {
			string message = null;
			string msgID = "GroupCreated";

			if (!IsPlayerInGroup(leaderID)) {
				if (!GroupAlreadyExists(groupName)) {
					try{
						if (string.IsNullOrEmpty(groupName)) {
							MySockets.Server.GetAUser(leaderID).MessageHandler("You must provide a name for the group.");
						}
						else {
							_groupList.Add(new Group(groupName));
						}
					}
					catch (Exception) { //you never know
						msgID = "GroupCreateFailed";
					}
				}
				else {
					msgID = "GroupExists";
				}
			}
			else {
				msgID = "LeaderInOtherGroup";
			}

			MySockets.Server.GetAUser(leaderID).MessageHandler(string.Format(GetMessageFromDB(msgID), message));
		}

		private bool GroupAlreadyExists(string groupName) {
			bool exists = false;
			foreach (Group group in _groupList) {
					if (string.Equals(group.GroupName, groupName, StringComparison.InvariantCultureIgnoreCase)) {
						exists = true;
						break;
					}
				}

			return exists;
		}

		public void DisbandGroup(string leaderID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(group.LeaderID, leaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.Disband(GetMessageFromDB("DisbandGroup"));
					_groupList.Remove(group);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("You can not disband the group. Only the leader can disband the group.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group exist with that name to disband.");
			}
		}

		public string GetGroupNameOnlyList() {
			StringBuilder sb = new StringBuilder();
			foreach (Group group in _groupList) {
				if (group.GroupRuleForVisibility != GroupVisibilityRule.Private) {
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

		public Group GetGroup(string groupName) {
			Group groupFound = null;
			foreach (Group group in _groupList) {
				if (string.Equals(group.GroupName, groupName, StringComparison.InvariantCultureIgnoreCase)) {
					groupFound = group;
					break;
				}
			}

			return groupFound;
		}

		public Group GetGroupByLeaderID(string leaderID) {
			Group groupFound = null;
			foreach (Group group in _groupList) {
				if (string.Equals(group.LeaderID, leaderID, StringComparison.InvariantCultureIgnoreCase)) {
					groupFound = group;
					break;
				}
			}

			return groupFound;
		}

		public string GetPlayersInGroup(string groupName, Group group = null) {
			if (group == null) {
				group = GetGroup(groupName);
			}

			return group.ToString() ?? "";
		}

		public void ChangeLootingRule(string leaderID, string groupName, GroupLootRule newRule) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.ChangeLootingRule(newRule);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can chnage group looting rules.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void ChangeJoiningRule(string leaderID, string groupName, GroupJoinRule newRule) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.ChangeJoinRule(newRule);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can change group joining rules.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void RemovePlayerFromGroup(string leaderID, string groupName, string playerID) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.RemovePlayerFromGroup(playerID);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can remove players from the group.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void AddPlayerToGroup(string leaderID, string groupName, string playerID) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.AddPlayerToGroup(playerID);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can add players to the group.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void PromoteToLeader(string leaderID, string groupName, string newLeaderID) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID, StringComparison.InvariantCultureIgnoreCase)) {
					group.PromoteToLeader(newLeaderID);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can promote another player to group leader.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		private bool IsPlayerInGroup(string playerID, string groupName = null) {
			bool playerIsInGroup = false;

			if (string.IsNullOrEmpty(groupName)) {
				foreach (Group group in _groupList) {
					playerIsInGroup = group.HasPlayer(playerID);
					if (playerIsInGroup) {
						break;
					}
				}
			}
			else {
				playerIsInGroup = GetGroup(groupName).HasPlayer(playerID);
			}

			return playerIsInGroup;
		}

		public void RequestGroupJoin(string playerID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (group.GroupRuleForVisibility == GroupVisibilityRule.Public) {
					if (group.GroupRuleForJoining == GroupJoinRule.Request) {
						group.RequestJoin(playerID);
					}
					else if (group.GroupRuleForJoining == GroupJoinRule.Friends_only) {
						if (!MySockets.Server.GetAUser(group.LeaderID).FriendsList.Contains(playerID)) {
							MySockets.Server.GetAUser(playerID).MessageHandler("You are not a friend of the group leader. You can not join the group");
						}
						else {
							group.RequestJoin(playerID);
						}
					}
					else {
						MySockets.Server.GetAUser(playerID).MessageHandler("The group is public, a request is not neccessary to join it.");
					}
				}
				else {
					MySockets.Server.GetAUser(playerID).MessageHandler("You can not submit a request to join the group.");
				}
			}
			else {
				MySockets.Server.GetAUser(playerID).MessageHandler("No group with that name exists.");
			}
		}

		public void GetPendingRequests(string leaderID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID)) {
					group.GetPendingRequests();
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("You are not the group leader and can not view pending requests.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void GetPendingInvitations(string leaderID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID)) {
					group.GetPendingInvitationRequests();
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("You are not the group leader and can not view group invitations.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void DeclineInvite(string playerID, string groupName) {
			string msg = null;
			Group group = GetGroup(groupName);
			if (group != null) {
				if (group.PendingInvitations.Contains(playerID)) {
					group.RemovePendingInvitation(playerID);
				}
				else {
					msg = "You did not receive an invitation to join this group.";					
				}
			}
			else {
				msg = "No group with that name exists.";
			}

			MySockets.Server.GetAUser(playerID).MessageHandler(msg);
		}

		public void InviteToGroup(string leaderID, string playerName, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (string.Equals(leaderID, group.LeaderID)) {
					group.InvitePlayer(playerName);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("You are not the group leader and can not invite players to join the group.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void AcceptDenyJoinRequest(string leaderID, string playerName, bool accepted) {
			Group group = GetGroupByLeaderID(leaderID);
			if (group != null) {
				User.User player = MySockets.Server.GetAUserByFullName(playerName);
				if (player != null) {
					group.ApproveDenyRequest(player.UserID, accepted);
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("No player with that name exists.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("No group with that name exists.");
			}
		}

		public void RewardXP(long xpAmount, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				group.RewardXP(xpAmount);
			}
		}

		public void Say(string message, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null && !string.IsNullOrEmpty(message)) {
				group.SayToGroup(message);
			}
		}

		public void Join(string playerID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				//player was sent an invitation by group leader at some point
				if (group.PendingInvitations.Contains(playerID)) {
					group.AddPlayerToGroup(playerID);
				}
				else {
					if (group.GroupRuleForJoining == GroupJoinRule.Open) {
						group.AddPlayerToGroup(playerID);
					}
					else if (group.GroupRuleForJoining == GroupJoinRule.Friends_only) {
						if (!MySockets.Server.GetAUser(group.LeaderID).FriendsList.Contains(playerID)) {
							MySockets.Server.GetAUser(playerID).MessageHandler("You are not a friend of the leader, you can not join the group.");
						}
						else {
							group.AddPlayerToGroup(playerID);
						}
					}
					else if (group.GroupRuleForJoining == GroupJoinRule.Request) {
						MySockets.Server.GetAUser(playerID).MessageHandler("You can not freely join the group. Submit a request to join.");
					}
				}
			}
			else {
				MySockets.Server.GetAUser(playerID).MessageHandler("No group with that name exists.");
			}
		}

		public void Uninvite(string leaderID, string playerName, string groupName) {
			string msg = null;
			Group group = GetGroup(groupName);
			if (group != null) {
				if (group.IsLeader(leaderID)) {
					group.RemovePendingInvitation(MySockets.Server.GetAUserByFullName(playerName).UserID);
				}
				else {
					msg = "You are not the group leader.  You can not perform this action.";
				}
			}
			else {
				msg = "No group by that name exists.";
			}

			MySockets.Server.GetAUserByFullName(leaderID).MessageHandler(msg);
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
		Friends_only,
		Request,
		Open
	};

	public enum GroupVisibilityRule {
		Public,
		Private
	}

}
