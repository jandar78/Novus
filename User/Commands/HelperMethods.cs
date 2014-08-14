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
                            if (otherUser.Player.ActionState != CharacterEnums.CharacterActionState.HIDING && otherUser.Player.ActionState != CharacterEnums.CharacterActionState.SNEAKING){  //(string.IsNullOrEmpty(PassesHideCheck(otherUser, ignoreId, out spot))) { //player should do a spot check this should not be a given
                                sb.AppendLine(otherUser.Player.FirstName + " is " + otherUser.Player.StanceState.ToString().ToLower() + " here.");
                            }  
                        }
                    }
                }
                Dictionary<string, int> npcGroups = new Dictionary<string, int>();

                foreach (string id in room.GetObjectsInRoom(Room.RoomObjects.Npcs)) {
                    //we don't need to create an npc object here since we can just poll the DB for the info
                    MongoUtils.MongoData.ConnectToDatabase();
                    MongoDatabase db = MongoUtils.MongoData.GetDatabase("Characters");
                    MongoCollection npcs = db.GetCollection("NPCCharacters");
                    IMongoQuery query = Query.EQ("_id", ObjectId.Parse(id));
                    BsonDocument npc = npcs.FindOneAs<BsonDocument>(query);
                    //let's create groups for easy display
                    if (!npcGroups.ContainsKey(npc["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"])) {
                        npcGroups.Add(npc["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"], 1);
                    }
                    else {
                        npcGroups[npc["FirstName"].AsString + "$" + npc["LastName"].AsString + "$" + npc["StanceState"]] += 1;
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
                            if (otherUser.Player.ActionState != CharacterEnums.CharacterActionState.HIDING && otherUser.Player.ActionState != CharacterEnums.CharacterActionState.SNEAKING) {  //player should do a spot check this should not be a given
                                count++;
                            }
                        }
                    }
                }
                count += room.GetObjectsInRoom(Room.RoomObjects.Npcs).Count;

                if (count == 1) {
                    sb.AppendLine("Someone is here.");
                }
                else if (count > 1) {
                    sb.AppendLine("Some persons are here.");
                }
            }

            return sb.ToString();
        }

        private static string PassesHideCheck(User.User target, string playerID, out bool spotted) {
            string message = null;
            spotted = true;

            if (target.Player.ActionState == CharacterEnums.CharacterActionState.HIDING || target.Player.ActionState == CharacterEnums.CharacterActionState.SNEAKING) {
                User.User player = MySockets.Server.GetAUser(playerID);              

                MongoUtils.MongoData.ConnectToDatabase();
                MongoDatabase db = MongoUtils.MongoData.GetDatabase("Messages");
                MongoCollection col = db.GetCollection("Skills");
                var skill = col.FindOneAs<BsonDocument>(Query.EQ("_id", "Spot"));
                List<string> attributes = new List<string>();
                foreach (BsonValue attrib in skill["Attributes"].AsBsonArray){
                    attributes.Add(attrib.AsString);
                }

                double result = 0; // CalculateSkillLevel(target, attributes) - CalculateSkillLevel(player, attributes);

                foreach (BsonDocument doc in skill["Message"].AsBsonArray) {
                    if (result >= doc["min"].AsInt32 && result < doc["max"].AsInt32) {
                        message = string.Format(doc["Msg"].AsString, target.Player.FirstName).FontColor(Utils.FontForeColor.YELLOW);
                        target.MessageHandler(string.Format(doc["MsgTarget"].AsString, player.Player.FirstName).FontColor(Utils.FontForeColor.YELLOW));
                        spotted = doc["spotted"].AsBoolean;
                        break;
                    }
                }
            }
            if (spotted) return null;
            else return "You fail to spot anyone hiding here.";
        }

        private static string PassSneakCheck(User.User target, string playerID, out bool spotted) {
            User.User player = MySockets.Server.GetAUser(playerID);
            string message = null;
            string msgOther = null;
            spotted = false;

            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("Messages");
            MongoCollection col = db.GetCollection("Skills");
            var mastery = col.FindOneAs<BsonDocument>(Query.EQ("_id", "SpotSneak"));

            double hide = 0; //GetHide(target.Player.GetSubAttributes()["Agility"], target.Player.GetSubAttributes()["Cunning"], target.Player.GetAttributeRank("Dexterity"));
            double spot = 0; //GetHide(player.Player.GetSubAttributes()["Agility"], player.Player.GetSubAttributes()["Cunning"], player.Player.GetAttributeRank("Dexterity"));

            double result = hide - spot;

            foreach (BsonDocument doc in mastery["Message"].AsBsonArray) {
                if (result >= doc["min"].AsInt32 && result < doc["max"].AsInt32) {
                    message = string.Format(doc["msg"].AsString, target.Player.FirstName).FontColor(Utils.FontForeColor.YELLOW);
                    spotted = doc["spotted"].AsBoolean;
                    break;
                }
            }

            foreach (BsonDocument doc in mastery["MessageOthers"].AsBsonArray) {
                if (result >= doc["min"].AsInt32 && result < doc["max"].AsInt32) {
                    msgOther = string.Format(doc["msg"].AsString, player.Player.FirstName).FontColor(Utils.FontForeColor.YELLOW);
                    target.MessageHandler(string.Format(msgOther));
                    break;
                }
            }

            return message;
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
            //Todo:  Build a dictionary with what the player is immune/has resistance to and for how much to then calculate the actual
            //       damage/buff value that will be applied to the player
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
        private static string GetObjectInPosition(int position, string name, int location) {
            string result = "";

            string[] parsedName = name.Split('.');
            //TODO: do the same for items at some point

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
