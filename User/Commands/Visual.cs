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
	public  partial class CommandParser {

        private static void Examine(IUser player, List<string> commands) {
            string message = "";
            bool foundIt = false;
            if (commands.Count > 2) {
                //order is room, door, items, player, NPCs

                //rooms should have a list of items that belong to the room (non removable) but which can be interacted with by the player.  For example a loose brick, oven, fridge, closet, etc.
                //in turn these objects can have items that can be removed from the room I.E. food, clothing, weapons, etc.

                IRoom room = Room.GetRoom(player.Player.Location);
                IDoor door = FindDoor(player.Player.Location, commands);

                if (door != null) {
                    message = door.Examine;
                    foundIt = true;
                }


                //TODO: For items and players we need to be able to use the dot operator to discern between multiple of the same name.
                //look for items in room, then inventory, finally equipment.  What about another players equipment?
                //maybe the command could be "examine [itemname] [playername]" or "examine [itemname] equipment/inventory"
                if (!foundIt) {
                   message = FindAnItem(commands, player, room, out foundIt);
                    
                }

                if (!foundIt) {
                    message = FindAPlayer(commands, room, out foundIt);
                    
                }
                if (!foundIt) {
                    message = FindAnNpc(commands, room, out foundIt);
                   

                }
            }

            if (!foundIt) {
                message = "Examine what?";
            }

            player.MessageHandler(message);
        }

        private static string FindAnNpc(List<string> commands, IRoom room, out bool foundIt) {
            foundIt = false;
            string message = null;

            var npcList = room.GetObjectsInRoom(RoomObjects.Npcs);

            var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            IMongoQuery query = null;

            foreach (var id in npcList) {
                query = Query.EQ("_id", id);
                var result = MongoUtils.MongoData.RetrieveObject<NPC>(npcCollection, n => n.Id.Equals(id));

                string tempName = result.FirstName + " " + result.LastName;

                if (commands[2].ToLower().Contains(result.FirstName.ToLower()) || commands[2].ToLower().Contains(result.LastName.ToLower())) {

                    string[] position = commands[0].Split('.'); //we are spearating based on using the decimal operator after the name of the npc/item
                    if (position.Count() > 1) {
                        //ok so the player specified a specific NPC in the room list to examine and not just the first to match
                        int pos;
                        int.TryParse(position[position.Count() - 1], out pos);
                        if (pos != 0) {
                            var idToParse = GetObjectInPosition(pos, commands[2], room.Id);                            
                            result = MongoUtils.MongoData.RetrieveObject<NPC>(npcCollection, n => n.Id.Equals(idToParse));
                        }
                    }

                    if (result != null) {
                        message = result.Description;
                        foundIt = true;
                        break;
                    }
                }
            }

            return message;
        }

        private static string FindAPlayer(List<string> commands, IRoom room, out bool foundIt) {
            foundIt = false;
            string message = null;
            var chars = room.GetObjectsInRoom(RoomObjects.Players);
            foreach (var id in chars) {
                Character.Character playerChar = Server.GetAUser(id).Player as Character.Character;
                string tempName = playerChar.FirstName + " " + playerChar.LastName;
                if (commands[2].ToLower().Contains(playerChar.FirstName.ToLower()) || commands[2].ToLower().Contains(playerChar.LastName.ToLower())) {
                    message = playerChar.Examine();
                    foundIt = true;
                    break;
                }
            }

            return message;
        }

        private static string FindAnItem(List<string> commands, IUser player, IRoom room, out bool foundIt) {
            foundIt = false;
            string message = null;

            var itemsInRoom = room.GetObjectsInRoom(RoomObjects.Items);
   
            foreach (var id in itemsInRoom) {
                IItem item = Items.ItemFactory.CreateItem(id).Result;
                if (commands[2].ToLower().Contains(item.Name.ToLower())) {
                    message = item.Examine();
                    foundIt = true;
                    break;
                }
            }

            if (!foundIt) { //not in room check inventory
                List<IItem> inventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (IItem item in inventory) {
                    if (commands[2].ToLower().Contains(item.Name.ToLower())) {
                        message = item.Examine();
                        foundIt = true;
                        break;
                    }
                }
            }

            if (!foundIt) { //check equipment
                Dictionary<Wearable, IItem> equipment = player.Player.Equipment.GetEquipment();
                foreach (IItem item in equipment.Values) {
                    if (commands[2].ToLower().Contains(item.Name.ToLower())) {
                        message = item.Examine();
                        foundIt = true;
                        break;
                    }
                }
            }

            return message;
        }

		private static void Look(IUser player, List<string> commands) {
            List<CharacterActionState> NonAllowableStates = new List<CharacterActionState> { CharacterActionState.Dead,
                CharacterActionState.Rotting, CharacterActionState.Sleeping, CharacterActionState.Unconcious };
            
            StringBuilder sb = new StringBuilder();

            if (!NonAllowableStates.Contains(player.Player.ActionState)) {
                if (commands.Count > 2 && commands[2] == "in") {
                    LookIn(player, commands);
                    return;
                }

                //let's build the description the player will see
                IRoom room = Room.GetRoom(player.Player.Location);
                room.GetRoomExits();
                var exitList = room.RoomExits;
                                
                sb.AppendLine(("- " + room.Title + " -\t\t\t").FontStyle(Utils.FontStyles.BOLD));
                //TODO: add a "Descriptive" flag, that we will use to determine if we need to display the room description.  Should be a player level
				//config we can probably store them in a dictionary called Settings and make a method that turns them on or off.  User would just type
				//Set Descriptions and the method would toggle the bit and then display back to the user the current setting ON/OFF
                sb.AppendLine(room.Description);
                sb.Append(HintCheck(player));
               
                foreach (IExit exit in exitList) {
                    sb.AppendLine(GetExitDescription(exit, room));
                }

                sb.Append(DisplayPlayersInRoom(room, player.UserID));
                sb.Append(DisplayItemsInRoom(room));
            }
            else {
                sb.Append(string.Format("You can't look around when you are {0}!", player.Player.Action));
            }
           
           player.MessageHandler(sb.ToString());
            
		}

        private static string GetExitDescription(IExit exit, IRoom room) {
            //there's a lot of sentence  
            string[] vowel = new string[] { "a", "e", "i", "o", "u" };

			RoomExits exitDirection = (RoomExits)Enum.Parse(typeof(RoomExits), exit.Direction.CamelCaseWord());
            if (room.IsDark) {
                exit.Description = "something";
            }

            if (string.IsNullOrEmpty(exit.Description)) {
                exit.Description = exit.availableExits[exitDirection].Title.ToLower();
            }

            if (exit.Description.Contains("that leads to")) {
                exit.Description += exit.availableExits[exitDirection].Title.ToLower();
            }

            string directionCorrected = "To the " + exit.Direction.CamelCaseWord().FontColor(Utils.FontForeColor.CYAN) + " there is ";

            if (String.Compare(exit.Direction, "up", true) == 0 || String.Compare(exit.Direction, "down", true) == 0) {
                if (!room.IsDark) {
                    directionCorrected = exit.Description.UppercaseFirstWordInString();
                }
                else {
                    directionCorrected = "something";
                }

                directionCorrected += " leads " + exit.Direction.CamelCaseWord().FontColor(Utils.FontForeColor.CYAN) + " towards ";

                if (!room.IsDark) {
                    exit.Description = "somewhere";
                }
                else {
                    exit.Description = exit.availableExits[exitDirection].Title.ToLower();
                }
            }

            if (!exit.Description.Contains("somewhere") && vowel.Contains(exit.Description[0].ToString())) {
                directionCorrected += "an ";
            }
            else if (!exit.Description.Contains("somewhere") && exit.Description != "something") {
                directionCorrected += "a ";
            }

            return (directionCorrected + exit.Description + ".");
        }

        private async static void LookIn(IUser player, List<string> commands) {
            commands.RemoveAt(2); //remove "in"
            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;
            IRoom room = Room.GetRoom(player.Player.Location);

            var location = "";
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
               itemNameToGet = itemNameToGet.Remove(itemNameToGet.Length - 2, 2);
            }

            int index = 1;

            if (location.Equals(ObjectId.Empty)) {//player didn't specify it was in his inventory check room first
                foreach (var itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem inventoryItem = await Items.Items.GetByID(itemID);
                    inventoryItem = KeepOpening(itemNameToGet, inventoryItem, itemPosition, index);

                    if (inventoryItem.Name.Contains(itemNameToGet)) {
                        IContainer container = inventoryItem as IContainer;
                        player.MessageHandler(container.LookIn());
                        itemFound = true;
                        break;

                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (IItem inventoryItem in playerInventory) {
                    if (inventoryItem.Name.Contains(itemNameToGet)) {
                        //if player didn't specify an index number loop through all items until we find the first one we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            IContainer container = inventoryItem as IContainer;
                            player.MessageHandler(container.LookIn());
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }
        }

		private static void DisplayDate(IUser player, List<string> commands) {
			//full, short or whatever combination we feel like allowing the player to grab
			Dictionary<string, string> dateInfo = Calendar.Calendar.GetDate();
			string message = "";
			if (commands.Count < 3) { commands.Add("FULL"); } //let's add in the "full" for date full
			string inth = "";
			if (dateInfo["DayInMonth"].Substring(dateInfo["DayInMonth"].Length - 1) == "1") inth = "st";
            else if (dateInfo["DayInMonth"].Substring(dateInfo["DayInMonth"].Length - 1) == "2") inth = "nd";
            else if (dateInfo["DayInMonth"].Substring(dateInfo["DayInMonth"].Length - 1) == "3") inth = "rd";
			else inth = "th";
			switch (commands[2].ToUpper()) {
				case "SHORT":
					message = String.Format("\r{0}, {1}{2} of {3}, {4}.\n", dateInfo["DayInWeek"], dateInfo["DayInMonth"], inth, dateInfo["Month"], dateInfo["Year"]);
					break;
				case "FULL":
				default:
					message = String.Format("\r{0}, on the {1}{2} day of the month of {3}, {4} the year of the {5}.\n", dateInfo["DayInWeek"], dateInfo["DayInMonth"], inth, dateInfo["Month"], dateInfo["Year"], dateInfo["YearOf"]);
					break;
			}

			player.MessageHandler(message);
		}

		private static void DisplayTime(IUser player, List<string> commands) {
			//full, short or whatever combination we feel like allowing the player to grab
			BsonDocument time = Calendar.Calendar.GetTime();
			string message = "";
			if (commands.Count < 3) { commands.Add("FULL"); } //let's add in the "full" for time full

			string amPm = time["Hour"].AsInt32 > 12 ? "PM" : "AM";

			if (!player.HourFormat24) {
				int hour = time["Hour"].AsInt32 > 12 ? time["Hour"].AsInt32 - 12 : time["Hour"].AsInt32;
			}

			switch (commands[2].ToUpper()) {
				case "SHORT":
					message = String.Format("\rCurrent Time: {0:D2}:{1:D2}:{2:D2} {3}.\n", time["Hour"].AsInt32, time["Minute"].AsInt32, time["Second"].AsInt32, amPm);
					break;
				case "FULL":
				default:
					message = String.Format("\rCurrent Time: {0}, {1:D2}:{2:D2}:{3:D2} {4}.\n", time["TimeOfDay"].AsString.CamelCaseWord(), time["Hour"].AsInt32, time["Minute"].AsInt32, time["Second"].AsInt32, amPm);
					break;
			}

			player.MessageHandler(message);
		}
	}
}
