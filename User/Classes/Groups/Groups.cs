using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Interfaces;
using Sockets;

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
			var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("Messages", "Groups");
            return MongoUtils.MongoData.RetrieveObject<BsonDocument>(collection, m => m["_id"] == messageID)["Message"].AsString;
		}

		public void CreateGroup(ObjectId LeaderId, string groupName) {
			string msgID = "GroupCreated";

			if (!IsPlayerInGroup(LeaderId)) {
				if (!GroupAlreadyExists(groupName)) {
					try{
						if (string.IsNullOrEmpty(groupName)) {
							Server.GetAUser(LeaderId).MessageHandler("You must provide a name for the group.");
						}
						else {
							_groupList.Add(new Group(groupName, LeaderId));
							Server.GetAUser(LeaderId).GroupName = groupName;
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

			Server.GetAUser(LeaderId).MessageHandler(string.Format(GetMessageFromDB(msgID), groupName));
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

		public void DisbandGroup(ObjectId LeaderId, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
                    group.Disband(GetMessageFromDB("DisbandGroup"));
					_groupList.Remove(group);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("You can not disband the group. Only the leader can disband the group.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group exist with that name to disband.");
			}
		}

		public string GetGroupNameOnlyList() {
			StringBuilder sb = new StringBuilder();
			if (_groupList.Count > 0) {
				sb.AppendLine("Available Groups:");
				foreach (Group group in _groupList) {
					if (group.GroupRuleForVisibility != GroupVisibilityRule.Private) {
						sb.AppendLine(group.GroupName);
					}
				}
			}
			else {
				sb.AppendLine("There are no groups available.");
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

		public Group GetGroupByLeaderId(ObjectId LeaderId) {
			Group groupFound = null;
			foreach (Group group in _groupList) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
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

		public void ChangeLootingRule(ObjectId LeaderId, string groupName, GroupLootRule newRule) {
			Group group = GetGroup(groupName);
			if (group != null) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
                    group.ChangeLootingRule(newRule);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can chnage group looting rules.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void ChangeJoiningRule(ObjectId LeaderId, string groupName, GroupJoinRule newRule) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (LeaderId.Pid == group.LeaderId.Pid) {
					group.ChangeJoinRule(newRule);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can change group joining rules.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void RemovePlayerFromGroup(ObjectId LeaderId, string playerName, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (LeaderId.Pid == group.LeaderId.Pid) {
					group.RemovePlayerFromGroup(Server.GetAUserByFullName(playerName).UserID);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can remove players from the group.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void AddPlayerToGroup(ObjectId LeaderId, string groupName, ObjectId playerID) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (LeaderId.Pid == group.LeaderId.Pid) {
					group.AddPlayerToGroup(playerID);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can add players to the group.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void PromoteToLeader(ObjectId LeaderId, string groupName, ObjectId newLeaderId) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (LeaderId.Pid == group.LeaderId.Pid) {
					group.PromoteToLeader(newLeaderId);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can promote another player to group leader.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		private bool IsPlayerInGroup(ObjectId playerID, string groupName = null) {
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

		public void RequestGroupJoin(ObjectId playerID, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (group.GroupRuleForVisibility == GroupVisibilityRule.Public) {
					if (group.GroupRuleForJoining == GroupJoinRule.Request) {
						group.RequestJoin(playerID);

					}
					else if (group.GroupRuleForJoining == GroupJoinRule.Friends_only) {
						if (!Server.GetAUser(group.LeaderId).FriendsList.Contains(playerID)) {
							Server.GetAUser(playerID).MessageHandler("You are not a friend of the group leader. You can not join the group");
						}
						else {
							group.RequestJoin(playerID);
						}
					}
					else {
						Server.GetAUser(playerID).MessageHandler("The group is public, a request is not neccessary to join it.");
					}
				}
				else {
					Server.GetAUser(playerID).MessageHandler("You can not submit a request to join the group.");
				}
			}
			else {
				Server.GetAUser(playerID).MessageHandler("No group with that name exists.");
			}
		}

		public void GetPendingRequests(ObjectId LeaderId, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
                    group.GetPendingRequests();
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("You are not the group leader and can not view pending requests.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void GetPendingInvitations(ObjectId LeaderId, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
                    group.GetPendingInvitationRequests();
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("You are not the group leader and can not view group invitations.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void DeclineInvite(ObjectId playerID, string groupName) {
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

			Server.GetAUser(playerID).MessageHandler(msg);
		}

		public void InviteToGroup(ObjectId LeaderId, string playerName, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
                if (LeaderId.Pid == group.LeaderId.Pid) {
                    group.InvitePlayer(playerName);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("You are not the group leader and can not invite players to join the group.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void AcceptDenyJoinRequest(ObjectId LeaderId, string playerName, bool accepted) {
			Group group = GetGroupByLeaderId(LeaderId);
			if (group != null) {
				IUser player = Server.GetAUserByFullName(playerName);
				if (player != null) {
					group.ApproveDenyRequest(player.UserID, accepted);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("No player with that name exists.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
			}
		}

		public void RewardXP(long xpAmount, string groupName) {
			Group group = GetGroup(groupName);
			if (group != null) {
				group.RewardXP(xpAmount);
			}
		}

		public void Say(string message, string groupName, ObjectId playerID) {
			Group group = GetGroup(groupName);
			if (group != null && !string.IsNullOrEmpty(message)) {
				group.SayToGroup(message, playerID);
			}
		}

		public void Join(ObjectId playerID, string groupName) {
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
						if (!Server.GetAUser(group.LeaderId).FriendsList.Contains(playerID)) {
							Server.GetAUser(playerID).MessageHandler("You are not a friend of the leader, you can not join the group.");
						}
						else {
							group.AddPlayerToGroup(playerID);
						}
					}
					else if (group.GroupRuleForJoining == GroupJoinRule.Request) {
						Server.GetAUser(playerID).MessageHandler("You can not freely join the group. Submit a request to join.");
					}
				}
			}
			else {
				Server.GetAUser(playerID).MessageHandler("No group with that name exists.");
			}
		}

		public void Uninvite(ObjectId LeaderId, string playerName, string groupName) {
			string msg = null;
			Group group = GetGroup(groupName);
			if (group != null) {
				if (group.IsLeader(LeaderId)) {
					group.RemovePendingInvitation(Server.GetAUserByFullName(playerName).UserID);
					msg = "The group invitation for " + playerName + " has been removed.";
				}
				else {
					msg = "You are not the group leader.  You can not perform this action.";
				}
			}
			else {
				msg = "No group by that name exists.";
			}

			Server.GetAUser(LeaderId).MessageHandler(msg);
		}

		public void AssignMasterLooter(ObjectId LeaderId, string masterID, string groupName) {
			Group group = GetGroup(groupName);
			group.AssignMasterLooter(LeaderId, masterID);
		}

		public void ChangeVisibilityRule(ObjectId LeaderId, string groupName, GroupVisibilityRule newRule) {
			Group group = GetGroup(groupName);
			if (group != null) {
				if (LeaderId.Pid == group.LeaderId.Pid) {
					group.ChangeVisibilityRule(newRule);
				}
				else {
					Server.GetAUser(LeaderId).MessageHandler("Only the group leader can change group visibility rules.");
				}
			}
			else {
				Server.GetAUser(LeaderId).MessageHandler("No group with that name exists.");
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
		Chance_vote,
		Master_Looter
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
