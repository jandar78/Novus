using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rooms;
using User;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Extensions;

namespace Commands {
	public partial class CommandParser {

		//TODO: this needs more work done and also need to figure out a nice way to display it to the user
		private static void DisplayStats(User.User player, List<string> commands) {
            Character.Character character = player.Player as Character.Character;
            if (character != null) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("You are currently " + character.ActionState.ToString().ToUpper() + " and " + character.StanceState.ToString().ToUpper());
                sb.AppendLine("\nName: " + character.FirstName.CamelCaseWord() + " " + character.LastName.CamelCaseWord());
                sb.AppendLine("Level : " + character.Level); 
                sb.AppendLine("Class: " + character.Class);
                sb.AppendLine("Race: " + character.Race);
                sb.AppendLine("XP: " + (long)character.Experience);
                sb.AppendLine("Next Level XP required: " + (long)character.NextLevelExperience + " (Needed: " + (long)(character.NextLevelExperience - character.Experience) + ")");
                sb.AppendLine("\n[Attributes]");
                foreach (KeyValuePair<string, Character.Attribute> attrib in player.Player.GetAttributes()) {
                    sb.AppendLine(string.Format("{0,-12}: {1}/{2,-3}    Rank: {3}",attrib.Key.CamelCaseWord(), attrib.Value.Value, attrib.Value.Max, attrib.Value.Rank));
                }

                sb.AppendLine("\n[Sub Attributes]");
                foreach (KeyValuePair<string, double> attrib in player.Player.GetSubAttributes()) {
                    sb.AppendLine(attrib.Key.CamelCaseWord() + ": \t" + attrib.Value);
                }

                sb.AppendLine("Description/Bio: " + player.Player.Description);
                player.MessageHandler(sb.ToString());
            }
		}

        private static void Inventory(User.User player, List<string> commands){
            List<Items.Iitem> inventoryList = player.Player.Inventory.GetInventoryAsItemList();
            StringBuilder sb = new StringBuilder();

            Dictionary<string, int> grouping = new Dictionary<string, int>();

            //let's group repeat items for easier display this may be a candidate for a helper method
            foreach (Items.Iitem item in inventoryList) {
                Items.Icontainer container = item as Items.Icontainer;
                if (!grouping.ContainsKey(item.Name)) {
                    if (!item.ItemType.ContainsKey(Items.ItemsType.CONTAINER)) {
                        grouping.Add(item.Name, 1);
                    }
                    else{
                        grouping.Add(item.Name + " [" + (container.Opened ? "Opened" : "Closed") + "]", 1);
                        container = null;
                    }
                }
                else {
                    if (!item.ItemType.ContainsKey(Items.ItemsType.CONTAINER)) {
                        grouping[item.Name] += 1;
                    }
                    else {
                        grouping[item.Name + " [" + (container.Opened ? "Opened" : "Closed") + "]"] += 1;
                        container = null;
                    }
                }
            }

            if (grouping.Count > 0) {
                sb.AppendLine("You are carrying:");
                foreach (KeyValuePair<string, int> pair in grouping) {
                    sb.AppendLine(pair.Key.CamelCaseString() + (pair.Value > 1 ? "[" + pair.Value + "]" : ""));
                }
                sb.AppendLine("\n\r");
            }
            else{
                sb.AppendLine("\n\r[ EMPTY ]\n\r");
            }
            
            player.MessageHandler(sb.ToString());
        }

        private static void Equipment(User.User player, List<string> commands) {
            Dictionary<Items.Wearable, Items.Iitem> equipmentList = player.Player.Equipment.GetEquipment();
            StringBuilder sb = new StringBuilder();

            if (equipmentList.Count > 0) {
                sb.AppendLine("You are equipping:");
                foreach (KeyValuePair<Items.Wearable, Items.Iitem> pair in equipmentList) {
                    sb.AppendLine("[" + pair.Key.ToString().Replace("_", " ").CamelCaseWord() + "] " + pair.Value.Name);
                }
                sb.AppendLine("\n\r");
            }
            else {
                sb.AppendLine("\n\r[ EMPTY ]\n\r");
            }

            player.MessageHandler(sb.ToString());
        }

