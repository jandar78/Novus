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
using MongoDB.Driver.Linq;
using Extensions;

namespace Commands {
    public partial class CommandParser {

        //called from the LOOK command
        private static string DisplayPlayersInRoom(Room room, string ignoreId) {
            StringBuilder sb = new StringBuilder();

            if (!room.IsDark) {
                foreach (string id in room.GetObjectsInRoom(Room.RoomObjects.Players)) {
                    if (id != ignoreId) {
                        User.User otherUser = MySockets.Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == User.User.UserState.TALKING) {
                            if (otherUser.Player.ActionState != CharacterEnums.CharacterActionState.Hiding && otherUser.Player.ActionState != CharacterEnums.CharacterActionState.Sneaking){  //(string.IsNullOrEmpty(PassesHideCheck(otherUser, ignoreId, out spot))) { //player should do a spot check this should not be a given
                                sb.AppendLine(otherUser.Player.FirstName + " is " + otherUser.Player.StanceState.ToString().ToLower() + " here.");
                            }  
                        }
                    }
                }
                Dictionary<string, int> npcGroups = new Dictionary<string, int>();

                foreach (string id in room.GetObjectsInRoom(Room.RoomObjects.Npcs)) {
                    //MongoCollection npcs = MongoUtils.MongoData.GetCollection("Characters", "NPCCharacters"); //wtf is this here for? doesn't the for loop get all the NPC's?
                   // IMongoQuery query = Query.EQ("_id", ObjectId.Parse(id));
                   // BsonDocument npc = npcs.FindOneAs<BsonDocument>(query);

                    var npc = Character.NPCUtils.GetAnNPCByID(id);


                    //let's create groups for easy display
                    //if (!npcGroups.ContainsKey(npc.["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"])) {
                    //    npcGroups.Add(npc["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"], 1);
                    //}
                    //else {
                    //    npcGroups[npc["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"]] += 1;
                    //}

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
                foreach (string id in room.GetObjectsInRoom(Room.RoomObjects.Players)) {
                    if (id != ignoreId) {
                        User.User otherUser = MySockets.Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == User.User.UserState.TALKING) {
                            if (otherUser.Player.ActionState != CharacterEnums.CharacterActionState.Hiding && otherUser.Player.ActionState != CharacterEnums.CharacterActionState.Sneaking) {  //player should do a spot check this should not be a given
                                count++;
                            }
                        }
                    }
                }
                count += room.GetObjectsInRoom(Room.RoomObjects.Npcs).Count;

                if (count == 1) {
                    sb.AppendLine("A presence is here.");
                }
                else if (count > 1) {
                    sb.AppendLine("Several presences are here.");
                }
            }

            return sb.ToString();
        }

       private static string DisplayItemsInRoom(Room room) {
            StringBuilder sb = new StringBuilder();

            List<string> itemsInRoom = room.GetObjectsInRoom(Room.RoomObjects.Items);

            Dictionary<string, int> itemGroups = new Dictionary<string, int>();

            if (!room.IsDark) {
                foreach (string id in itemsInRoom) {

                    Items.Iitem item = Items.Items.GetByID(id);
                    if (item != null) {
                       // Items.Icontainer containerItem = item as Items.Icontainer;
                       // Items.Iedible edible = item as Items.Iedible;

                        if (item.ItemType.ContainsKey(Items.ItemsType.CONTAINER)) {
                            Items.Icontainer containerItem = item as Items.Icontainer;
                            if (!itemGroups.ContainsKey(item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]"))) {
                                itemGroups.Add(item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]"), 1);
                            }
                            else {
                                itemGroups[item.Name + "$" + (containerItem.IsOpenable == true ? (containerItem.Opened ? "[Opened]" : "[Closed]") : "[container]")] += 1;
                            }
                        }
                        else if (item.ItemType.ContainsKey(Items.ItemsType.DRINKABLE) || item.ItemType.ContainsKey(Items.ItemsType.EDIBLE)) {
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
                    Items.Iitem item = Items.Items.GetByID(id);
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
        private static string HintCheck(User.User player) {
            StringBuilder sb = new StringBuilder();
            //let's display the room hints if the player passes the check
            foreach (RoomModifier mod in Rooms.Room.GetModifiers(player.Player.Location)) {
                foreach (Dictionary<string, string> dic in mod.Hints) {
                    if (player.Player.GetAttributeValue(dic["Attribute"]) >= int.Parse(dic["ValueToPass"])) {
                        sb.AppendLine(dic["Display"]);
                    }
                }
            }
            return sb.ToString();
        }

        //called from the MOVE command
        private static void ApplyRoomModifier(User.User player) {
            StringBuilder sb = new StringBuilder();
            //Todo:  Check the player bonuses to see if they are immune or have resistance to the modifier
            foreach (Dictionary<string, string> modifier in Rooms.Room.GetModifierEffects(player.Player.Location)) {
                player.Player.ApplyEffectOnAttribute("Hitpoints", double.Parse(modifier["Value"]));

                double positiveValue = double.Parse(modifier["Value"]);
                if (positiveValue < 0) {
                    positiveValue *= -1;
                }

                sb.Append(String.Format(modifier["DescriptionSelf"], positiveValue));
                if (!player.Player.IsNPC) {
                    player.MessageHandler("\r" + sb.ToString());
                }
                sb.Clear();
                sb.Append(String.Format(modifier["DescriptionOthers"], player.Player.FirstName,
                           player.Player.Gender.ToString() == "Male" ? "he" : "she", positiveValue));

                Room.GetRoom(player.Player.Location).InformPlayersInRoom("\r" + sb.ToString(), new List<string>(new string[] { player.UserID }));
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
            MongoCollection itemCollection = MongoUtils.MongoData.GetCollection("World","Items" );
            List<string> itemIds = Room.GetRoom(location).GetObjectsInRoom(Room.RoomObjects.Items);

            int count = 1;

            foreach (string ids in itemIds) {
                IMongoQuery query = Query.EQ("_id", ObjectId.Parse(ids));
                BsonDocument item = itemCollection.FindOneAs<BsonDocument>(query);
                if (string.Equals(item["Name"].AsString, parsedName[0], StringComparison.InvariantCultureIgnoreCase) && count == position) {
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
            
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("Characters");
            MongoCollection npcs = db.GetCollection("NPCCharacters");
            List<string> npcIds = Room.GetRoom(location).GetObjectsInRoom(Room.RoomObjects.Npcs);

            int count = 1;

            foreach (string ids in npcIds) {
                IMongoQuery query = Query.EQ("_id", ObjectId.Parse(ids));
                BsonDocument npc = npcs.FindOneAs<BsonDocument>(query);
                if (string.Equals(npc["FirstName"].AsString, parsedName[0], StringComparison.InvariantCultureIgnoreCase) && count == position) {
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
