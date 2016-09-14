using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Extensions;
using Interfaces;
using Sockets;
using Rooms;

namespace Commands {
	public partial class CommandParser {

		//TODO: this needs more work done and also need to figure out a nice way to display it to the user
		private static void DisplayStats(IUser player, List<string> commands) {
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
                foreach (var attrib in player.Player.GetAttributes()) {
                    sb.AppendLine(string.Format("{0,-12}: {1}/{2,-3}    Rank: {3}",attrib.Name.CamelCaseWord(), attrib.Value, attrib.Max, attrib.Rank));
                }

                sb.AppendLine("\n[Sub Attributes]");
                foreach (var attrib in player.Player.GetSubAttributes()) {
                    sb.AppendLine(attrib.Key.CamelCaseWord() + ": \t" + attrib.Value);
                }

                sb.AppendLine("Description/Bio: " + player.Player.Description);
                player.MessageHandler(sb.ToString());
            }
		}

        private static void Inventory(IUser player, List<string> commands){
            List<IItem> inventoryList = player.Player.Inventory.GetInventoryAsItemList();
            StringBuilder sb = new StringBuilder();

            Dictionary<string, int> grouping = new Dictionary<string, int>();

            //let's group repeat items for easier display this may be a candidate for a helper method
            foreach (IItem item in inventoryList) {
                IContainer container = item as IContainer;
                if (!grouping.ContainsKey(item.Name)) {
                    if (!item.ItemType.ContainsKey(ItemsType.CONTAINER)) {
                        grouping.Add(item.Name, 1);
                    }
                    else{
                        grouping.Add(item.Name + " [" + (container.Opened ? "Opened" : "Closed") + "]", 1);
                        container = null;
                    }
                }
                else {
                    if (!item.ItemType.ContainsKey(ItemsType.CONTAINER)) {
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

        private static void Equipment(IUser player, List<string> commands) {
            Dictionary<Wearable, IItem> equipmentList = player.Player.Equipment.GetEquipment();
            StringBuilder sb = new StringBuilder();

            if (equipmentList.Count > 0) {
                sb.AppendLine("You are equipping:");
                foreach (KeyValuePair<Wearable, IItem> pair in equipmentList) {
                    sb.AppendLine("[" + pair.Key.ToString().Replace("_", " ").CamelCaseWord() + "] " + pair.Value.Name);
                }
                sb.AppendLine("\n\r");
            }
            else {
                sb.AppendLine("\n\r[ EMPTY ]\n\r");
            }

            player.MessageHandler(sb.ToString());
        }

        private static void LevelUp(IUser player, List<string> commands) {
            Character.Character temp = player.Player as Character.Character;
            //if we leveled up we should have gotten points to spend assigned before we eve get here
            if (temp != null && (temp.PointsToSpend > 0 || temp.IsLevelUp)) {
                if (temp.Experience >= temp.NextLevelExperience || temp.Leveled) {
                    player.Player.Level += 1;
                    temp.Leveled = false;
                }
                player.CurrentState = UserState.LEVEL_UP;
            }
            else {
                player.MessageHandler("You do not have enough Experience, or points to spend to perform this action.");
            }
        }

		private static void Emote(IUser player, List<string> commands) {
			IMessage message = new Message();
						
			string temp = "";
			if (commands[0].Trim().Length > 6) {
				temp = commands[0].Substring(6).Trim();
			}
			else {
				message.Self = "You go to do something but what?.";
			}

            if (!player.Player.IsNPC && !string.IsNullOrEmpty(temp)) {
                message.Self = player.Player.FirstName + " " + temp + (temp.EndsWith(".") == false ? "." : "");
            }

            IRoom room = Room.GetRoom(player.Player.Location);
			message.Room = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " " + temp + (temp.EndsWith(".") == false ? "." : "");
			message.InstigatorID = player.Player.Id.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
			player.MessageHandler(message.Self);
            room.InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID });
		}

		private static void Say(IUser player, List<string> commands) {
			IMessage message = new Message();
						
			string temp ="";
            if (commands[0].Length > 4)	temp = commands[0].Substring(4).Trim();
            else{
                if (!player.Player.IsNPC) {
                    player.MessageHandler("You decide to stay quiet since you had nothing to say.");
                    return;
                }
            }
            IRoom room = Room.GetRoom(player.Player.Location);

			message.Self = "You say \"" + temp + "\"";
			message.Room = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " says \"" + temp + "\"";
			message.InstigatorID = player.Player.Id.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (player.Player.IsNPC) {
				message.InstigatorType = ObjectType.Npc;
				player.MessageHandler(message);

			}
			else {
				player.MessageHandler(message.Self);
			}

			room.InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID }); 
		}