        private static void LevelUp(User.User player, List<string> commands) {
            Character.Character temp = player.Player as Character.Character;
            //if we leveled up we should have gotten points to spend assigned before we eve get here
            if (temp != null && (temp.PointsToSpend > 0 || temp.IsLevelUp)) {
                if (temp.Experience >= temp.NextLevelExperience || temp.Leveled) {
                    player.Player.Level += 1;
                    temp.Leveled = false;
                }
                player.CurrentState = User.User.UserState.LEVEL_UP;
            }
            else {
                player.MessageHandler("You do not have enough Experience, or points to spend to perform this action.");
            }
        }

		private static void Emote(User.User player, List<string> commands) {
			string temp = "";
			if (commands[0].Trim().Length > 6) temp = commands[0].Substring(6).Trim();
			else {
				player.MessageHandler("You go to do something but what?.");
				return;
			}
            if (!player.Player.IsNPC) {
                player.MessageHandler(player.Player.FirstName + " " + temp + (temp.EndsWith(".") == false ? "." : ""));
            }
            Room room = Room.GetRoom(player.Player.Location);
            room.InformPlayersInRoom((room.IsDark == true ? "Someone" : player.Player.FirstName) + " " + temp + (temp.EndsWith(".") == false ? "." : ""), new List<string>(new string[] { player.UserID }));
		}

		private static void Say(User.User player, List<string> commands) {
            string temp ="";
            if (commands[0].Length > 4)	temp = commands[0].Substring(4);
            else{
                if (player.Player.IsNPC) {
                    player.MessageHandler("You decide to stay quiet since you had nothing to say.");
                    return;
                }
            }
            Room room = Room.GetRoom(player.Player.Location);
            player.MessageHandler("You say \"" + temp + "\"");
			room.InformPlayersInRoom((room.IsDark == true ? "Someone" : player.Player.FirstName) + " says \"" + temp + "\"",
				new List<string>(new string[] { player.UserID })); 
		}

	    //a whisper is a private message but with a chance that other players may hear what was said, other player has to be in the same room
        //TODO: need to add the ability for others to listen in on the whisper if they have the skill
		private static void Whisper(User.User player, List<string> commands){
			List<User.User> toPlayerList = new List<User.User>();
            Room room = Room.GetRoom(player.Player.Location);
			if (commands.Count > 2){
				if (commands[2].ToUpper() == "SELF") {
					player.MessageHandler("You turn your head towards your own shoulder and whisper quietly to yourself.");
					room.InformPlayersInRoom((room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers into " + player.Player.Gender == "MALE" ? "his" : "her" + " own shoulder.", 
						new List<string>(new string[] { player.UserID })); ;
					return;
				}

				toPlayerList = MySockets.Server.GetAUserByName(commands[2]).Where(p => p.Player.Location == player.Player.Location).ToList();
			}	

			User.User toPlayer = null;
			string message = "";

			if (toPlayerList.Count < 1) {
				player.MessageHandler("You whisper to no one. Creepy.");
				return;
			}
			else if (toPlayerList.Count > 1) { //let's narrow it down by including a last name (if provided)
				toPlayer = toPlayerList.Where(p => String.Compare(p.Player.LastName, commands[3], true) == 0).SingleOrDefault();
			}
			else {
				if (toPlayerList.Count == 1) {
					toPlayer = toPlayerList[0];
				}
				if (commands.Count == 2 || toPlayer.UserID == player.UserID) {
						player.MessageHandler("You realize you have nothing to whisper about.");
						return;
					}
			}

			bool fullName = true;
			if (toPlayer == null) {
				message = "You try and whisper to " + commands[2].CamelCaseWord() + " but they're not around.";
			}
			else {
				int startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower() + " " + toPlayer.Player.LastName.ToLower());
				if (startAt == -1 || startAt > 11) {
					startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower());
					fullName = false;
				}

				if (startAt > 11) startAt = 11 + toPlayer.Player.FirstName.Length + 1;
				else startAt += toPlayer.Player.FirstName.Length + 1;
				if (fullName) startAt += toPlayer.Player.LastName.Length + 1;

				if (commands[0].Length > startAt) {
					message = commands[0].Substring(startAt);
					player.MessageHandler("You whisper to " + toPlayer.Player.FirstName + " \"" + message + "\"");
					toPlayer.MessageHandler((room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers to you \"" + message + "\"");
					//this is where we would display what was whispered to players with high perception?
					room.InformPlayersInRoom((room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers something to " + toPlayer.Player.FirstName, new List<string>(new string[] { player.UserID, toPlayer.UserID }));
				}
			}
		}

		//a tell is a private message basically, location is not a factor
		private static void Tell(User.User player, List<string> commands) {
			List<User.User> toPlayerList = MySockets.Server.GetAUserByName(commands[2]).ToList();
			User.User toPlayer = null;
			string message = "";

			if (commands[2].ToUpper() == "SELF") {
				player.MessageHandler("You go to tell yourself something when you realize you already know it.");
				return;
			}

			if (toPlayerList.Count < 1) {
				message = "There is no one named " + commands[2].CamelCaseWord() + " to tell something.";
			}
			else if (toPlayerList.Count > 1) { //let's narrow it down by including a last name (if provided)
				toPlayer = toPlayerList.Where(p => String.Compare(p.Player.LastName, commands[3], true) == 0).SingleOrDefault();
			}
			else {
				toPlayer = toPlayerList[0];
				if (toPlayer.UserID == player.UserID) {
					player.MessageHandler("You tell yourself something important.");
					return;
				}
			}

		   bool fullName = true;
			if (toPlayer == null) {
				message = "There is no one named " + commands[2].CamelCaseWord() + " to tell something.";
			}
			else {
				int startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower() + " " + toPlayer.Player.LastName.ToLower());
				if (startAt == -1 || startAt > 11) {
					startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower());
					fullName = false;
				}

				if (startAt > 11) startAt = 11 + toPlayer.Player.FirstName.Length + 1;
				else startAt += toPlayer.Player.FirstName.Length + 1;
				if (fullName) startAt += toPlayer.Player.LastName.Length + 1;

				if (commands[0].Length > startAt) {
					message = commands[0].Substring(startAt);
					player.MessageHandler("You tell " + toPlayer.Player.FirstName + " \"" + message + "\"");
					toPlayer.MessageHandler(player.Player.FirstName + " tells you \"" + message + "\"");
				}
				else {
					player.MessageHandler("You have nothing to tell them.");
				}
			}
		}

