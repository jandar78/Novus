using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using Extensions;
using Rooms;
using Interfaces;
using Sockets;

namespace Commands {
    public partial class CommandParser {

        //called from the LOOK command
        private static string DisplayPlayersInRoom(IRoom room, string ignoreId) {
            StringBuilder sb = new StringBuilder();

            if (!room.IsDark) {
                foreach (string id in room.GetObjectsInRoom(RoomObjects.Players)) {
                    if (id != ignoreId) {
                        IUser otherUser = Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == UserState.TALKING) {
                            if (otherUser.Player.ActionState != CharacterActionState.Hiding && otherUser.Player.ActionState != CharacterActionState.Sneaking){  //(string.IsNullOrEmpty(PassesHideCheck(otherUser, ignoreId, out spot))) { //player should do a spot check this should not be a given
                                sb.AppendLine(otherUser.Player.FirstName + " is " + otherUser.Player.StanceState.ToString().ToLower() + " here.");
                            }  
                        }
                    }
                }
                Dictionary<string, int> npcGroups = new Dictionary<string, int>();

                foreach (string id in room.GetObjectsInRoom(RoomObjects.Npcs)) {
                    var npc = Character.NPCUtils.GetAnNPCByID(id);

                    if (!npcGroups.ContainsKey(npc.FirstName + "$" + npc.LastName + "$" + npc.StanceState)) {
                        npcGroups.Add(npc.FirstName + "$" + npc.LastName + "$" + npc.StanceState, 1);
                    }
                    else {
                        npcGroups[npc.FirstName + "$" + npc.LastName + "$" + npc.StanceState] += 1;
                    }
                }

                foreach (KeyValuePair<string, int> pair in npcGroups) {
                    string[] temp = pair.Key.Split('$');
                    sb.AppendLine(temp[0] + " is " + temp[2].Replace("_", " ").ToLower() + " here. " + (pair.Value > 1 ? ("[x" + pair.Value + "]") : ""));
                }
            }
            else {
                int count = 0;
                foreach (string id in room.GetObjectsInRoom(RoomObjects.Players)) {
                    if (id != ignoreId) {
                        IUser otherUser = Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == UserState.TALKING) {
                            if (otherUser.Player.ActionState != CharacterActionState.Hiding && otherUser.Player.ActionState != CharacterActionState.Sneaking) {  //player should do a spot check this should not be a given
                                count++;
                            }
                        }
                    }
                }
                count += room.GetObjectsInRoom(RoomObjects.Npcs).Count;

                if (count == 1) {
                    sb.AppendLine("A presence is here.");
                }
                else if (count > 1) {
                    sb.AppendLine("Several presences are here.");
                }
            }

