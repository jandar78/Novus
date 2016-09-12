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
		
		public static void Move(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
     		message.InstigatorType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

			if (!player.Player.InCombat) {

                bool foundExit = false;
                string direction = commands[1].ToLower();
				RoomExits directionToGo = RoomExits.None;
                var roomCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("Commands", "General"); //this is where general command messages are stored

                IRoom room = Room.GetRoom(player.Player.Location);
                room.GetRoomExits();

                if (direction.ToUpper() == RoomExits.North.ToString().ToUpper()) directionToGo = RoomExits.North;
                else if (direction.ToUpper() == RoomExits.South.ToString().ToUpper()) directionToGo = RoomExits.South;
                else if (direction.ToUpper() == RoomExits.East.ToString().ToUpper()) directionToGo = RoomExits.East;
                else if (direction.ToUpper() == RoomExits.West.ToString().ToUpper()) directionToGo = RoomExits.West;
                else if (direction.ToUpper() == RoomExits.Up.ToString().ToUpper()) directionToGo = RoomExits.Up;
                else if (direction.ToUpper() == RoomExits.Down.ToString().ToUpper()) directionToGo = RoomExits.Down;

				foreach (var exit in room.RoomExits) {
                    if (exit.availableExits.ContainsKey(directionToGo)) {

                        //is there a door blocking the exit?
                        bool blocked = false;
                        if (exit.doors.Count > 0 && !exit.doors[directionToGo].Open && !exit.doors[directionToGo].Destroyed) {
                            blocked = true;
                        }

						if (!blocked) {
							player.Player.LastLocation = player.Player.Location;
							player.Player.Location = exit.availableExits[directionToGo].Id;
							player.Player.Save();

                            var leave = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "Leaves");

							//so we don't sound retarded when making the output string
							if (directionToGo == RoomExits.Up) {
								direction = "above";
							}
							else if (directionToGo == RoomExits.Down) {
								direction = "below";
							}


							string who = player.Player.FirstName;

							if (room.IsDark) {
								who = "Someone";
								direction = "somewhere";
							}


							//if the player was just hiding and moves he shows himself
							if (player.Player.ActionState == CharacterActionState.Hiding) {
								PerformSkill(player, new List<string>(new string[] { "Hide", "Hide" }));
							}

							//when sneaking the skill displays the leave/arrive message
							if (player.Player.ActionState != CharacterActionState.Sneaking) {
								message.Room = String.Format(leave["ShowOthers"].AsString, who, direction);
								Room.GetRoom(player.Player.LastLocation).InformPlayersInRoom(message, new List<string>() { player.UserID });
							}

							//now we reverse the direction
							if (directionToGo == RoomExits.North)
								directionToGo = RoomExits.South;
							else if (directionToGo == RoomExits.South)
								directionToGo = RoomExits.North;
							else if (directionToGo == RoomExits.East)
								directionToGo = RoomExits.West;
							else if (directionToGo == RoomExits.Down)
								directionToGo = RoomExits.Up;
							else if (directionToGo == RoomExits.Up)
								directionToGo = RoomExits.Down;
							else
								directionToGo = RoomExits.East;

							var arrive = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "Arrives");
                            room = Room.GetRoom(player.Player.Location); //need to get the new room player moved into

							if (room.IsDark) {
								who = "Someone";
								direction = "somewhere";
							}
							else {
								who = player.Player.FirstName;
							}

							if (directionToGo == RoomExits.Up) {
								direction = "above";
							}
							else if (directionToGo == RoomExits.Down) {
								direction = "below";
							}

							if (!player.Player.IsNPC) {
								Look(player, commands);
							}
							ApplyRoomModifier(player);

							if (player.Player.ActionState != CharacterActionState.Sneaking) {
								if (directionToGo == RoomExits.Up || directionToGo == RoomExits.Down) {
									message.Room = String.Format(arrive["ShowOthers"].AsString.Replace("the {1}","{1}"), who, directionToGo.ToString());
								}
								else {
									message.Room = String.Format(arrive["ShowOthers"].AsString, who, directionToGo.ToString());
								}
							}

							foundExit = true;
							break;
						}
						else {//uh-oh there's a door and it's closed
							foundExit = true; //we did find an exit it just happens to be blocked by a door
							var noExit = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "NoExit");
							message.Self = noExit["ShowSelf"].AsString;
							noExit = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "Blocked"); 
							message.Self += " " + noExit["ShowSelf"];
							noExit = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "ClosedDoor");
							message.Self = message.Self.Remove(message.Self.Length - 1); //get rid of previous line period
							message.Self += " because " + String.Format(noExit["ShowSelf"].AsString.ToLower(), exit.doors[directionToGo].Description.ToLower());

							break;
						}
                    }
                    else {
                        continue;
                    }
                }
				if (!foundExit) {
					var noExit = MongoUtils.MongoData.RetrieveObject<BsonDocument>(roomCollection, r => r["_id"] == "NoExit");
					message.Self = noExit["ShowSelf"].AsString + "\r\n";

				}
            }
            else {
				if (player.Player.InCombat) {
					message.Self = "You can't do that while you are in combat!";
				}
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>() { player.UserID });
		}

		#region Open things
        public static bool OpenDoor(string roomID, string doorDirection) {
            IDoor door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                if (!door.Open && !door.Locked && !door.Destroyed) {
                    OpenADoor(door);
                    return true;
                }
            }
            return false;
        }

        public static bool OpenDoorOverride(string roomID, string doorDirection) {
            IDoor door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                    OpenADoor(door);
                    return true;
                }
            return false;
        }

		private static void Open(IUser player, List<string> commands) {
			IDoor door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				OpenDoor(player, door);
                return;
			}

            //ok not a door so then we'll check containers in the room
            OpenContainer(player, commands);
		}

        private async static void OpenContainer(IUser player, List<string> commands) {
			//this is a quick work around for knowing which container to open without implementing the dot operator
			//I need to come back and make it work like with NPCS once I've tested everything works correctly
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			string location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = null;
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }
            
            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
            }
            
            int index = 1;
            IRoom room = Room.GetRoom(location);
            if (!string.IsNullOrEmpty(location)) {//player didn't specify it was in his inventory check room first
                foreach (string itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem inventoryItem = await Items.Items.GetByID(itemID);
                    inventoryItem = KeepOpening(itemNameToGet, inventoryItem, itemPosition);

                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        if (index == itemPosition) {
                            IContainer container = inventoryItem as IContainer;
                            player.MessageHandler(container.Open());
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (IItem inventoryItem in playerInventory) {
                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        //if player didn't specify an index number loop through all items until we find the want we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            IContainer container = inventoryItem as IContainer;
                            message.Self = container.Open();
							message.Room = player.Player.FirstName + " opens " + inventoryItem.Name.ToLower();
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }

            if (!itemFound) {
                message.Self ="Open what?";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<string>() { player.UserID });
		}

        private static IItem KeepOpening(string itemName, IItem item, int itemPosition = 1, int itemIndex = 1) {
            IContainer container = item as IContainer;

            if (item.ItemType.ContainsKey(ItemsType.CONTAINER) && container.Contents.Count > 0) {
                foreach (string innerID in container.GetContents()) {
                    IItem innerItem = Items.Items.GetByID(innerID).Result;
                    if (innerItem != null && KeepOpening(itemName, innerItem, itemPosition, itemIndex).Name.Contains(itemName)) {
                        if (itemIndex == itemPosition) {
                            return innerItem;
                        }
                        else {
                            itemIndex++;
                        }
                    }
                }
            }

            return item;
        }

        private static void OpenADoor(IDoor door) {
            door.Open = true;
            door.UpdateDoorStatus();
        }

		private static void OpenDoor(IUser player, IDoor door) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
			IRoom room = Room.GetRoom(player.Player.Location);

			if (!player.Player.InCombat) {
                if (!room.IsDark) {
                    if (door.Openable) {
                        if (!door.Open && !door.Locked && !door.Destroyed) {
                            door.Open = true;
                            door.UpdateDoorStatus();
                            message.Self = String.Format("You open {0} {1}.", GetArticle(door.Description[0]), door.Description);
                            message.Room = String.Format("{0} opens {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description);
                        }
                        else if (door.Open && !door.Destroyed) {
                            message.Self = "It's already open.";
                        }
                        else if (door.Locked && !door.Destroyed) {
                            message.Self ="You can't open it because it is locked.";
                        }
                        else if (door.Destroyed) {
                            message.Self = "It's more than open it's in pieces!";
                        }
                    }
                    else {
                        message.Self ="It can't be opened.";
                    }
                }
                else {
                    message.Self ="You can't see anything! Let alone what you are trying to open.";
                }
            }
            else {
                player.MessageHandler("You are in the middle of combat, there are more pressing matters at hand than opening something.");
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<string>() { player.UserID });
		}
		#endregion
		
		#region Close things
		private static void Close(IUser player, List<string> commands) {
			List<string> message = new List<string>();

			IDoor door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				CloseDoor(player, door);
                return;
			}

            CloseContainer(player, commands);
           
		}

        private async static void CloseContainer(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
			string location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = null;
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }

            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;

            IRoom room = Room.GetRoom(player.Player.Location);

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
            }
            int index = 1;

            //Here is the real problem, how do I differentiate between a container in the room and one in the players inventory?
            //if a backpack is laying on the ground th eplayer should be able to put stuff in it or take from it, same as if it were
            //in his inventory.  I should probably check room containers first then player inventory otherwise the player can 
            //specify "inventory" to just do it in their inventory container.

            if (!string.IsNullOrEmpty(location)) {//player didn't specify it was in his inventory check room first
                foreach (string itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem roomItem = await Items.Items.GetByID(itemID);
                    if (string.Equals(roomItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        if (index == itemPosition) {
                            IContainer container = roomItem as IContainer;
                            message.Self = container.Close();
							message.Room = player.Player.FirstName + " closes " + roomItem.Name.ToLower();
                            itemFound = true;
                            break;
                        }
                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (IItem inventoryItem in playerInventory) {
                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        //if player didn't specify an index number loop through all items until we find the want we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            IContainer container = inventoryItem as IContainer;
                            message.Self = container.Close();
							message.Room = player.Player.FirstName + " closes " + inventoryItem.Name.ToLower();
							itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }

			player.MessageHandler(message.Self);
			room.InformPlayersInRoom(message, new List<string>() { player.UserID });
        }

        private static void CloseADoor(IDoor door) {
            door.Open = false;
            door.UpdateDoorStatus();
        }

        public static bool CloseDoorOverride(string roomID, string doorDirection) {
            IDoor door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                //we only care thats it's open and not destroyed, we bypass any other check
                if (door.Open && !door.Destroyed) {
                    CloseADoor(door);
                    return true;
                }
            }
            return false;
        }

        private static void CloseDoor(IUser player, IDoor door) {
			IMessage message = new Message();
			IRoom room = Room.GetRoom(player.Player.Location);
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (!player.Player.InCombat) {
                if (!room.IsDark) {
                    if (door.Openable) {
                        if (door.Open && !door.Destroyed) {
                            door.Open = false;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Self = String.Format("You close {0} {1}.", GetArticle(door.Description[0]), door.Description);
                            message.Room = String.Format("{0} closes {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description);
                        }
                        else if (door.Destroyed) {
                            message.Self = "You can't close it because it is in pieces!";
                        }
                        else {
                            message.Self = "It's already closed.";
                        }
                    }
                    else {
                        message.Self = "It can't be closed.";
                    }
                }
                else {
                    message.Self = "You can't see anything! Let alone what you are trying to close.";
                }
            }
            else {
                message.Self = "You are in the middle of combat, there are more pressing matters at hand than closing something.";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<string>() { player.UserID });
		}
		#endregion

		#region Lock and Unlock
		private static void Lock(IUser player, List<string> commands) {
			IDoor door = FindDoor(player.Player.Location, commands);
            if (door != null) {
                LockDoor(player, door);
            }
			//ok not a door so then we'll check containers in the room
		}

        public static bool LockDoorOverride(string roomID, string doorDirection) {
            IDoor door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (!door.Open && !door.Destroyed) {
                door.Locked = true;
                return true;
            }
            return false;
        }

        public static bool UnlockDoorOverride(string roomID, string doorDirection) {
            IDoor door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (!door.Open && !door.Destroyed) {
                door.Locked = false;
                return true;
            }
            return false;
        }

		private static void LockDoor(IUser player, IDoor door) {
			IRoom room = Room.GetRoom(player.Player.Location);
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (!player.Player.InCombat) {
                bool hasKey = false;
                if (!room.IsDark) {
                    if (door.Lockable) {
                        if (door.RequiresKey) {
                            //let's see if the player has the key in his inventory or a skeleton key (opens any door)
                            List<IItem> inventory = player.Player.Inventory.GetInventoryAsItemList();
                            List<IItem> keyList = inventory.Where(i => i.ItemType.ContainsKey(ItemsType.KEY)).ToList();
                            IKey key = null;
                            foreach (IItem keys in keyList) {
                                key = keys as IKey;
                                if (key.DoorID == door.Id || key.SkeletonKey) {
                                    hasKey = true;
                                    break;
                                }
                            }
                        }
                        if (!door.Open && !door.Destroyed  && ((door.RequiresKey && hasKey) || !door.RequiresKey)) {
                            door.Locked = true;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Self = String.Format("You lock {0} {1}.", GetArticle(door.Description[0]), door.Description);
                            message.Room = String.Format("{0} locks {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description);
                        }
                        else if (door.Destroyed) {
                            message.Self = "Why would you want to lock something that is broken?";
                        }
                        else if (!hasKey) {
                            message.Self = "You don't have the key to lock this door.";
                        }
                        else {
                            message.Self = "It can't be locked, the door is open.";
                        }
                    }
                    else {
                        message.Self = "It can't be locked.";
                    }
                }
                else {
                    message.Self = "You can't see anything! Let alone what you are trying to lock.";
                }
            }
            else {
                message.Self = "You are in the middle of combat there are more pressing matters at hand than locking something.";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<string>(new string[] { player.UserID }));
		}

		private static void Unlock(IUser player, List<string> commands) {
			List<string> message = new List<string>();

			IDoor door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				UnlockDoor(player, door);
			}
			//ok not a door so then we'll check containers in the room
		}

		private static void UnlockDoor(IUser player, IDoor door) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            if (!player.Player.InCombat) {
                IRoom room = Room.GetRoom(player.Player.Location);
                bool hasKey = false;
                if (!room.IsDark) {
                    if (door.Lockable) {
                        if (door.RequiresKey) {
                            //let's see if the player has the key in his inventory or a skeleton key (opens any door)
                            List<IItem> inventory = player.Player.Inventory.GetInventoryAsItemList();
                            List<IItem> keyList = inventory.Where(i => i.ItemType.ContainsKey(ItemsType.KEY)).ToList();
                            IKey key = null;
                            foreach (IItem keys in keyList) {
                                key = keys as IKey;
                                if (key.DoorID == door.Id || key.SkeletonKey) {
                                    hasKey = true;
                                    break;
                                }
                            }
                        }
                        if (!door.Open && !door.Destroyed && ((door.RequiresKey && hasKey) || !door.RequiresKey)) {
                            door.Locked = false;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Self = String.Format("You unlock {0} {1}.", GetArticle(door.Description[0]), door.Description);
                            message.Room = String.Format("{0} unlocks {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description);
                        }
                        else if (door.Destroyed) {
                            message.Self ="Why would you want to unlock something that is in pieces?";
                        }
                        else if (!hasKey) {
                            message.Self ="You don't have the key to unlock this door.";
                        }
                        else {
                            message.Self ="It can't be unlocked, the door is open.";
                        }
                    }
                    else {
                        message.Self = "It can't be unlocked.";
                    }
                }
                else {
                    message.Self = "You can't see anything! Let alone what you are trying to unlock.";
                }
				if (player.Player.IsNPC) {
					player.MessageHandler(message);
				}
				else {
					player.MessageHandler(message.Self);
				}
                
                room.InformPlayersInRoom(message, new List<string>() { player.UserID });
                
            }
            else {
                player.MessageHandler("You are in the middle of combat there are more pressing matters at hand than unlocking something.");
            }
		}
		#endregion 

		#region Actions    
        private static void PerformSkill(IUser user, List<string> commands) {
            Skill skill = new Skill();
            skill.FillSkill(user, commands);
            skill.ExecuteScript();
        }

       private static void Prone(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (player.Player.StanceState != CharacterStanceState.Prone && (player.Player.ActionState == CharacterActionState.None
				|| player.Player.ActionState == CharacterActionState.Fighting)) {
				player.Player.SetStanceState(CharacterStanceState.Prone);
				message.Self = "You lay down.";
				message.Room = String.Format("{0} lays down on the ground.", player.Player.FirstName);
                
			}
			else if (player.Player.ActionState != CharacterActionState.None) {
				message.Self = String.Format("You can't lay prone.  You are {0}!", player.Player.ActionState.ToString().ToLower());
			}
			else {
				message.Self = String.Format("You can't lay prone.  You are {0}!", player.Player.StanceState.ToString().ToLower());
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>() { player.UserID });
		}

		//this should replace any current methods that change a players stance like Stand() and Prone() we just need to add the messages to the DB
		//and we should then be able to get rid of them.
		//***** This has not been tested!! ******
		private static void ChangeStance(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			var stances = MongoUtils.MongoData.GetCollection<BsonDocument>("Charcaters", "Stances");
			IMongoQuery query = Query.EQ("_id", commands[0].Replace(" ", "_"));
            var stanceMessages = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(stances, s => s["_id"] == commands[0].Replace(" ", "_")).Result;

			CharacterStanceState newStance = (CharacterStanceState)Enum.Parse(typeof(CharacterStanceState), commands[1].Replace(" ","_"));
			if (player.Player.StanceState != newStance && (player.Player.ActionState == CharacterActionState.None
				|| player.Player.ActionState == CharacterActionState.Fighting)) {
				player.Player.SetStanceState(newStance);
				message.Self = String.Format("You {0}", stanceMessages["Self"].AsString);
				message.Room = String.Format("{0} {1}", player.Player.FirstName, stanceMessages["Room"].AsString);

			}
			else if (player.Player.ActionState != CharacterActionState.None) {
				message.Self = String.Format("You can't {0}.  You are {1}!", stanceMessages["Deny"].AsString, player.Player.ActionState.ToString().ToLower());
			}
			else {
				message.Self = String.Format("You can't {0}.  You are {1}!", stanceMessages["Deny"].AsString, player.Player.StanceState.ToString().ToLower());
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>() { player.UserID });
		}

		private static void Stand(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (player.Player.StanceState != CharacterStanceState.Standing && (player.Player.ActionState == CharacterActionState.None
				|| player.Player.ActionState == CharacterActionState.Fighting)) {
				player.Player.SetStanceState(CharacterStanceState.Standing);
				message.Self = "You stand up.";
				message.Room = String.Format("{0} stands up.", player.Player.FirstName);
                
			}
			else if (player.Player.ActionState != CharacterActionState.None) {
				message.Self = String.Format("You can't stand up.  You are {0}!", player.Player.ActionState.ToString().ToLower());
			}
			else {
				message.Self = String.Format("You can't stand up.  You are {0}!", player.Player.StanceState.ToString().ToLower());
			}


			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}

			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>() { player.UserID });
		}

		private static void Sit(IUser player, List<string> commands) {
			IMessage message = new Message();
			message.InstigatorID = player.UserID;
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (player.Player.StanceState != CharacterStanceState.Sitting && (player.Player.ActionState == CharacterActionState.None
				|| player.Player.ActionState == CharacterActionState.Fighting)) {
				player.Player.SetStanceState(CharacterStanceState.Sitting);
				message.Self = "You sit down.";
				message.Room = String.Format("{0} sits down.", player.Player.FirstName);

			}
			else if (player.Player.ActionState != CharacterActionState.None) {
				message.Self = String.Format("You can't sit down.  You are {0}!", player.Player.ActionState.ToString().ToLower());
			}
			else {
				message.Self = String.Format("You can't sit down.  You are {0}!", player.Player.StanceState.ToString().ToLower());
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>(){ player.UserID });
		}

        
		#endregion Actions

		#region Helper methods
		private static IDoor FindDoor(string location, List<string> commands) {
			//this needs to be somewhat smart if the player types "break door" we should assume he wants to break the only door
			//in the room, otherwise if he passes in "break iron door" we should be able to figure out he wants to break the door
			//made of iron and if he passes "break west iron door"  he wants to break the iron door in the west exit.
			//Same if he just types "break west door" he wants to break the door in the west exit.
			string[] dirs = new string[] { "north", "south", "east", "west", "up", "down" };
			string objectName = "";
			string possibleDirection = "";
			for (int i = 1; i < commands.Count; i++) {
				if ((i == 1 || i == commands.Count - 1) && dirs.Contains(commands[i])) { //the direction is 99.9% probably going to be at the start or end
					possibleDirection = commands[i];
					continue;
				}
				if (i == 1) continue;  //I don't care about the index which is the action to get the object name
				objectName += commands[i];
				if (i + 1 < commands.Count) objectName += " ";
			}

            IRoom room = Room.GetRoom(location);
			RoomExits directionToGo = RoomExits.None;

			//let's see if the player provided a direction first
			if (possibleDirection.ToUpper().Contains(RoomExits.North.ToString().ToUpper()))
				directionToGo = RoomExits.North;
			else if (possibleDirection.ToUpper().Contains(RoomExits.South.ToString().ToUpper()))
				directionToGo = RoomExits.South;
			else if (possibleDirection.ToUpper().Contains(RoomExits.East.ToString().ToUpper()))
				directionToGo = RoomExits.East;
			else if (possibleDirection.ToUpper().Contains(RoomExits.West.ToString().ToUpper()))
				directionToGo = RoomExits.West;
			else if (possibleDirection.ToUpper().Contains(RoomExits.Up.ToString().ToUpper()))
				directionToGo = RoomExits.Up;
			else if (possibleDirection.ToUpper().Contains(RoomExits.Down.ToString().ToUpper()))
				directionToGo = RoomExits.Down;
			else if (possibleDirection.ToUpper().Contains("ABOVE"))
                directionToGo = RoomExits.Up;
			else if (possibleDirection.ToUpper().Contains("BELOW"))
                directionToGo = RoomExits.Down;

			IDoor door = null;
			//get the exit based on the direction, if we find an exit then we can start looking for a door
            room.GetRoomExits();
			var exits = room.RoomExits;
			IExit exit = exits.Where(e => e.availableExits.ContainsKey(directionToGo)).SingleOrDefault();

			//let's see if we find one based on a direction
			if (exit != null && !string.IsNullOrEmpty(possibleDirection)) { 
				door = exit.doors[directionToGo];
			}
			//didn't find anything based on direction, search based on name
			else if (exit == null) { 
				foreach (IExit ex in exits) {
					if (ex.doors.Where(d => d.Value.Name.ToLower().Contains(objectName.ToLower())).Any()) {
						door = ex.doors.Where(d => d.Value.Name.ToLower().Contains(objectName.ToLower())).SingleOrDefault().Value; 
						break; //we found it!
					}
				}
				//we didn't find it, is there even a door in the room at all? let's find the first one we see 
				if (door == null && commands.Count < 2) { //if the player just typed "break" we'll return a door otherwise it could be another object
					foreach (IExit ex in exits) {          //they actually wanted to break.
						if (ex.doors.Count > 0) {
							door = ex.doors.ElementAt(0).Value;
							break; //we found a door
						}
					}
				}
			}
			//at this point we either found one or didn't so we're returning the door we found or null
			return door;
		}


		private static string GetArticle(char firstLetter) {
			string[] vowels = new string[] { "a", "e", "i", "o", "u" };
			string article = vowels.Contains(firstLetter.ToString().ToLower()) ? "an" : "a";
			return article;
		}
		#endregion
	}
}
