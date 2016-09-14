using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Sockets;
using MongoDB.Bson;

namespace Groups {
	//this class will hold all the pertinent information about a group, like who the leader is, what players are in it, which rules apply to it etc.
	public class Group {
		public string GroupName {
			get;
			private set;
		}

		public List<ObjectId> PlayerList {
			get;
			private set;
		}

		private Dictionary<int, ObjectId> NextLooterList {
			get;
			set;
		}

		private int CurrentLooter {
			get;
			set;
		}

		public List<ObjectId> PendingRequests {
			get;
			private set;
		}

		public List<ObjectId> PendingInvitations {
			get;
			private set;
		}

		public ObjectId LeaderId {
			get;
			private set;
		}

		private ObjectId MasterLooter {
			get;
			set;
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

		private List<ObjectId> LastLootedCorpse {
			get;
			set;
		}

		public Group(string groupName, ObjectId leaderId) {
			if (string.IsNullOrEmpty(groupName)) {
				groupName = "The filthy scallywags"; //eventually get a terrible random group name from a list in the DB
			}

			GroupName = groupName;
			LeaderId = leaderId;
			PlayerList = new List<ObjectId>();
			PendingRequests = new List<ObjectId>();
			PendingInvitations = new List<ObjectId>();
			AddPlayerToGroup(leaderId);
		}

		public bool IsLeader(ObjectId playerId) {
			return playerId.Pid == LeaderId.Pid;
		}

		public void AddPlayerToGroup(ObjectId playerId) {
			if (!PlayerList.Contains(playerId)) {
				InformPlayersInGroup(Server.GetAUser(playerId).Player.FullName + " has joined the group.");
				
				PlayerList.Add(playerId);
				Server.GetAUser(playerId).GroupName = GroupName;
				if (PendingInvitations.Contains(playerId)) {
					PendingInvitations.Remove(playerId);
				}
				if (PendingRequests.Contains(playerId)) {
					PendingRequests.Remove(playerId);
				}

				InformPlayerInGroup("You have joined '" + GroupName + "'.", playerId);
			}

			if (GroupRuleForLooting == GroupLootRule.Master_Looter && MasterLooter == ObjectId.Empty) {
				Server.GetAUser(LeaderId).MessageHandler("You have not yet assigned someone in the group as the master looter.");
			}
		}

		public void AssignMasterLooter(ObjectId leaderId, string playerName) {
			if (GroupRuleForLooting == GroupLootRule.Master_Looter) {
				if (leaderId.Pid == LeaderId.Pid) {
					var playerId = Server.GetAUserByFullName(playerName).UserID;
					if (PlayerList.Contains(playerId)) {
						MasterLooter = playerId;
						InformPlayersInGroup(string.Format("{0} has been assigned by {1} as the group Master looter.", Server.GetAUser(playerId).Player.FullName, Server.GetAUser(LeaderId).Player.FullName), new List<ObjectId>() { playerId });
						InformPlayerInGroup(string.Format("You have been designated the group Master Looter by {0}.", Server.GetAUser(LeaderId).Player.FullName), playerId);
					}
				}
				else {
					Server.GetAUser(leaderId).MessageHandler("Only the group leader can assign a master looter.");
				}
			}
			else {
				Server.GetAUser(leaderId).MessageHandler("You can only assign a master looter if the group looting rule is set to master looter.");
			}
		}

		public void RemoveMasterLooter(ObjectId leaderId) {
			if (string.Equals(leaderId, LeaderId)) {
				InformPlayersInGroup(string.Format("{0} is no longer the group Master looter.", Server.GetAUser(MasterLooter).Player.FullName), new List<ObjectId>() { MasterLooter });
				InformPlayerInGroup("You are no longer the group Master looter.", MasterLooter);
				MasterLooter = ObjectId.Empty;
				
			}
			else {
				Server.GetAUser(leaderId).MessageHandler("Only the group leader can remove the master looter.");
			}
		}



		public void RemovePlayerFromGroup(ObjectId playerId) {
			if (PlayerList.Contains(playerId)) {
				PlayerList.Remove(playerId);
				InformPlayersInGroup(Server.GetAUser(playerId).Player.FullName + " has left the group.", new List<ObjectId>() { playerId });
				InformPlayerInGroup("You have left '" + GroupName + "'.", playerId);
			}
		}

		public void RemovePendingInvitation(ObjectId playerId) {
			if (PendingInvitations.Contains(playerId)) {
				PendingInvitations.Remove(playerId);
				Server.GetAUser(playerId).MessageHandler("You are no longer invited to join the group.");
				Server.GetAUser(LeaderId).MessageHandler(Server.GetAUser(playerId).Player.FullName + " has declined the invitation to join the group.");
			}
		}

		public void InvitePlayer(string playerName) {
			IUser user = Server.GetAUserByFullName(playerName);
			user.MessageHandler(string.Format("You have been invited to join the group \"{0}\"", GroupName));
			Server.GetAUser(LeaderId).MessageHandler("An invitation to join the group has been sent to " + user.Player.FullName + ".");
		}

		public void ChangeLootingRule(GroupLootRule newRule) {
			if (GroupRuleForLooting != newRule) {
				//zero out stuff for other rules
				if (GroupRuleForLooting == GroupLootRule.Master_Looter) {
					RemoveMasterLooter(LeaderId);
				}
				else if (GroupRuleForLooting == GroupLootRule.Next_player_loots) {
					NextLooterList = null;
					CurrentLooter = 0;
				}
				else if (GroupRuleForLooting == GroupLootRule.Chance_Loot) {
					MasterLooter = ObjectId.Empty;
					LastLootedCorpse = null;
				}

				if (newRule == GroupLootRule.Chance_Loot) {
					LastLootedCorpse = new List<ObjectId>();
				}

				GroupRuleForLooting = newRule;
				InformPlayersInGroup("Group looting rule has been changed to " + newRule.ToString().Replace("_", " "));
			}
		}

		public void ChangeJoinRule(GroupJoinRule newRule) {
			GroupRuleForJoining = newRule;
			InformPlayersInGroup("Group join rule has been changed to " + newRule.ToString().Replace("_", " "));
		}

		public void ChangeVisibilityRule(GroupVisibilityRule newRule) {
			GroupRuleForVisibility = newRule;
			InformPlayersInGroup("Group visibility rule has been changed to " + newRule.ToString().Replace("_", " "));
		}

		public void PromoteToLeader(ObjectId newLeaderId) {
			if (PlayerList.Contains(newLeaderId)) {
				LeaderId = newLeaderId;
				InformPlayersInGroup(Server.GetAUser(LeaderId).Player.FullName + " has been promoted to group leader.", new List<ObjectId>(){ LeaderId });
				InformGroupLeader("You have been promoted to group leader of " + GroupName);
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(GroupName);
			foreach (var playerId in PlayerList) {
				sb.AppendLine("\t" + Server.GetAUser(playerId).Player.FullName + (playerId.Pid == LeaderId.Pid ? " (Leader)" : ""));
			}

			return sb.ToString();
		}

		public void Disband(string message){
			foreach (var playerId in PlayerList) {
				Server.GetAUser(playerId).MessageHandler(message);
			}

			PendingInvitations.Clear();
			PendingRequests.Clear();
			PlayerList.Clear();
			LeaderId = ObjectId.Empty;
			GroupName = null;
		}

		public void RequestJoin(ObjectId playerId) {
			PendingRequests.Add(playerId);
			string message = string.Format("{0} requests permission to join the group.", Server.GetAUser(playerId).Player.FullName);
			Server.GetAUser(playerId).MessageHandler("Your request to join the group has been sent to the group leader.");
			InformGroupLeader(message);
		}

		public void ApproveDenyRequest(ObjectId playerId, bool accepted) {
			if (PendingRequests.Contains(playerId)) {
				PendingRequests.Remove(playerId);
				string msg = "Your join request has been accepted by " + Server.GetAUser(LeaderId).Player.FullName;
				if (accepted) {
					AddPlayerToGroup(playerId);
				}
				else {
					msg = "Your join request has been denied by " + Server.GetAUser(LeaderId).Player.FullName;
				}

				Server.GetAUser(playerId).MessageHandler("Your request to join the group has been " + (accepted ? "approved" : "denied") + ".");
			}
			else {
				InformGroupLeader(Server.GetAUser(LeaderId).Player.FullName + " was not found in the pending request list.");
			}
		}

		public void GetPendingRequests() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Pending Group Requests");
			foreach (var playerId in PendingRequests) {
				sb.AppendLine("\t" + Server.GetAUser(playerId).Player.FullName);
			}

			Server.GetAUser(LeaderId).MessageHandler(sb.ToString());
		}

		public void GetPendingInvitationRequests() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Pending Group Invitation Requests");
			foreach (var playerId in PendingRequests) {
				sb.AppendLine("\t" + Server.GetAUser(playerId).Player.FullName);
			}

			Server.GetAUser(LeaderId).MessageHandler(sb.ToString());
		}