            return sb.ToString();
        }

       private static string DisplayItemsInRoom(IRoom room) {
            StringBuilder sb = new StringBuilder();

            List<string> itemsInRoom = room.GetObjectsInRoom(RoomObjects.Items);

            Dictionary<string, int> itemGroups = new Dictionary<string, int>();

            if (!room.IsDark) {
                foreach (string id in itemsInRoom) {

                    IItem item = Items.Items.GetByID(id).Result;
                    if (item != null) {
                       if (item.ItemType.ContainsKey(ItemsType.CONTAINER)) {
                            IContainer containerItem = item as IContainer;
                            if (!itemGroups.ContainsKey(item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]"))) {
                                itemGroups.Add(item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]"), 1);
                            }
                            else {
                                itemGroups[item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]")] += 1;
                            }
                        }
                        else if (item.ItemType.ContainsKey(ItemsType.DRINKABLE) || item.ItemType.ContainsKey(ItemsType.EDIBLE)) {
                            if (!itemGroups.ContainsKey(item.Name)) {
                                itemGroups.Add(item.Name, 1);
                            }
                            else {
                                itemGroups[item.Name] += 1;
                            }
                        }
                        else {
                            if (!itemGroups.ContainsKey(item.Name + "$" + item.CurrentCondition)) {
                                itemGroups.Add(item.Name + "$" + item.CurrentCondition, 1);
                            }
                            else {
                                itemGroups[item.Name + "$" + item.CurrentCondition] += 1;
                            }
                        }
                    }
                }
                 
                foreach (KeyValuePair<string, int> pair in itemGroups) {
                    string[] temp = pair.Key.Split('$');
                    sb.Append(temp[0] + " is laying here");
                    if (temp.Count() > 1 && !string.Equals(temp[1], "NONE", StringComparison.InvariantCultureIgnoreCase)) {
                        if (temp[1].Contains("[Opened]") || temp[1].Contains("[Closed]") || temp[1].Contains("[container]")) {
                            sb.AppendLine(". " + (temp[1] == "[container]" ? "" : temp[1]) + (pair.Value > 1 ? (" [x" + pair.Value + "]") : ""));
                        }
                        else {
                            sb.AppendLine(" in " + temp[1].Replace("_", " ").ToLower() + " condition." + (pair.Value > 1 ? ("[x" + pair.Value + "]") : ""));
                        }
                    }
                    else {
                        sb.AppendLine(".");
                    }
                }
            }
            else {
                int count = 0;
                foreach (string id in itemsInRoom) {
                    IItem item = Items.Items.GetByID(id).Result;
                    if (item != null) {
                        count++;
                    }
                }
                
                if (count == 1) {
                    sb.AppendLine("Something is laying here.");
                }
                else if (count > 1) {
                    sb.AppendLine("Somethings are laying here.");
                }
            }

            return sb.ToString();
        }

        //called from the LOOK command
        private static string HintCheck(IUser player) {
            StringBuilder sb = new StringBuilder();
            //let's display the room hints if the player passes the check
            foreach (IRoomModifier mod in Room.GetModifiers(player.Player.Location)) {
                foreach (Dictionary<string, object> dic in mod.Hints) {
                    if (player.Player.GetAttributeValue((string)dic["Attribute"]) >= (int)dic["ValueToPass"]) {
                        sb.AppendLine((string)dic["Display"]);
                    }
                }
            }
            return sb.ToString();
        }

        //called from the MOVE command
        private static void ApplyRoomModifier(IUser player) {
            StringBuilder sb = new StringBuilder();
            IMessage message = new Message();
            message.InstigatorID = player.UserID;
            message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
            //Todo:  Check the player bonuses to see if they are immune or have resistance to the modifier
            foreach (var modifier in Room.GetModifierEffects(player.Player.Location)) {
                player.Player.ApplyEffectOnAttribute("Hitpoints", (double)modifier["Value"]);

                double positiveValue = (double)modifier["Value"];
                if (positiveValue < 0) {
                    positiveValue *= -1;
                }

                sb.Append(String.Format((string)modifier["DescriptionSelf"], positiveValue));
                if (!player.Player.IsNPC) {
                    message.Self = "\r" + sb.ToString();
                }
                sb.Clear();
                sb.Append(String.Format((string)modifier["DescriptionOthers"], player.Player.FirstName,
                           player.Player.Gender.ToString() == "Male" ? "he" : "she", positiveValue));

                message.Room = "\r" + sb.ToString();
                Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<string>() { player.UserID });
                if (player.Player.IsNPC) {
                    player.MessageHandler(message);
                } else {
                    player.MessageHandler(message.Self);
                }
            }
        }
        

       
        //could be an item or an npc, we'll figure it out
        private static string GetObjectInPosition(int position, string name, string location) {
            string result = "";
            string[] parsedName = name.Split('.');
            
            result = GetNPCInPosition(position, name, location, parsedName);
            if (string.IsNullOrEmpty(result)) {
               result = GetItemInPosition(position, name, location, parsedName);
            }

            return result;
        }

        private static string GetItemInPosition(int position, string name, string location, string[] parsedName) {
            string result = "";

            MongoUtils.MongoData.ConnectToDatabase();
            var itemCollection = MongoUtils.MongoData.GetCollection<Items.Items>("World","Items" );
            List<string> itemIds = Room.GetRoom(location).GetObjectsInRoom(RoomObjects.Items);

            int count = 1;

            foreach (string ids in itemIds) {
                var item = MongoUtils.MongoData.RetrieveObject<Items.Items>(itemCollection, i => i.Id == ObjectId.Parse(ids));
                if (string.Equals(item.Name, parsedName[0], StringComparison.InvariantCultureIgnoreCase) && count == position) {
                    result = ids;
                    break;
                }
                else {
                    count++;
                    continue;
                }
            }

            return result;
        }

        private static string GetNPCInPosition(int position, string name, string location, string[] parsedName) {
            string result = "";
            var npcs = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            List<string> npcIds = Room.GetRoom(location).GetObjectsInRoom(RoomObjects.Npcs);

            int count = 1;

            foreach (string ids in npcIds) {
                var npc = MongoUtils.MongoData.RetrieveObject<NPC>(npcs, n => n.ID == ids);
                if (string.Equals(npc.FirstName, parsedName[0], StringComparison.InvariantCultureIgnoreCase) && count == position) {
                    result = ids;
                    break;
                }
                else {
                    count++;
                    continue;
                }
            }

            return result;
        }
    }
}
