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
       
        public static void Drop(IUser player, List<string> commands) {
            //1.get the item name from the command, may have to join all the words after dropping the command
            StringBuilder itemName = new StringBuilder();
            IRoom room = Room.GetRoom(player.Player.Location);

            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);
            
            foreach (string word in commands) {
                itemName.Append(word + " ");
            }

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            //2.get the item from the DB
            List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
            IItem item = items[itemPosition - 1];

			//3.have player drop item
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            if (item != null) {
                player.Player.Inventory.RemoveInventoryItem(item, player.Player.Equipment);
                item.Location = player.Player.Location;
				item.Owner = player.UserID;
                item.Save();

                //4.Inform room and player of action
                message.Room = string.Format("{0} drops {1}", player.Player.FirstName, item.Name);
                room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
                message.Self = string.Format("You drop {0}", item.Name);
            }
            else {
                message.Self = "You are not carrying anything of the sorts.";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
		}

		private static void Give(IUser player, List<string> commands) {
			//get the item name from the command, may have to join all the words after dropping the command
			StringBuilder itemName = new StringBuilder();
			IRoom room = Room.GetRoom(player.Player.Location);

			string[] full = commands[0].Replace("give","").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			
			foreach (string word in full) {
				if (word.ToLower() != "to") {
					itemName.Append(word + " ");
				}
				else {
					break; //we got to the end of the item name
				}
			}

			int itemPosition = 1;
			string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
			if (position.Count() > 1) {
				int.TryParse(position[position.Count() - 1], out itemPosition);
				itemName = itemName.Remove(itemName.Length - 2, 2);
			}

			//get the item from the DB
			List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
			if (items.Count == 0) {
				player.MessageHandler("You can't seem to find an item by that name.");
				return;
			}
			IItem item = items[itemPosition - 1];


			string toPlayerName = commands[0].ToLower().Replace("give", "").Replace(itemName.ToString(), "").Replace("to", "").Trim();

			bool HasDotOperator = false;
			int playerPosition = 0;
			position = toPlayerName.Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
			if (position.Count() > 1) {
				int.TryParse(position[position.Count() - 1], out playerPosition);
				HasDotOperator = true;
			}

			IUser toPlayer = null;
			List<IUser> toPlayerList = new List<IUser>();
			//we need some special logic here, first we'll try by first name only and see if we get a hit.  If there's more than one person named the same
			//then we'll see if the last name was included in the commands. And try again.  If not we'll check for the dot operator and all if else fails tell them
			//to be a bit more specific about who they are trying to directly speak to.
			string[] nameBreakDown = toPlayerName.ToLower().Split(' ');
			foreach (var id in room.GetObjectsInRoom(RoomObjects.Players, 100)) {
				toPlayerList.Add(Server.GetAUser(id));
			}

			if (toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[0]).Count() > 1) { //let's narrow it down by including a last name (if provided)
				toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == (nameBreakDown[0] ?? "")).Where(p => String.Compare(p.Player.LastName.ToLower(), nameBreakDown[1] ?? "", true) == 0).SingleOrDefault();

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
				toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == (nameBreakDown[0] ?? "")).SingleOrDefault();

				if (toPlayer != null && toPlayer.UserID == player.UserID) {
					toPlayer = null; //It's the player saying something!
				}
			}

			if (toPlayer == null) { //we are looking for an npc at this point
				toPlayerList.Clear();
				foreach (var id in room.GetObjectsInRoom(RoomObjects.Npcs, 100)) {
					toPlayerList.Add(Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { id }));
				}
				if (toPlayerList.Where(p => p.Player.FirstName.ToLower() == nameBreakDown[0]).Count() > 1) { //let's narrow it down by including a last name (if provided)
					toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == (nameBreakDown[0] ?? "")).Where(p => String.Compare(p.Player.LastName, nameBreakDown[1] ?? "", true) == 0).SingleOrDefault();

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
					toPlayer = toPlayerList.Where(p => p.Player.FirstName.ToLower() == (nameBreakDown[0] ?? "")).SingleOrDefault();

					if (commands.Count == 2 || toPlayer != null && toPlayer.UserID == player.UserID) {
						toPlayer = null;
						player.MessageHandler("Really? Giving to yourself?.");
					}
					else if (toPlayer == null) {
						player.MessageHandler("You can't give things to someone who is not here.");
					}
				}
			}

			//have player give item
			IMessage message = new Message();

			if (item != null && toPlayer != null) {
				message.InstigatorID = player.Player.Id.ToString();
				message.InstigatorType = player.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;
				message.TargetID = toPlayer.Player.Id.ToString();
				message.TargetType = toPlayer.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;

				player.Player.Inventory.RemoveInventoryItem(item, player.Player.Equipment);
				player.Player.Save();

                item.Location = "";
				item.Owner = toPlayer.UserID;
				item.Save();

				toPlayer.Player.Inventory.AddItemToInventory(item);
				toPlayer.Player.Save();
				
				//Inform room and player of action
				message.Room = string.Format("{0} gives {1} to {2}", player.Player.FirstName, item.Name, toPlayer.Player.FirstName);
				message.Self = string.Format("You give {0} to {1}", item.Name, toPlayer.Player.FirstName);
				message.Target = string.Format("{0} gives you {1}", player.Player.FirstName, item.Name);

				if (toPlayer.Player.IsNPC) {
					toPlayer.MessageHandler(message);
				}
				else {
					toPlayer.MessageHandler(message.Target);
				}
			}

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}

			room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
		}

        private static void Loot(IUser player, List<string> commands) {
            IActor npc = null;
            string[] position = commands[0].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                //ok so the player specified a specific NPC in the room list to loot and not just the first to match
                int pos;
                int.TryParse(position[position.Count() - 1], out pos);
                if (pos != 0) {
                    npc = Character.NPCUtils.GetAnNPCByID(GetObjectInPosition(pos, commands[2], player.Player.Location));
                }
            }
			if (npc == null) {
				var npcList = Character.NPCUtils.GetAnNPCByName(commands[2], player.Player.Location);
				if (npcList.Count > 1) {
					npc = npcList.SingleOrDefault(n => n.Location == player.Player.Location);
				}
				else {
					npc = npcList[0];
				}
			}
            if (npc == null) {
                npc = Character.NPCUtils.GetAnNPCByID(player.Player.CurrentTarget);
            }

            if (npc != null && npc.IsDead()) {
                npc.Loot(player, commands);
            }
            else if (npc != null && !npc.IsDead()) {
                player.MessageHandler("You can't loot what is not dead! Maybe you should try killing it first.");
            }

            //wasn't an npc we specified so it's probably a player
            if (npc == null) {
                IUser lootee = FindTargetByName(commands[commands.Count - 1], player.Player.Location);
                if (lootee != null && lootee.Player.IsDead()) {
                    lootee.Player.Loot(player, commands);
                }
                else if (lootee != null && !lootee.Player.IsDead()) {
                    player.MessageHandler("You can't loot what is not dead! Maybe you should try pickpocketing or killing it first.");
                }
                else {
                    player.MessageHandler("You can't loot what doesn't exist...unless you see dead people, but you don't.");
                }
            }

            return;
        }


        public static void Unequip(IUser player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            //they said 'all' so we are going to remove everything
            if (commands.Count > 2 && string.Equals(commands[2].ToLower(), "all", StringComparison.InvariantCultureIgnoreCase)) {
                foreach (KeyValuePair<Wearable, IItem> item in player.Player.Equipment.GetEquipment()) {
                    if (player.Player.Equipment.UnequipItem(item.Value, player.Player)) {
                    }
                }

                message.Room = string.Format("{0} removes all his equipment.", player.Player.FirstName);
            }
            else {
                string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                if (position.Count() > 1) {
                    int.TryParse(position[position.Count() - 1], out itemPosition);
                    itemName = itemName.Remove(itemName.Length - 2, 2);
                }

                string full = commands[0];
                commands.RemoveAt(0);
                commands.RemoveAt(0);

                foreach (string word in commands) {
                    itemName.Append(word + " ");
                }
				
                List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
                IItem item = items[itemPosition - 1];

                if (item != null) {
                    player.Player.Equipment.UnequipItem(item, player.Player);
                    message.Room = string.Format("{0} unequips {1}", player.Player.FirstName, item.Name);
                    message.Self = string.Format("You unequip {0}", item.Name);
                }
                else {
                    if (commands.Count == 2) {
                        message.Self = "Unequip what?";
                    }
                    else {
						message.Self = "You don't seem to be equipping that at the moment.";
                    }
                }
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID });
        }

        public static void Equip(IUser player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            //we need to make a list of items to wear from the players inventory and sort them based on stats
            if (commands.Count > 2 && string.Equals(commands[2].ToLower(), "all", StringComparison.InvariantCultureIgnoreCase)) {
                foreach (IItem item in player.Player.Inventory.GetAllItemsToWear()) {
                    if (player.Player.Equipment.EquipItem(item, player.Player.Inventory)) {
                        message.Self += string.Format("You equip {0}.\n", item.Name);
                        message.Room += string.Format("{0} equips {1}.\n", player.Player.FirstName, item.Name);
                    }
                }
            }
            else {
                string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                if (position.Count() > 1) {
                    int.TryParse(position[position.Count() - 1], out itemPosition);
                    itemName = itemName.Remove(itemName.Length - 2, 2);
                }

                string full = commands[0];
                commands.RemoveRange(0, 2);

                foreach (string word in commands) {
                    itemName.Append(word + " ");
                }

                List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
                
                //players need to specify an indexer or we will just give them the first one we found that matched
                IItem item = items[itemPosition - 1];

                IWeapon weapon = item as IWeapon;

                if (item != null && item.IsWearable) {
                    player.Player.Equipment.EquipItem(item, player.Player.Inventory);
                    if (item.ItemType.ContainsKey(ItemsType.CONTAINER)) {
                        IContainer container = item as IContainer;
                        container.Wear();
                    }
                    if (item.ItemType.ContainsKey(ItemsType.CLOTHING)) {
                        IClothing clothing = item as IClothing;
                        clothing.Wear();
                    }

                    message.Room = string.Format("{0} equips {1}.", player.Player.FirstName, item.Name);
                    message.Self = string.Format("You equip {0}.", item.Name);
                }
                else if (weapon.IsWieldable) {
                    message.Self = "This item can only be wielded not worn.";
                }
                else if (!item.IsWearable || !weapon.IsWieldable) {
                    message.Self = "That doesn't seem like something you can wear.";
                }
                else {
                    message.Self = "You don't seem to have that in your inventory to be able to wear.";
                }
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID });
        }

        public static void Wield(IUser player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            string full = commands[0];
            commands.RemoveRange(0, 2);
                        
            foreach (string word in commands) {
                itemName.Append(word + " ");
            }

			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
            IItem item = items[itemPosition - 1];
            
            IWeapon weapon = (IWeapon)item;
            
            if (weapon != null && weapon.IsWieldable && player.Player.Equipment.GetWieldedWeapons().Count < 2) {
                if (string.IsNullOrEmpty(player.Player.MainHand)) { //no mainhand assigned yet
                    player.Player.MainHand = Wearable.WIELD_RIGHT.ToString(); //we will default to the right hand
                }
                
                player.Player.Equipment.Wield(item, player.Player.Inventory);
                item.Save();
                //TODO: check weapon for any wield perks/curses

                message.Room = string.Format("{0} wields {1}", player.Player.FirstName, item.Name);
                message.Self = string.Format("You wield {0}", item.Name);
            }
            else if (player.Player.Equipment.GetWieldedWeapons().Count == 2) {
                message.Self =  "You are already wielding two weapons...and you don't seem to have a third hand.";
            }
            else if (item.IsWearable) {
				message.Self = "This item can only be wielded not worn.";
            }
            else if (!item.IsWearable) {
				message.Self = "That not something you can wear or would want to wear.";
            }
            else {
				message.Self = "You don't seem to have that in your inventory to be able to wear.";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}

			Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
		}

        public static void Eat(IUser player, List<string> commands) {		
            IItem item = GetItem(commands, player.Player.Location);
            if (item == null) {
                player.MessageHandler("You don't seem to be carrying that to eat it.");
                return;
            }

            if (item.ItemType.ContainsKey(ItemsType.EDIBLE)) {
               Consume(player, commands, "eat", item);
            }
            else {
                player.MessageHandler("You can't eat that!");
            }
            
        }

        public static void Drink(IUser player, List<string> commands) {
            IItem item = GetItem(commands, player.Player.Location);
            if (item == null) {
                player.MessageHandler("You don't seem to be carrying that to drink it.");
                return;
            }
            if (item.ItemType.ContainsKey(ItemsType.DRINKABLE)) {
                Consume(player, commands, "drink", item);
            }
            else {
                player.MessageHandler("You can't drink that!");
            }
            
        }

        private static IItem GetItem(List<string> commands, string location) {
            StringBuilder itemName = new StringBuilder();
            
            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);
            

            foreach (string word in commands) {
                itemName.Append(word + " ");
            }

            List<IItem> items = Items.Items.GetByName(itemName.ToString().Trim(), ObjectId.Parse(location));
            if (items != null && items.Count > 0) {
                return items[itemPosition - 1];
            }
            else {
                return null;
            }
        }

        private static void Consume(IUser player, List<string> commands, string action, IItem item){
            string upDown = "gain";
			IMessage message = new Message();
                                   
            Dictionary<string, double> affectAttributes = null;
            
            IEdible food = item as IEdible;
            affectAttributes = food.Consume();
            
            foreach (KeyValuePair<string, double> attribute in affectAttributes) {
                player.Player.ApplyEffectOnAttribute(attribute.Key.CamelCaseWord(), attribute.Value);
                if (attribute.Value < 0) {
                    upDown = "lost";
                }

                message.Self += string.Format("You {0} {1} and {2} {3:F1} points of {4}.\n", action, item.Name, upDown, Math.Abs(attribute.Value), attribute.Key);
                message.Room = string.Format("{0} {1}s {2}", player.Player.FirstName.CamelCaseWord(), action, item.Name);
            }

            //now remove it from the players inventory
            player.Player.Inventory.RemoveInventoryItem(item, player.Player.Equipment);

            //item has been consumed so get rid of it from the DB
            MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items").DeleteOneAsync<Items.Items>(i => i.Id == item.Id);

            Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId> { player.UserID });
			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
		}

        //container commands
        public async static void Put(IUser player, List<string> commands) {
            //this command is used only for putting an Item in the root inventory of a player into a bag.  
            //If an item needs to go from a bag to the root inventory level player should use the GET command instead.

            int itemPosition = 1;
            int containerPosition = 1;
            string itemName = "";
            string containerName = "";

           //this allows players to use either IN or INTO
            int commandIndex = 0;
            foreach (string word in commands) {
                if (string.Equals(word, "in", StringComparison.InvariantCultureIgnoreCase)) {
                    commands[commandIndex] = "into";
                    break;
                }
                commandIndex++;
            }

            var location = "";
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }

            List<string> commandAltered = ParseItemPositions(commands, "into", out itemPosition, out itemName);
            ParseContainerPosition(commandAltered, "", out containerPosition, out containerName);

            IItem retrievedItem = null;
            IItem containerItem = null;

            //using a recursive method we will dig down into each sub container looking for the appropriate container
            if (location.Equals(ObjectId.Empty)) {
                TraverseItems(player, containerName.ToString().Trim(), itemName.ToString().Trim(), containerPosition, itemPosition, out retrievedItem, out containerItem);

                //player is an idiot and probably wanted to put it in his inventory but didn't specify it so let's check there as well
                if (containerItem == null) {
                    foreach (IItem tempContainer in player.Player.Inventory.GetInventoryAsItemList()) {
                        containerItem = KeepOpening(containerName.CamelCaseString(), tempContainer, containerPosition);
                        if (string.Equals(containerItem.Name, containerName.CamelCaseString(), StringComparison.InvariantCultureIgnoreCase)) {
                            break;
                        }
                    }
                }
            }
            else{ //player specified it is in his inventory 
                foreach (var id in player.Player.Inventory.GetInventoryList()) {
                   IItem tempContainer = await Items.Items.GetByID(ObjectId.Parse(id));
                    containerItem = KeepOpening(containerName.CamelCaseString(), tempContainer, containerPosition);
                    if (string.Equals(containerItem.Name, containerName.CamelCaseString(), StringComparison.InvariantCultureIgnoreCase)) {
                        break;
                    }
                }
            }

            bool stored = false;

            retrievedItem = player.Player.Inventory.GetInventoryAsItemList().Where(i => i.Name == itemName).SingleOrDefault();

            if (containerItem != null  && retrievedItem != null) {
                retrievedItem.Location = containerItem.Location;
                retrievedItem.Owner = containerItem.Id;
                retrievedItem.Save();
                IContainer container = containerItem as IContainer;
                stored = container.StoreItem(retrievedItem.Id);
            }
            

            string msg = null;

            if (!stored) {
                msg = "Could not put " + itemName.ToString().Trim().ToLower() + " inside the " + containerName.ToString().Trim().ToLower() + ".";
            }
            else {
                msg = "You place " + itemName.ToString().Trim().ToLower() + " inside the " + containerName.ToString().Trim().ToLower() + ".";
            }

            player.MessageHandler(msg);
        }

        //TODO: had a bug where I removed item form a container, shut down the game and then both container and player still had the same item (the player even had it duped)
        //needless to say this is bad and fail.
        public static void Get(IUser player, List<string> commands) {
            int itemPosition = 1;
            int containerPosition = 1;
            string itemName = "";
            string containerName = "";

            List<string> commandAltered = ParseItemPositions(commands, "from", out itemPosition, out itemName);
			if (commands.Count >= 3) {
				ParseContainerPosition(commandAltered, commands[3], out containerPosition, out containerName);
			}
          
            var location = player.Player.Location;
           
            IItem retrievedItem = null;
            IItem containerItem = null;
            
            //using a recursive method we will dig down into each sub container and look for the appropriate item/container
            TraverseItems(player, containerName.ToString().Trim(), itemName.ToString().Trim(), containerPosition, itemPosition, out retrievedItem, out containerItem);

			IMessage message = new Message();

            if (retrievedItem != null) {
                IContainer container = containerItem as IContainer;
                if (containerItem != null) {
                    retrievedItem = container.RetrieveItem(retrievedItem.Id);
                    message.Self = "You take " + retrievedItem.Name.ToLower() + " out of " + containerItem.Name.ToLower() + ".";

                    message.Room = string.Format("{0} takes {1} out of {2}", player.Player.FirstName, retrievedItem.Name.ToLower(), containerItem.Name.ToLower());
                    
                }
                else {
					message.Self = "You get " + retrievedItem.Name.ToLower();
					message.Room = string.Format("{0} grabs {1}.", player.Player.FirstName, retrievedItem.Name.ToLower());
                }

                retrievedItem.Location = "";
                retrievedItem.Owner = player.UserID;
                retrievedItem.Save();
                player.Player.Inventory.AddItemToInventory(retrievedItem);
            }
            else {
				message.Self = "You can't seem to find " + itemName.ToString().Trim().ToLower() + " to grab it.";
            }

            Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>(){ player.UserID });
			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}

		}

        private static void ParseContainerPosition(List<string> commands, string separator, out int containerPosition, out string containerName) {
            containerName = "";
            containerPosition = 1;
            StringBuilder containerNameTemp = new StringBuilder();
            bool start = false;

            foreach (string word in commands) {
                if (string.Equals(word, separator, StringComparison.InvariantCultureIgnoreCase)) {
                    start = true;
                    continue;
                }
                if (start) {
                    containerNameTemp.Append(word + " ");
                }
            }

            string[] positionItem = containerNameTemp.ToString().Trim().Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (positionItem.Count() > 1) {
                int.TryParse(positionItem[positionItem.Count() - 1], out containerPosition);
                containerNameTemp.Remove(containerNameTemp.Length - 3, 2);
            }

            containerName = containerNameTemp.ToString().Trim().CamelCaseString();
        }

        private static List<string> ParseItemPositions(List<string> commands, string separator, out int itemPosition, out string itemNameReturned){
            itemPosition = 1;           
            
            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);

            StringBuilder itemName = GetItemName(commands, separator);
            
            string[] position = itemName.ToString().Trim().Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName.Remove(itemName.Length - 3, 2);
            }

            itemNameReturned = itemName.ToString().Trim().CamelCaseString();
            return commands;
            
        }

        private static void TraverseItems(IUser player, string containerName, string itemName, int containerPosition, int itemPosition, out IItem retrievedItem, out IItem retrievedContainer) {
            int containerIndex = 1;
            int itemIndex = 1;
            retrievedItem = null;
            retrievedContainer = null;
            IRoom room = Room.GetRoom(player.Player.Location);
            if (!string.IsNullOrEmpty(containerName.CamelCaseString())) {
                foreach (var itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem inventoryItem = Items.Items.GetByID(itemID).Result;

                    inventoryItem = KeepOpening(containerName.CamelCaseString(), inventoryItem, containerPosition, containerIndex);

                    if (inventoryItem.Name.Contains(containerName.CamelCaseString())) {
                        retrievedContainer = inventoryItem;
                        break;
                    }
                }
            }           

            //if we retrieved a specific indexed container search within it for the item
            if (retrievedContainer != null) {
                IContainer container = null;
                container = retrievedContainer as IContainer;
                foreach (var itemID in container.GetContents()) {
                    IItem inventoryItem = Items.Items.GetByID(itemID).Result;

                    inventoryItem = KeepOpening(itemName.CamelCaseString(), inventoryItem, itemPosition, itemIndex);

                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
            else if (string.IsNullOrEmpty(containerName)) {//we are grabbing a container or an item without a specific index
                foreach (var itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem inventoryItem = Items.Items.GetByID(itemID).Result;

                    inventoryItem = KeepOpening(itemName.CamelCaseString(), inventoryItem, itemPosition, itemIndex);

                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
            else {
                foreach (var itemID in room.GetObjectsInRoom(RoomObjects.Items)) {
                    IItem inventoryItem = Items.Items.GetByID(itemID).Result;
                                     
                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
        }      

        private static void Activate(IUser player, List<string> commands) {
            //used for lighting up a lightSource that can be lit.
            IIluminate lightItem = null;
            string command = null;
            switch (commands[1]) {
                case "TURNON": command = "turn on";
                    break;
                case "SWITHCON": command = "switch on";
                    break;
                default: command = commands[1];
                    break;
            }
            commands.RemoveRange(0, 2);
            IMessage message = new Message();
			IRoom room = Room.GetRoom(player.Player.Location);

            
            lightItem = FindLightInEquipment(commands, player, room);
            

            if (lightItem != null) {

                if (lightItem.isLit == false) {
                    message = lightItem.Ignite();
                    message.Room = ParseMessage(message.Room, player, null);
                }
                else {
                    message.Self = "It's already on!";
                }
            }
            else {
                message.Self ="You don't see anything to " + command + ".";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
            
        }

        private static IIluminate FindLightInEquipment(List<string> commands, IUser player, IRoom room) {
            IItem lightItem = null;
            if (commands.Count > 0) {
                string itemName = GetItemName(commands, "").ToString();
                //let's see if player has a lightsource equipped
                foreach (IItem item in player.Player.Equipment.GetEquipment().Values) {
                    if (item.WornOn == Wearable.WIELD_LEFT || item.WornOn == Wearable.WIELD_RIGHT) {
                        IIluminate temp = item as IIluminate;
                        if (temp != null && temp.isLightable) {
                            lightItem = item;
                            break;
                        }
                    }
                }
            }
            else { //let's be smart and figure out what lightSource he wants activated, first come first serve otherwise
                foreach (IItem item in player.Player.Equipment.GetEquipment().Values) {
                    IIluminate lightsource = item as IIluminate;
                    if (lightsource != null && lightsource.isLightable) {
                        lightItem = item;
                        break;
                    }
                }
                if (lightItem == null) { //not in players equipment let's check the room
                    foreach (var itemId in room.GetObjectsInRoom(RoomObjects.Items)) {
                        lightItem = Items.Items.GetByID(itemId).Result;
                        IIluminate lightsource = lightItem as IIluminate;
                        if (lightsource != null && lightsource.isLightable) {
                            break;
                        }
                        //if it's a container and it's open see if it has a lightsource inside
                        if (lightItem.ItemType.ContainsKey(ItemsType.CONTAINER)) {
                            IContainer containerItem = lightItem as IContainer;
                            if (containerItem.Opened) {
                                foreach (var id in containerItem.GetContents()) {
                                    lightItem = Items.Items.GetByID(itemId).Result;
                                    lightsource = lightItem as IIluminate;
                                    if (lightsource != null && lightsource.isLightable) {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (lightItem as IIluminate);
        }


        private static void DeActivate(IUser player, List<string> commands) {
            //used for turning off a lightSource that can be lit.
            IIluminate lightItem = null;

            //just making the command be display friendly for the messages
            string command = null;
            switch (commands[1]) {
                case "TURNOFF": command = "turn off";
                    break;
                case "SWITCHOFF": command = "switch off";
                    break;
                default: command = commands[1];
                    break;
            }

            commands.RemoveRange(0, 2);

            IMessage message = new Message();
            IRoom room = Room.GetRoom(player.Player.Location);

            lightItem = FindLightInEquipment(commands, player, room);

            if (lightItem != null) {
                if (lightItem.isLit) {
                    message = lightItem.Extinguish();
                    message.Room = string.Format(message.Room, player.Player.FirstName);
                    
                }
                else {
                    message.Self = "It's already off!";
                }
            }
            else {
                message.Self = "You don't see anything to " + command + ".";
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}
			room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
        }


        public static StringBuilder GetItemName(List<string> commands, string separator) {
            StringBuilder itemName = new StringBuilder();
            foreach (string word in commands) {
                if (!string.Equals(word, separator, StringComparison.InvariantCultureIgnoreCase)) {
                    itemName.Append(word + " ");
                }
                else {
                    //we got the item name
                    break;
                }
            }

            return itemName;
        }
    }
}