		private static void SayTo(IUser player, List<string> commands) {
			IMessage message = new Message();
			
			string temp = "";
			if (commands[0].Length > 5)
				temp = commands[0].Substring(6);
			else {
				if (!player.Player.IsNPC) {
					player.MessageHandler("You decide to stay quiet since you had nothing to say.");
					return;
				}
			}

			//let's check for dot operator
			bool HasDotOperator = false;
			int playerPosition = 0;
			string[] position = commands[2].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
			if (position.Count() > 1) {
				int.TryParse(position[position.Count() - 1], out playerPosition);
				HasDotOperator = true;
			}
			IRoom room = Room.GetRoom(player.Player.Location);
			IUser toPlayer = null;
			List<IUser> toPlayerList = new List<IUser>();
			//we need some special logic here, first we'll try by first name only and see if we get a hit.  If there's more than one person named the same
			//then we'll see if the last name was included in the commands. And try again.  If not we'll check for the dot operator and all if else fails tell them
			//to be a bit more specific about who they are trying to directly speak to.
			string[] nameBreakDown = commands[0].ToLower().Split(' ');
			foreach (var id in room.GetObjectsInRoom(RoomObjects.Players, 100)) {
				toPlayerList.Add(Server.GetAUser(id));
			}
			
			if (toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).Count() > 1) { //let's narrow it down by including a last name (if provided)
				toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).Where(p => String.Compare(p.Player.LastName, nameBreakDown[2], true) == 0).SingleOrDefault();