		private static void Who(User.User player, List<string> commands) {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("PlayerName");
			sb.AppendLine("----------");
			MySockets.Server.GetCurrentUserList()
				.Where(u => u.CurrentState == User.User.UserState.TALKING)
				.OrderBy(u => u.Player.FirstName)
				.ToList()
				.ForEach(u => sb.AppendLine(u.Player.FirstName + " " + u.Player.LastName));
			
			player.MessageHandler(sb.ToString());
		}

		private static void Help(User.User player, List<string> commands) {
			StringBuilder sb = new StringBuilder();
            MongoCollection helpCollection = MongoUtils.MongoData.GetCollection("Commands", "Help");

            if (commands.Count < 3 || commands.Contains("all")) { //display just the names
                MongoCursor cursor = helpCollection.FindAllAs<BsonDocument>();
                foreach (BsonDocument doc in cursor) {
                    sb.AppendLine(doc["_id"].ToString());
                }
            }
            else { //user specified a specific command so we will display the explanation and example
                BsonDocument doc = helpCollection.FindOneAs<BsonDocument>(Query.Matches("_id", commands[2].ToUpper()));
                sb.AppendLine(doc["_id"].ToString());
                sb.AppendLine(doc["explanation"].AsString);
                sb.AppendLine(doc["Example"].AsString);
            }
            
			player.MessageHandler(sb.ToString());
		}

        public static void ReportBug(User.User player, List<string> commands) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("Logs");
            MongoCollection col = db.GetCollection("Bugs");
            BsonDocument doc = new BsonDocument();

            doc.Add("ReportedBy", BsonValue.Create(player.UserID));
            doc.Add("DateTime", BsonValue.Create(DateTime.Now.ToUniversalTime()));
            doc.Add("Resolved", BsonValue.Create(false));
            doc.Add("Issue", BsonValue.Create(commands[0].Substring("bug".Length + 1)));

            col.Save(doc);

            if (player.Player != null) { //could be an internal bug being reported
                player.MessageHandler("Your bug has been reported, thank you.");
            }
        }
	}
}