		public void CancelJoinRequest(ObjectId playerId) {
			if (PendingRequests.Contains(playerId)) {
				PendingRequests.Remove(playerId);
			}
			else {
				Server.GetAUser(playerId).MessageHandler("You never submitted a request to join this group.");
			}
		}

		public void RewardXP(long xpAmount) {
			foreach (var playerId in PlayerList) {
				IUser user = Server.GetAUser(playerId);
				user.MessageHandler(string.Format("You gain {0:0.##} XP", xpAmount));
				user.Player.Experience += xpAmount;
			}
		}

		public void SayToGroup(string message, ObjectId playerToIgnore) {
			InformPlayersInGroup(Server.GetAUser(playerToIgnore).Player.FullName + " says to the group \"" + message + "\"", new List<ObjectId>(){ playerToIgnore });
			InformPlayerInGroup("You say to the group \"" + message + "\"", playerToIgnore);
		}

		public bool HasPlayer(ObjectId playerId) {
			return PlayerList.Contains(playerId);
		}

		private void InformGroupLeader(string message) {
			Server.GetAUser(LeaderId).MessageHandler(message);
		}

		private void InformPlayerInGroup(string message, ObjectId playerId) {
			Server.GetAUser(playerId).MessageHandler(message);
		}

		private void InformPlayersInGroup(string message, List<ObjectId> IdToIgnore = null) {
			if (IdToIgnore == null) {
				IdToIgnore = new List<ObjectId>();
			}
			foreach (var playerId in PlayerList) {
				if (!IdToIgnore.Contains(playerId)) {
					Server.GetAUser(playerId).MessageHandler(message);
				}
			}
		}