				if (toPlayer == null) { //no match on full name, let's try with the dot operator if they provided one
					if (HasDotOperator && (playerPosition < toPlayerList.Count && playerPosition >= 0)) {
						toPlayer = toPlayerList[playerPosition];
					}
					else {
						toPlayer = toPlayerList[0];
					}
				}
			}
			else { //we found an exact match
				toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).SingleOrDefault();
				
				if (toPlayer != null && toPlayer.UserID == player.UserID) {
					toPlayer = null; //It's the player saying something!
				}
			}

			if (toPlayer == null) { //we are looking for an npc at this point
				toPlayerList.Clear();
				foreach (var id in room.GetObjectsInRoom(RoomObjects.Npcs, 100)) {
					toPlayerList.Add(Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { id }));
				}
				if (toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).Count() > 1) { //let's narrow it down by including a last name (if provided)
					toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).Where(p => String.Compare(p.Player.LastName, nameBreakDown[2], true) == 0).SingleOrDefault();

					if (toPlayer == null) { //no match on full name, let's try with the dot operator if they provided one
						if (HasDotOperator && (playerPosition < toPlayerList.Count && playerPosition >= 0)) {
							toPlayer = toPlayerList[playerPosition];
						}
						else {
							toPlayer = toPlayerList[0];
						}
					}
				}
				else { //we found an exact match
					toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[1]).SingleOrDefault();
					
					if (commands.Count == 2 || toPlayer != null && toPlayer.UserID == player.UserID) {
						toPlayer = null;
						player.MessageHandler("You realize you have nothing to say that you don't already know.");
					}
					else if (toPlayer == null) {
						player.MessageHandler("That person is not here.  You can't speak directly to them.");
					}
				}
			}

			if (toPlayer != null) {
				if (temp.ToLower().StartsWith(toPlayer.Player.FirstName.ToLower())) {
					temp = temp.Substring(toPlayer.Player.FirstName.Length);
				}
				if (temp.ToLower().StartsWith(toPlayer.Player.LastName.ToLower())) {
					temp = temp.Substring(toPlayer.Player.LastName.Length);
				}
				temp = temp.Trim();
				message.Self = "You say to " + (room.IsDark == true ? "Someone" : toPlayer.Player.FirstName) + " \"" + temp + "\"";
				message.Target = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " says to you \"" + temp + "\"";
				message.Room = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " says to " + (room.IsDark == true ? "Someone" : toPlayer.Player.FirstName) + " \"" + temp + "\"";

				message.InstigatorID = player.Player.Id.ToString();
				message.InstigatorType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;
				message.TargetID = toPlayer.UserID.ToString();
				message.TargetType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

				if (player.Player.IsNPC) {
					player.MessageHandler(message);
				}
				else {
					player.MessageHandler(message.Self);
				}

				if (toPlayer.Player.IsNPC) {
					toPlayer.MessageHandler(message);
				}
				else {
					toPlayer.MessageHandler(message.Target);
				}

				room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID, toPlayer.UserID });
			}
		}

		//a whisper is a private message but with a chance that other players may hear what was said, other player has to be in the same room
		//TODO: need to add the ability for others to listen in on the whisper if they have the skill
		private static void Whisper(IUser player, List<string> commands){
			IMessage message = new Message();
			message.InstigatorID = player.Player.Id.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			List<IUser> toPlayerList = new List<IUser>();
            IRoom room = Room.GetRoom(player.Player.Location);
			if (commands.Count > 2){
				if (commands[2].ToUpper() == "SELF") {
					message.Self = "You turn your head towards your own shoulder and whisper quietly to yourself.";
					message.Room = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers into " + player.Player.Gender == "MALE" ? "his" : "her" + " own shoulder.";
				}

				toPlayerList = Server.GetAUserByFirstName(commands[2]).Where(p => p.Player.Location == player.Player.Location).ToList();
			}	

			IUser toPlayer = null;

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
				message.Self = "You try and whisper to " + commands[2].CamelCaseWord() + " but they're not around.";
			}
			else {
				message.TargetID = toPlayer.Player.Id.ToString();
				message.TargetType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

				int startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower() + " " + toPlayer.Player.LastName.ToLower());
				if (startAt == -1 || startAt > 11) {
					startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower());
					fullName = false;
				}

				if (startAt > 11) startAt = 11 + toPlayer.Player.FirstName.Length + 1;
				else startAt += toPlayer.Player.FirstName.Length + 1;
				if (fullName) startAt += toPlayer.Player.LastName.Length + 1;

				if (commands[0].Length > startAt) {
					string whisper = commands[0].Substring(startAt);
					message.Self = "You whisper to " + toPlayer.Player.FirstName + " \"" + whisper + "\"";
					message.Target = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers to you \"" + whisper + "\"";
					//this is where we would display what was whispered to players with high perception?
					message.Room = (room.IsDark == true ? "Someone" : player.Player.FirstName) + " whispers something to " + toPlayer.Player.FirstName;
				}
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID, !toPlayer.Equals(ObjectId.Empty) ? toPlayer.UserID : ObjectId.Empty});
			
		}

		//a tell is a private message basically, location is not a factor
		private static void Tell(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.Player.Id.ToString();
			message.InstigatorType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

			List<IUser> toPlayerList = Server.GetAUserByFirstName(commands[2]).ToList();
			IUser toPlayer = null;
			
			if (commands[2].ToUpper() == "SELF") {
				message.Self = "You go to tell yourself something when you realize you already know it.";
				return;
			}

			if (toPlayerList.Count < 1) {
				message.Self = "There is no one named " + commands[2].CamelCaseWord() + " to tell something.";
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
				message.Self = "There is no one named " + commands[2].CamelCaseWord() + " to tell something.";
			}
			else {
				message.TargetID = toPlayer.Player.Id.ToString();
				message.InstigatorType = toPlayer.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

				int startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower() + " " + toPlayer.Player.LastName.ToLower());
				if (startAt == -1 || startAt > 11) {
					startAt = commands[0].ToLower().IndexOf(toPlayer.Player.FirstName.ToLower());
					fullName = false;
				}

				if (startAt > 11) startAt = 11 + toPlayer.Player.FirstName.Length + 1;
				else startAt += toPlayer.Player.FirstName.Length + 1;
				if (fullName) startAt += toPlayer.Player.LastName.Length + 1;

				if (commands[0].Length > startAt) {
					string temp = commands[0].Substring(startAt);
					message.Self = "You tell " + toPlayer.Player.FirstName + " \"" + temp + "\"";
					message.Target = player.Player.FirstName + " tells you \"" + temp + "\"";
                }
				else {
					message.Self = "You have nothing to tell them.";
				}
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			if (toPlayer.Player.IsNPC) {
				toPlayer.MessageHandler(message);
			}
			else {
				toPlayer.MessageHandler(message.Target);
			}
		}

		private static void Who(IUser player, List<string> commands) {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("PlayerName");
			sb.AppendLine("----------");
			Server.GetCurrentUserList()
				.Where(u => u.CurrentState == UserState.TALKING)
				.OrderBy(u => u.Player.FirstName)
				.ToList()
				.ForEach(u => sb.AppendLine(u.Player.FirstName + " " + u.Player.LastName));
			
			player.MessageHandler(sb.ToString());
		}

		private static void Help(IUser player, List<string> commands) {
			StringBuilder sb = new StringBuilder();

            if (commands.Count < 3 || commands.Contains("all")) { //display just the names
                var cursor = MongoUtils.MongoData.RetrieveObjects<BsonDocument>("Commands", "Help", null);
                foreach (BsonDocument doc in cursor) {
                    sb.AppendLine(doc["_id"].ToString());
                }
            }
            else { //user specified a specific command so we will display the explanation and example
                var doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>("Commands", "Help", b => b["_id"] == commands[2].ToUpper());
                sb.AppendLine(doc["_id"].ToString());
                sb.AppendLine(doc["explanation"].AsString);
                sb.AppendLine(doc["Example"].AsString);
            }
            
			player.MessageHandler(sb.ToString());
		}

        public static async void ReportBug(IUser player, List<string> commands) {
            MongoUtils.MongoData.ConnectToDatabase();
            var bugCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("Logs", "Bugs");

            BsonDocument doc = new BsonDocument{
                { "ReportedBy", BsonValue.Create(player.UserID) },
                { "DateTime", BsonValue.Create(DateTime.Now.ToUniversalTime()) },
                { "Resolved", BsonValue.Create(false) },
                { "Issue", BsonValue.Create(commands[0].Substring("bug".Length + 1)) }
            };

            await bugCollection.InsertOneAsync(doc);

            if (player.Player != null) { //could be an internal bug being reported
                player.MessageHandler("Your bug has been reported, thank you.");
            }
        }
	}
}
