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

		private Dictionary<int, string> NextLooterList {
			get;
			set;
		}

		private int CurrentLooter {
			get;
			set;
		}

		public List<string> PendingRequests {
			get;
			private set;
		}

		public List<string> PendingInvitations {
			get;
			private set;
		}

		public string LeaderID {
			get;
			private set;
		}

		private string MasterLooter {
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

		private List<string> LastLootedCorpse {
			get;
			set;
		}

		public Group(string groupName, string leaderID) {
			if (string.IsNullOrEmpty(groupName)) {
				groupName = "The filthy scallywags"; //eventually get a terrible random group name from a list in the DB
			}

			GroupName = groupName;
			LeaderID = leaderID;
			PlayerList = new List<string>();
			PendingRequests = new List<string>();
			PendingInvitations = new List<string>();
			AddPlayerToGroup(leaderID);
		}

		public bool IsLeader(string playerID) {
			return string.Equals(playerID, LeaderID, StringComparison.InvariantCultureIgnoreCase);
		}

		public void AddPlayerToGroup(string playerID) {
			if (!PlayerList.Contains(playerID)) {
				InformPlayersInGroup(MySockets.Server.GetAUser(playerID).Player.FullName + " has joined the group.");
				
				PlayerList.Add(playerID);
				MySockets.Server.GetAUser(playerID).GroupName = GroupName;
				if (PendingInvitations.Contains(playerID)) {
					PendingInvitations.Remove(playerID);
				}
				if (PendingRequests.Contains(playerID)) {
					PendingRequests.Remove(playerID);
				}

				InformPlayerInGroup("You have joined '" + GroupName + "'.", playerID);
			}

			if (GroupRuleForLooting == GroupLootRule.Master_Looter && string.IsNullOrEmpty(MasterLooter)) {
				MySockets.Server.GetAUser(LeaderID).MessageHandler("You have not yet assigned someone in the group as the master looter.");
			}
		}

		public void AssignMasterLooter(string leaderID, string playerID) {
			if (GroupRuleForLooting == GroupLootRule.Master_Looter) {
				if (string.Equals(leaderID, LeaderID)) {
					playerID = MySockets.Server.GetAUserByFullName(playerID).UserID;
					if (PlayerList.Contains(playerID)) {
						MasterLooter = playerID;
						InformPlayersInGroup(string.Format("{0} has been assigned by {1} as the group Master looter.", MySockets.Server.GetAUser(playerID).Player.FullName, MySockets.Server.GetAUser(LeaderID).Player.FullName), new List<string>() { playerID });
						InformPlayerInGroup(string.Format("You have been designated the group Master Looter by {0}.", MySockets.Server.GetAUser(LeaderID).Player.FullName), playerID);
					}
				}
				else {
					MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can assign a master looter.");
				}
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("You can only assign a master looter if the group looting rule is set to master looter.");
			}
		}

		public void RemoveMasterLooter(string leaderID) {
			if (string.Equals(leaderID, LeaderID)) {
				InformPlayersInGroup(string.Format("{0} is no longer the group Master looter.", MySockets.Server.GetAUser(MasterLooter).Player.FullName), new List<string>() { MasterLooter });
				InformPlayerInGroup("You are no longer the group Master looter.", MasterLooter);
				MasterLooter = null;
				
			}
			else {
				MySockets.Server.GetAUser(leaderID).MessageHandler("Only the group leader can remove the master looter.");
			}
		}



		public void RemovePlayerFromGroup(string playerID) {
			if (PlayerList.Contains(playerID)) {
				PlayerList.Remove(playerID);
				InformPlayersInGroup(MySockets.Server.GetAUser(playerID).Player.FullName + " has left the group.", new List<string>() { playerID });
				InformPlayerInGroup("You have left '" + GroupName + "'.", playerID);
			}
		}

		public void RemovePendingInvitation(string playerID) {
			if (PendingInvitations.Contains(playerID)) {
				PendingInvitations.Remove(playerID);
				MySockets.Server.GetAUser(playerID).MessageHandler("You are no longer invited to join the group.");
				MySockets.Server.GetAUser(LeaderID).MessageHandler(MySockets.Server.GetAUser(playerID).Player.FullName + " has declined the invitation to join the group.");
			}
		}

		public void InvitePlayer(string playerName) {
			User.User user = MySockets.Server.GetAUserByFullName(playerName);
			user.MessageHandler(string.Format("You have been invited to join the group \"{0}\"", GroupName));
			MySockets.Server.GetAUser(LeaderID).MessageHandler("An invitation to join the group has been sent to " + user.Player.FullName + ".");
		}

		public void ChangeLootingRule(GroupLootRule newRule) {
			if (GroupRuleForLooting != newRule) {
				//zero out stuff for other rules
				if (GroupRuleForLooting == GroupLootRule.Master_Looter) {
					RemoveMasterLooter(LeaderID);
				}
				else if (GroupRuleForLooting == GroupLootRule.Next_player_loots) {
					NextLooterList = null;
					CurrentLooter = 0;
				}
				else if (GroupRuleForLooting == GroupLootRule.Chance_Loot) {
					MasterLooter = null;
					LastLootedCorpse = null;
				}

				if (newRule == GroupLootRule.Chance_Loot) {
					LastLootedCorpse = new List<string>();
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

		public void PromoteToLeader(string newLeaderID) {
			if (PlayerList.Contains(newLeaderID)) {
				LeaderID = newLeaderID;
				InformPlayersInGroup(MySockets.Server.GetAUser(LeaderID).Player.FullName + " has been promoted to group leader.", new List<string>(){ LeaderID });
				InformGroupLeader("You have been promoted to group leader of " + GroupName);
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

			PendingInvitations.Clear();
			PendingRequests.Clear();
			PlayerList.Clear();
			LeaderID = null;
			GroupName = null;
		}

		public void RequestJoin(string playerID) {
			PendingRequests.Add(playerID);
			string message = string.Format("{0} requests permission to join the group.", MySockets.Server.GetAUser(playerID).Player.FullName);
			MySockets.Server.GetAUser(playerID).MessageHandler("Your request to join the group has been sent to the group leader.");
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

				MySockets.Server.GetAUser(playerID).MessageHandler("Your request to join the group has been " + (accepted ? "approved" : "denied") + ".");
			}
			else {
				InformGroupLeader(MySockets.Server.GetAUser(LeaderID).Player.FullName + " was not found in the pending request list.");
			}
		}

		public void GetPendingRequests() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Pending Group Requests");
			foreach (string playerID in PendingRequests) {
				sb.AppendLine("\t" + MySockets.Server.GetAUser(playerID).Player.FullName);
			}

			MySockets.Server.GetAUser(LeaderID).MessageHandler(sb.ToString());
		}

		public void GetPendingInvitationRequests() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Pending Group Invitation Requests");
			foreach (string playerID in PendingRequests) {
				sb.AppendLine("\t" + MySockets.Server.GetAUser(playerID).Player.FullName);
			}

			MySockets.Server.GetAUser(LeaderID).MessageHandler(sb.ToString());
		}

		public void CancelJoinRequest(string playerID) {
			if (PendingRequests.Contains(playerID)) {
				PendingRequests.Remove(playerID);
			}
			else {
				MySockets.Server.GetAUser(playerID).MessageHandler("You never submitted a request to join this group.");
			}
		}

		public void RewardXP(long xpAmount) {
			foreach (string playerID in PlayerList) {
				User.User user = MySockets.Server.GetAUser(playerID);
				user.MessageHandler(string.Format("You gain {0:0.##} XP", xpAmount));
				user.Player.Experience += xpAmount;
			}
		}

		public void SayToGroup(string message, string playerToIgnore) {
			InformPlayersInGroup(MySockets.Server.GetAUser(playerToIgnore).Player.FullName + " says to the group \"" + message + "\"", new List<string>(){ playerToIgnore });
			InformPlayerInGroup("You say to the group \"" + message + "\"", playerToIgnore);
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

		private void InformPlayersInGroup(string message, List<string> idToIgnore = null) {
			if (idToIgnore == null) {
				idToIgnore = new List<string>();
			}
			foreach (string playerID in PlayerList) {
				if (!idToIgnore.Contains(playerID)) {
					MySockets.Server.GetAUser(playerID).MessageHandler(message);
				}
			}
		}

		public void Loot(User.User looter, List<string> commands, Character.NPC npc) {
			//okay the group looting rule is not free for all thats why we arrived here.  We now need to abide by the looting rule that governs the
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
		private void ChanceLoot(User.User looter, List<string> commands, Character.NPC npc) {
			//we don't have a loot winner yet so let's roll the dice
			if (string.IsNullOrEmpty(MasterLooter)) {
				int highestRoll = 0;
				string winner = null;
				foreach (string player in PlayerList) {
					int currentRoll = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 20);
					if (currentRoll > highestRoll) {
						winner = player;
					}
				}
			}

			//so the looter was the winner or another player in the group is looting a corpse that was previously looted by a previous winner
			if (string.Equals(looter.UserID, MasterLooter) || LastLootedCorpse.Contains(npc.ID)) {
				if (npc.Loot(looter, commands, true)) {
					//if player actually looted something
					MasterLooter = null;
					if (!LastLootedCorpse.Contains(npc.ID)) {
						LastLootedCorpse.Add(npc.ID);
					}
				}
				//we don't need to keep any old ID's since they probably rotted away anyways. Seriously doubt the group looted 100 bodies and then wanted to go
				//back and re-loot one of the 50 first corpses.  This number is subject to change once testing starts happening.
				if (LastLootedCorpse.Count > 100) {
					LastLootedCorpse.RemoveRange(0, 49);
				}
			}
			else {
				MySockets.Server.GetAUser(looter.UserID).MessageHandler("You did not win the loot draw and can not loot this corpse.");
			}

		}

		//the way this works is the current looter can either loot all or loot a specific item and then his turn is over and the next player gets a chance to loot.
		//the next player can choose to loot something from the current corpse
		private void NextPlayerLoots(User.User looter, List<string> commands, Character.NPC npc) {
			if (NextLooterList == null) {
				//create th elist of looters in no particular order
				NextLooterList = new Dictionary<int, string>();
				int index = 0;
				foreach (string player in PlayerList) {
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

				MySockets.Server.GetAUser(looter.UserID).MessageHandler(string.Format("You are not eligible to loot at this time, it will be your turn in {0} more lootings.", playerPosition));
			}
		}

		private void OnlyLeaderLoots(User.User looter, List<string> commands, Character.Iactor npc) {
			if (string.Equals(looter.UserID, LeaderID)) {
				((Character.NPC)npc).Loot(looter, commands, true);
			}
			else {
				looter.MessageHandler("Only the group leader can loot corpses killed by the group.");
			}
		}

		private void MasterLooterLoots(User.User looter, List<string> commands, Character.Iactor npc) {
			if (string.Equals(looter.UserID, MasterLooter)) {
				((Character.NPC)npc).Loot(looter, commands, true);
			}
			else {
				looter.MessageHandler("Only the master looter can loot corpses killed by the group.");
			}
		}

	}
}