		public void Loot(IUser looter, List<string> commands, IActor npc) {
			//okay the group looting rule is not free for all thats why we arrived here.  We now need to abIde by the looting rule that governs the
			//group.

			//need to create the loot methods for each rule and also add master loot rule
			switch (GroupRuleForLooting) {
				case GroupLootRule.Leader_only:
					OnlyLeaderLoots(looter, commands, npc);
					break;
				case GroupLootRule.Next_player_loots:
					NextPlayerLoots(looter, commands, npc);
					break;
				case GroupLootRule.Chance_Loot:
					ChanceLoot(looter, commands, npc);
					break;
				case GroupLootRule.Chance_vote:
					break;
				case GroupLootRule.Master_Looter:
					MasterLooterLoots(looter, commands, npc);
					break;
			}
		}


		//one player will randomly be chosen as the loot winner and can loot something. Once he loots something looting should be opne for all for
		//the corpse that was looted, otherwise another winner is randomly chosen again.
		//this has the drawback that only one corpse is remembered and not all the corpses that got looted not cool.
		private void ChanceLoot(IUser looter, List<string> commands, IActor npc) {
			//we don't have a loot winner yet so let's roll the dice
			if (MasterLooter == null) {
				int highestRoll = 0;
                ObjectId winner = ObjectId.Empty;
				foreach (var player in PlayerList) {
					int currentRoll = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 20);
					if (currentRoll > highestRoll) {
						winner = player;
					}
				}
			}

			//so the looter was the winner or another player in the group is looting a corpse that was previously looted by a previous winner
			if (string.Equals(looter.UserID, MasterLooter) || LastLootedCorpse.Contains(npc.Id)) {
				if (npc.Loot(looter, commands, true)) {
					//if player actually looted something
					MasterLooter = ObjectId.Empty;
					if (!LastLootedCorpse.Contains(npc.Id)) {
						LastLootedCorpse.Add(npc.Id);
					}
				}
				//we don't need to keep any old Id's since they probably rotted away anyways. Seriously doubt the group looted 100 bodies and then wanted to go
				//back and re-loot one of the 50 first corpses.  This number is subject to change once testing starts happening.
				if (LastLootedCorpse.Count > 100) {
					LastLootedCorpse.RemoveRange(0, 49);
				}
			}
			else {
				Server.GetAUser(looter.UserID).MessageHandler("You dId not win the loot draw and can not loot this corpse.");
			}

		}

		//the way this works is the current looter can either loot all or loot a specific item and then his turn is over and the next player gets a chance to loot.
		//the next player can choose to loot something from the current corpse
		private void NextPlayerLoots(IUser looter, List<string> commands, IActor npc) {
			if (NextLooterList == null) {
				//create th elist of looters in no particular order
				NextLooterList = new Dictionary<int, ObjectId>();
				int index = 0;
				foreach (var player in PlayerList) {
					NextLooterList.Add(index, player);
					index++;
				}
			}

			if (looter.UserID == NextLooterList[CurrentLooter]) {
				if (npc.Loot(looter, commands, true)) {
					//if the player actually loots something then we'll increment the counter
					CurrentLooter++;
					if (CurrentLooter > NextLooterList.Count) {
						CurrentLooter = 0;
					}
				}
			}
			else {
				int playerPosition = 0;
				foreach (var keyValue in NextLooterList) {
					if (keyValue.Value == looter.UserID) {
						break;
					}
					playerPosition++;
				}

				playerPosition -= CurrentLooter;

				if (playerPosition < 0) {
					playerPosition = playerPosition + NextLooterList.Count;
				}

				Server.GetAUser(looter.UserID).MessageHandler(string.Format("You are not eligible to loot at this time, it will be your turn in {0} more lootings.", playerPosition));
			}
		}

		private void OnlyLeaderLoots(IUser looter, List<string> commands, IActor npc) {
			if (string.Equals(looter.UserID, LeaderId)) {
				npc.Loot(looter, commands, true);
			}
			else {
				looter.MessageHandler("Only the group leader can loot corpses killed by the group.");
			}
		}

		private void MasterLooterLoots(IUser looter, List<string> commands, IActor npc) {
			if (string.Equals(looter.UserID, MasterLooter)) {
				npc.Loot(looter, commands, true);
			}
			else {
				looter.MessageHandler("Only the master looter can loot corpses killed by the group.");
			}
		}

	}
}
