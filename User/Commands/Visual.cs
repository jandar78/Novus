﻿using System;
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
	public  partial class CommandParser {

        private static void Examine(User.User player, List<string> commands) {
            string message = "";
            bool foundIt = false;
            if (commands.Count > 2) {
                //Todo:
                //the player is trying to examine an item, the order will be room,exit, door, items, player, NPCs
                //rooms should have a list of items that belong to the room (non removable) but whihc can be interacted with by the player.  For example a loose brick, oven, fridge, closet, etc.
                //in turn these objects can items that can be removed from the room I.E. food, clothing, weapons, etc.  This is not implemented yet.

                //exits will never, ever, ever be null but just in case you can be teleported to somewhere where there are no exits....

                //Ok let's really think this over, do exits really need an examine? I mean they just lead somewhere.  A door on the other hand could have an inscription on it that if read can open the door.

                Room room = Room.GetRoom(player.Player.Location);
                Door door = FindDoor(player.Player.Location, commands);

                if (door != null) {
                    message = door.Examine;
                    foundIt = true;
                }


                //TODO: items
                if (!foundIt) {
                    //find me an item
                    //foundIt = true;
                }

                if (!foundIt) {
                    List<string> chars = room.GetObjectsInRoom("PLAYERS");
                    foreach (string id in chars) {
                        Character.Character playerChar = MySockets.Server.GetAUser(id).Player as Character.Character;
                        string tempName = playerChar.FirstName + " " + playerChar.LastName;
                        if (commands[2].ToLower().Contains(playerChar.FirstName.ToLower()) || commands[2].ToLower().Contains(playerChar.LastName.ToLower())) {
                            message = playerChar.Examine();
                            foundIt = true;
                            break;
                        }
                    }
                }
                if (!foundIt) {
                    List<string> npcList = room.GetObjectsInRoom("NPCS");

                    MongoUtils.MongoData.ConnectToDatabase();
                    MongoDatabase db = MongoUtils.MongoData.GetDatabase("Characters");
                    MongoCollection npcCollection = db.GetCollection("NPCCharacters");
                    IMongoQuery query = null;
                    
                    foreach (string id in npcList) {
                        query = Query.EQ("_id", ObjectId.Parse(id));

                        BsonDocument result = npcCollection.FindOneAs<BsonDocument>(query);
                       
                        string tempName = result["FirstName"].AsString + " " + result["LastName"].AsString;


                        if (commands[2].ToLower().Contains(result["FirstName"].AsString.ToLower()) || commands[2].ToLower().Contains(result["LastName"].AsString.ToLower())) {
                            
                            string[] position = commands[0].Split('.'); //we are spearating based on using the decimal operator after the name of the npc/item
                            if (position.Count() > 1) {
                                //ok so the player specified a specific NPC in the room list to examine and not just the first to match
                                int pos;
                                int.TryParse(position[position.Count()-1], out pos);
                                if (pos != 0) {
                                    ObjectId objId = new ObjectId();
                                    string idToParse = GetObjectInPosition(pos, commands[2], player.Player.Location);
                                    ObjectId.TryParse(idToParse, out objId);
                                    query = Query.EQ("_id", objId);
                                    result = npcCollection.FindOneAs<BsonDocument>(query);
                                }
                            }

                            if (result != null) {
                                message = result["Description"].AsString;
                                foundIt = true;
                                break;
                            }
                        }
                        
                    }

                }
            }

            if (!foundIt) {
                message = "Examine what?";
            }

            player.MessageHandler(message);
        }

		private static void Look(User.User player, List<string> commands) {
            List<CharacterEnums.CharacterActionState> NonAllowableStates = new List<CharacterEnums.CharacterActionState> { CharacterEnums.CharacterActionState.DEAD,
                CharacterEnums.CharacterActionState.ROTTING, CharacterEnums.CharacterActionState.SLEEPING, CharacterEnums.CharacterActionState.UNCONCIOUS };
            
            StringBuilder sb = new StringBuilder();

            if (!NonAllowableStates.Contains(player.Player.ActionState)) {
                if (commands.Contains(" in ")) {
                    LookIn(player, commands);
                    return;
                }
                Room room = Room.GetRoom(player.Player.Location);
                room.GetRoomExits();
                List<Exits> exitList = room.RoomExits;


                sb.AppendLine(("- " + room.Title + " -\t\t\t").FontStyle(Utils.FontStyles.BOLD));
                sb.AppendLine(room.Description);

                string[] vowel = new string[] { "a", "e", "i", "o", "u" };
                foreach (Exits exit in exitList) {

                    if (room.IsDark) {
                        exit.Description = "something";
                    }

                    if (string.IsNullOrEmpty(exit.Description)) {
                        exit.Description = exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
                    }

                    if (exit.Description.Contains("that leads to")) {
                        exit.Description += exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
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
                            exit.Description = exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
                        }
                    }

                    if (!exit.Description.Contains("somewhere") && vowel.Contains(exit.Description[0].ToString())) {
                        directionCorrected += "an ";
                    }
                    else if (!exit.Description.Contains("somewhere") && exit.Description != "something") {
                        directionCorrected += "a ";
                    }
                    sb.AppendLine(directionCorrected + exit.Description + ".");
                }

                sb.Append(HintCheck(player));
                sb.Append(DisplayPlayersInRoom(room, player.UserID));
                sb.Append(DisplayItemsInRoom(room));
            }
            else {
                sb.Append(string.Format("You can't look around when you are {0}!", player.Player.Action));
            }

            if (!(player.Player is Character.NPC)) {
                player.MessageHandler(sb.ToString());
            }
		}

        private static void LookIn(User.User player, List<string> commands) {
            commands.RemoveAt(2); //remove "in"
            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;
            Room room = Room.GetRoom(player.Player.Location);

            int location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = -1;
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

            if (location != -1) {//player didn't specify it was in his inventory check room first
                foreach (string itemID in room.GetObjectsInRoom("ITEMS")) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);
                    inventoryItem = KeepOpening(itemNameToGet, inventoryItem, itemPosition, index);

                    if (inventoryItem.Name.Contains(itemNameToGet)) {
                        Items.Icontainer container = inventoryItem as Items.Icontainer;
                        player.MessageHandler(container.LookIn());
                        itemFound = true;
                        break;

                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.GetInventoryAsItemList();
                foreach (Items.Iitem inventoryItem in playerInventory) {
                    if (inventoryItem.Name.Contains(itemNameToGet)) {
                        //if player didn't specify an index number loop through all items until we find the first one we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            Items.Icontainer container = inventoryItem as Items.Icontainer;
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

		private static void DisplayDate(User.User player, List<string> commands) {
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

		private static void DisplayTime(User.User player, List<string> commands) {
			//full, short or whatever combination we feel like allowing the player to grab
			BsonDocument time = Calendar.Calendar.GetTime();
			string message = "";
			if (commands.Count < 3) { commands.Add("SHORT"); } //let's add in the "full" for time full

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
