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
using System.Reflection;
using Commands;
using System.Threading;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using ClientHandling;

//An explanation on how rooms, exits and doors work
//Rooms have exits whose direction points to the room it connects to.  These connections can have doors in between them that can block the player
//from going between rooms, the "doors" (they can be called barricade, portcullis, gate, etc.) can be opened and closed by player as well as in
//some cases destroyed (if the door is destructible).  Doors are visible from both connecting rooms opening, closing, destroying or locking
//the door in one room affects it the same in the connecting room.  If a door has a description it will be displayed, otherwise the exit description will be displayed
//if that is blank then finally the adjoining room title will be displayed.  If a door is destroyed then a destroyed description will be displayed followed by the exit
//or adjoinig room title. Mimicking the player being able to see through the destroyed door to what is beyond it.

//Doors can accept voice commands from players and then call call a methods(s) (by chaining them together you can make a script) that can then do something that affects the players or the world
//if someone utters the words "fire" in front of a door that says don't say fire, then maybe the entire room catches on fire.  Doors can also be opened, closed, locked, unlocked and even do things to other rooms
//so you may not see a result where you are but you could be incinerating people in another room.


//TODO:  Rooms could respawn items at different time intervals, this way areas are in charge of how many items exist in the world at a time by polling 
//the ItemQuantity collection and then loading up the item from the ItemTemplate collection.
namespace Rooms {

    //Will create a room object that has all the necessary information that way it can inherit from interfaces and let mongoDB do all the heavy lifting like with items.
    //Will keep GetPlayers, GetNPCS and GetItems as static calls though.
    public class Room : IRoom, IWeather {
        private string _description;
        private string _title;
        private List<string> players;
        private List<string> npcs;
        private List<string> items;
        

        private List<Triggers.ITrigger> _triggers;

        public string Id { get; set; }
        public string Title {
            get {
                if (IsDark) {
                    return "Somewhere Dark";
                }
                return _title;
            }
            set {
                _title = value;
            }
        }

        public string Zone {
            get {
                return GetZoneCode(Id);
            }
        }

        public int RoomId {
            get {
                return ParseRoomID(Id);
            }
        }

        public BsonArray Descriptions { get; set; }
        public BsonArray Conditions { get; set; }
        public string CurrentCondition { get; set; }
        public DateTime ConditionTimeEnd { get; set; }
        public string WeatherMessage { get; set; }
        public string Description {
            get {
                if (IsDark) {
                    return "It is too dark to see anything!";
                }
                
                return _description + (WeatherMessage ?? "");
                
            }
            set {
                _description = value;
            }
        }
        public bool IsDark {
            get {
                if (!IsLightSourcePresent && ((IsOutdoors && Calendar.Calendar.IsNight()) ||
               (GetRoomType() & RoomTypes.DARK_CAVE) == RoomTypes.DARK_CAVE)) {
                    return true;
                }
                return false;
            }
        }
        public bool IsLightSourcePresent {
            get {
                return LightSourcePresent();
            }
        }
        public bool IsOutdoors {
            get {
                return (GetRoomType() & RoomTypes.OUTDOORS) == RoomTypes.OUTDOORS;
            }
        }
        public string Type { get; set; }
        public BsonArray Exits { private get; set; }
        public BsonArray Modifiers { get; set; }
        public List<Exits> RoomExits { get; private set; }
        public int WeatherIntensity { get; set; }
        public string Weather { get; set; }
        private BsonArray Triggers { get; set; }
        //constructor
        public Room() { }

        public Exits GetRoomExit(RoomExits direction = Rooms.RoomExits.None) {
            GetRoomExits();
            Exits result = null;
            if (direction != Rooms.RoomExits.None) {
                result = RoomExits.Where(e => e.Direction.ToUpper() == direction.ToString().ToUpper()).SingleOrDefault();
            }
            else {
                result = RoomExits.FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// This method should be called after the room has been instantiated to prevent a stack voerflow since it will become a recursive call otherwise.
        /// </summary>
        public void GetRoomExits() {
            MongoCollection doorCollection = MongoUtils.MongoData.GetCollection("World", "Doors");

            List<Exits> exitList = new List<Exits>();

            foreach (BsonDocument doc in Exits) {
                Exits exit = new Exits();
                exit.availableExits.Add((RoomExits)Enum.Parse(typeof(RoomExits),doc["Name"].AsString), GetRoom(doc["LeadsToRoom"].AsString)); //causing stackoverflow because exits point to each other
                //if it has door grab that as well
                //this query looks for a door with an id of either "roomid-adjecentroomid" or "adjacentroomid-roomid"
                string oneWay = Id.ToString() + "-" + exit.availableExits[(RoomExits)Enum.Parse(typeof(RoomExits), doc["Name"].AsString)].Id;
                string anotherWay = exit.availableExits[(RoomExits)Enum.Parse(typeof(RoomExits), doc["Name"].AsString)].Id + "-" + Id.ToString();
                
                Door door = Door.GetDoor(oneWay, anotherWay);
				RoomExits exitDirection = (RoomExits)Enum.Parse(typeof(RoomExits), doc["Name"].AsString);
                if (door != null) {
                    exit.doors.Add(exitDirection, door);
                }

                exit.Direction = exitDirection.ToString().ToLower();

                //door description overrides it unless it's blank
                if (exit.doors.Count > 0) {
                    string doorDescription = exit.doors.ContainsKey(exitDirection) ? (exit.doors[exitDirection].Destroyed == true ? exit.doors[exitDirection].Description + " that leads to " + exit.Description : exit.doors[exitDirection].Description) : "";
                    if (!string.IsNullOrEmpty(doorDescription)) exit.Description = doorDescription;
                }

                exitList.Add(exit);
            }
            RoomExits = exitList;
        }

        public string GetDirectionOfDoor(int doorId) {
            GetRoomExits(); //populate the Exit list
			RoomExits direction = Rooms.RoomExits.None;

            //only get the exits that have doors
            foreach(Exits exit in RoomExits){
                if (!exit.HasDoor) {
                    continue;
                }
              direction = exit.doors.Where(d => d.Value.Id.Contains(doorId.ToString())).Select(d => d.Key).SingleOrDefault();
            }

            return direction.ToString();
        }

        public RoomTypes GetRoomType() {
            string[] types = Type.Split(' ');
            RoomTypes roomType = RoomTypes.NONE;

            if (types.Count() > 0) {
                foreach (string type in types) {
                    RoomTypes parsedEnum = (RoomTypes)Enum.Parse(typeof(RoomTypes), type.ToUpper());
                    roomType = roomType | parsedEnum;
                }

                roomType = roomType ^ RoomTypes.NONE; //let's get rid of the NONE
            }

            return roomType;
        }

        private bool LightSourcePresent() {
            //foreach player/npc/item in room check to see if it is lightsource and on
            bool lightSource = false;

            //check th eplayers and see if anyones equipment is a lightsource
            foreach (string id in players) {
                User.User temp = MySockets.Server.GetAUser(id);
                Dictionary<Items.Wearable, Items.Iitem> equipped = temp.Player.Equipment.GetEquipment();
                foreach (Items.Iitem item in equipped.Values) {
                    Items.Iiluminate light = item as Items.Iiluminate;
                    if (light != null && light.isLit) {
                        lightSource = true;
                        break;
                    }
                }
                if (lightSource) break;
            }

            //check the NPCS and do the same as we did with the players
            if (!lightSource) {
                foreach (string id in npcs) {
                    Character.Iactor actor = Character.NPCUtils.GetAnNPCByID(id);
                    Dictionary<Items.Wearable, Items.Iitem> equipped = actor.Equipment.GetEquipment();
                    foreach (Items.Iitem item in equipped.Values) {
                        Items.Iiluminate light = item as Items.Iiluminate;
                        if (light != null && light.isLit) {
                            lightSource = true;
                            break;
                        }
                    }
                    if (lightSource) break;
                }
            }

            //finally check for items in the room (just ones laying in the room, if a container is open check in container)
            if (!lightSource) {
                foreach (string id in items) {
                    Items.Iitem item = Items.Items.GetByID(id);
                    Items.Icontainer container = item as Items.Icontainer;
                    Items.Iiluminate light = item as Items.Iiluminate; //even if container check this first. Container may have light enchanment.
                    if (light != null && light.isLit) {
                        lightSource = true;
                        break;
                    }
                    if (container != null && (container.Contents != null && container.Contents.Count > 0) && container.Opened) {//let's look in the container only if it's open
                        foreach (string innerId in container.Contents) {
                            Items.Iitem innerItem = Items.Items.GetByID(id);
                            light = innerItem as Items.Iiluminate;
                            if (light != null && light.isLit) {
                                lightSource = true;
                                break;
                            }
                        }
                    }

                    if (lightSource) break;
                }
            }
            return lightSource;
        }

        //just an overload since Lua will return any of our lists as objects. We just cast and call the real method.
        //I tried just using generic methods like Table2List<T>() but it didn't work out, Lua still complained.
        public void InformPlayersInRoom(Message message, List<object> ignoreId) {
            InformPlayersInRoom(message, ignoreId.Select(s => s.ToString()).ToList());
        }

        public void InformPlayersInRoom(Message message, List<string> ignoreId) {
            if (!string.IsNullOrEmpty(message.Room)) {
                GetPlayersInRoom();
                foreach (string id in players) {
                    if (!ignoreId.Contains(id)) { 
                        User.User otherUser = MySockets.Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == User.User.UserState.TALKING) {
                            otherUser.MessageHandler(message.Room);
                        }
                    }
                }

                GetNPCsInRoom();
                foreach (string id in npcs) {
                    if (!ignoreId.Contains(id)) {
                        User.User otherUser = Character.NPCUtils.GetUserAsNPCFromList(new List<string>(new string[] { id }));
                        if (otherUser != null && otherUser.CurrentState == User.User.UserState.TALKING) {
                            otherUser.MessageHandler(message);
                        }
                    }
                }

                //Here we want to see if this room has any triggers or if any of the exits have any triggers
                CheckRoomTriggers(message.Room);
                GetRoomExits();
                if (RoomExits != null) {
					RoomExits exitDirection = Rooms.RoomExits.None;
                    foreach (Exits exit in RoomExits) {
						exitDirection = (Rooms.RoomExits)Enum.Parse(typeof(Rooms.RoomExits), exit.Direction.CamelCaseWord());
                        if (exit.doors.Count > 0 && exit.doors[exitDirection].Listener) {
                            string methodToCall = exit.doors[exitDirection].CheckPhrase(message.Room);
                        }
                    }
                }
            }
        }

        //for items we can have triggers subscribe to certain events and then fire off based on them, but for rooms since they aren't always in memory
        //we have to iterate through each trigger and then fire off each one that's a hit.  Same concept is going to apply to the exits.
        private void CheckRoomTriggers(string message) {
            if (_triggers != null) {
                foreach (Triggers.ITrigger trigger in _triggers) {
					bool hasOn = false;
					bool hasAnd = false;
					foreach (string on in trigger.TriggerOn) {
							if (message.Contains(on)) {
								hasOn = true;
								break;
							}
						}
						if (trigger.AndOn.Count > 0) {
							foreach (string and in trigger.AndOn) {
								if (message.Contains(and)) {
									hasAnd = true;
									break;
								}
							}
						}
						else {
							hasAnd = true;
						}
						foreach (string not in trigger.NotOn) {
							if (message.Contains(not)) {
								hasOn = false;
								break;
							}
						}
						if (hasOn && hasAnd) {
						if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 100) <= trigger.ChanceToTrigger) {
							trigger.HandleEvent(null, null);
						}
                    }
                }
            }
        }

		public List<string> GetObjectsInRoom(string objectType, double percentage = 100) {
			return GetObjectsInRoom((RoomObjects)Enum.Parse(typeof(RoomObjects), objectType), percentage);
		}

        public List<string> GetObjectsInRoom(RoomObjects objectType, double percentage = 100) {
            List<string> result = new List<string>();
            object objectList = null;

            switch (objectType) {
                case RoomObjects.Players:
                    objectList = players;
                    break;
                case RoomObjects.Npcs:
                    objectList = npcs;
                    break;
                case RoomObjects.Items:
                    objectList = items;
                    break;
                default:
                    break;
            }

            foreach (string id in (dynamic)objectList) {
                if (RandomNumber.GetRandomNumber().NextNumber(0, 100) <= percentage) {
                    result.Add(id);
                }
            }

            return result;
        }

        public enum RoomObjects { Players, Npcs, Items };

        public void Save() {
            BsonDocument roomDoc = new BsonDocument();
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("Rooms");

            collection.Save<Room>(this);
        }

        private void Save(BsonDocument roomDoc) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("Rooms");

            collection.Save(roomDoc);
        }

        private void GetNPCsInRoom() {
            MongoDatabase npcDB = MongoUtils.MongoData.GetDatabase("Characters");
            MongoCollection npcCollection = npcDB.GetCollection("NPCCharacters");
            IMongoQuery query = Query.EQ("Location", Id);
            MongoCursor npcsInRoom = npcCollection.FindAs<BsonDocument>(query);

            if (npcs == null) {
                npcs = new List<string>();
            }

            foreach (BsonDocument doc in npcsInRoom) {
				if (!npcs.Contains(doc["_id"].AsObjectId.ToString())) {
					npcs.Add(doc["_id"].AsObjectId.ToString());
				}
            }
        }

        private void GetPlayersInRoom() {
            MongoCollection playerCollection = MongoUtils.MongoData.GetCollection("Characters", "PlayerCharacter");
            IMongoQuery query = Query.EQ("Location", Id);
            MongoCursor playersInRoom = playerCollection.FindAs<BsonDocument>(query);

            players = new List<string>();

            foreach (BsonDocument doc in playersInRoom) {
                User.User temp = MySockets.Server.GetAUser(doc["_id"].AsObjectId.ToString());
                if (temp != null && (temp.CurrentState != User.User.UserState.LIMBO || temp.CurrentState != User.User.UserState.LOGGING_IN ||
                    temp.CurrentState != User.User.UserState.CREATING_CHARACTER || temp.CurrentState != User.User.UserState.JUST_CONNECTED)) {
                    players.Add(doc["_id"].AsObjectId.ToString());
                }
            }
        }

        private void GetItemsInRoom() {
            MongoDatabase playersDB = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection playerCollection = playersDB.GetCollection("Items");
            IMongoQuery query = Query.EQ("Location", Id);
            MongoCursor itemsInRoom = playerCollection.FindAs<BsonDocument>(query);

            items = new List<string>();

            foreach (BsonDocument doc in itemsInRoom) {
                Items.Iitem item = Items.Items.GetByID(doc["_id"].AsObjectId.ToString());
                if (item != null) {
                    items.Add(item.Id.ToString());
                }
            }
        }


        //static methods that don't apply to creating a room object
        public static void ApplyRoomModifiers(int tick) {
            List<MongoCollection> roomCollection = MongoUtils.MongoData.GetCollections("Rooms");
            MongoCollection modifierCollection = MongoUtils.MongoData.GetCollection("World", "RoomModifiers");
			foreach (MongoCollection collection in roomCollection) {
				MongoCursor roomsFound = collection.FindAs<BsonDocument>(Query.Exists("Modifiers"));

				//allright this isn't as bad as it seems this actually executed pretty fast and it's running on a separate thread anyways since it's
				//coming off a timer event
				Room room = null;
				Message message = new Message();

				foreach (BsonDocument doc in roomsFound) {
					room = Room.GetRoom(doc["_id"].AsString);

					BsonArray modArray = doc["Modifiers"].AsBsonArray;

					foreach (BsonDocument mods in modArray.Where(m => m.AsBsonDocument.Count() > 0)) {
						BsonDocument modFound = modifierCollection.FindOneAs<BsonDocument>(Query.EQ("_id", mods["id"]));

						if (modFound["Timer"].AsInt32 > 0 && tick % modFound["Timer"].AsInt32 == 0) { //we only want to go through the rooms where the timer has hit
							BsonArray affectArray = modFound["Affects"].AsBsonArray;
							//we want to show the value always as positive to the players, only internally should they be negative
							foreach (BsonDocument affect in affectArray) {
								double makePositive = 1;

								if (affect["Value"].AsDouble < 0) {
									makePositive = -1;
								}

								foreach (string playerid in room.players) {
									User.User user = MySockets.Server.GetAUser(playerid);

									if (user != null) {
										user.Player.ApplyEffectOnAttribute("Hitpoints", affect["Value"].AsDouble);
										user.MessageHandler(String.Format(affect["DescriptionSelf"].AsString, affect["Value"].AsDouble * makePositive));
										message.Room = String.Format(affect["DescriptionOthers"].AsString, user.Player.FirstName,
															user.Player.Gender.ToString() == "Male" ? "his" : "her");
										message.InstigatorID = user.Player.ID;
										message.InstigatorType = user.Player.IsNPC ? Message.ObjectType.Npc : Message.ObjectType.Player;
										room.InformPlayersInRoom(message, new List<string>() { user.UserID });
									}
								}
							}
						}
					}
				}
			}
        }

        public static List<Dictionary<string, string>> GetModifierEffects(string roomId) {
            List<Dictionary<string, string>> affects = new List<Dictionary<string, string>>();
            foreach (RoomModifier mod in GetModifiers(roomId)) {
                foreach (Dictionary<string, string> dic in mod.Affects) {
                    if (mod.TimeInterval == 0) {
                        affects.Add(dic);
                    }
                }
            }

            return affects;
        }

        public static List<RoomModifier> GetModifiers(string roomId) {
            List<RoomModifier> roomModifierList = new List<RoomModifier>();

            BsonDocument roomFound = MongoUtils.MongoData.GetCollection("Rooms", GetZoneCode(roomId)).FindOneAs<BsonDocument>(Query.EQ("_id", roomId));
            BsonArray modifiers = roomFound["Modifiers"].AsBsonArray;

            MongoDatabase worldDB = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection modifierCollection = worldDB.GetCollection("RoomModifiers");
            foreach (BsonDocument mod in modifiers.Where(m => m.AsBsonDocument.Count() > 0)) {
                IMongoQuery modifierQuery = Query.EQ("_id", mod["id"]);
                BsonDocument modifier = modifierCollection.FindOneAs<BsonDocument>(modifierQuery);
                if (modifier != null) { //just in case you never know what stupid thing a builder might have done
                    RoomModifier roomMod = new RoomModifier();
                    roomMod.TimeInterval = modifier["Timer"].AsInt32;

                    //get the hints for the modifier
                    BsonArray hintArray = modifier["Hints"].AsBsonArray;
                    foreach (BsonDocument hint in hintArray.Where(h => h.AsBsonDocument.Count() > 0)) {
                        Dictionary<string, string> dic = new Dictionary<string, string>();
                        dic.Add("Attribute", hint["Attribute"].AsString);
                        dic.Add("ValueToPass", hint["ValueToPass"].AsInt32.ToString());
                        dic.Add("Display", hint["Display"].AsString);
                        roomMod.Hints.Add(dic);
                    }

                    BsonArray affectArray = modifier["Affects"].AsBsonArray;
                    foreach (BsonDocument affect in affectArray.Where(a => a.AsBsonDocument.Count() > 0)) {
                        Dictionary<string, string> dic = new Dictionary<string, string>();
                        dic.Add("Name", affect["Name"].AsString);
                        dic.Add("Value", affect["Value"].AsDouble.ToString());
                        dic.Add("DescriptionSelf", affect["DescriptionSelf"].AsString);
                        dic.Add("DescriptionOthers", affect["DescriptionOthers"].AsString);
                        roomMod.Affects.Add(dic);
                    }
                    roomModifierList.Add(roomMod);
                }

            }
            return roomModifierList;
        }

        public static Room GetRoom(string roomID) {
            Room room = null;

            MongoCollection roomCollection = MongoUtils.MongoData.GetCollection("Rooms", GetZoneCode(roomID));
            IMongoQuery query = Query.EQ("_id", roomID);
            BsonDocument roomDocument = roomCollection.FindOneAs<BsonDocument>(query);

            room = BsonSerializer.Deserialize<Room>(roomDocument);
            room._triggers = LoadTriggers(room.Triggers);
            room.UpdateDescription(roomDocument);
            room.GetPlayersInRoom();
            room.GetNPCsInRoom();
            room.GetItemsInRoom();
            return room;
        }

        private static List<Triggers.ITrigger> LoadTriggers(BsonArray triggers) {
            List<Triggers.ITrigger> triggerList = new List<Triggers.ITrigger>();
            if (triggers != null) {
                foreach (BsonDocument doc in triggers) {
                   Triggers.GeneralTrigger triggerToAdd = new Triggers.GeneralTrigger(doc, global::Triggers.TriggerType.Room);
                   triggerList.Add(triggerToAdd);
                }
            }
            return triggerList;
        }

        /// <summary>
        /// This method updates the room description depending on the currentDescriptionID.
        /// Any script that wants to change the room description just needs to update the currentDescriptionID in the DB and this will take care of the rest.
        /// </summary>
        private void UpdateDescription(BsonDocument doc) {
            
            //we are going to do a check to see if the timer expired
            DateTime conditionTime = doc["ConditionTimeEnd"].ToUniversalTime();
            if ( conditionTime <=  DateTime.UtcNow && conditionTime != DateTime.MaxValue) {
                BsonDocument currentCondition = Conditions.Where(c => c.AsBsonDocument["id"] == doc["CurrentCondition"].AsString).SingleOrDefault().AsBsonDocument;
                if (currentCondition != null) {
                    BsonDocument nextCondition = Conditions.Where(c => c.AsBsonDocument["id"] == currentCondition["NextCondition"].AsString).SingleOrDefault().AsBsonDocument;
                    if (nextCondition != null) {
                        doc["CurrentCondition"] = nextCondition["id"].AsString;
                        if (nextCondition["Duration"].AsInt32 != -1) {
                            doc["ConditionTimeEnd"] = DateTime.UtcNow.AddMilliseconds(nextCondition["Duration"].AsInt32);
                        }
                        else {
                            doc["ConditionTimeEnd"] = DateTime.MaxValue;
                        }
                    }
                }
            }

            BsonDocument currentDescription = Descriptions.Where(d => d.AsBsonDocument["id"].AsString == doc["CurrentCondition"].AsString).SingleOrDefault().AsBsonDocument;
            doc["Description"] = currentDescription["Description"].AsString;
            doc["Title"] = currentDescription["Title"].AsString;

            Save(doc);
            Description = doc["Description"].AsString;
            Title = doc["Title"].AsString;
        }

        private static int ParseRoomID(string roomId) {
            int ignore = 0;
            foreach (char alpha in roomId) {
                if (!char.IsDigit(alpha)) {
                    ignore++;
                }
                else {
                    break;
                }
            }

            int id = 0;
            int.TryParse(roomId.Substring(ignore), out id);

            return id;
        }

        private static string GetZoneCode(string roomId) {
            int include = 0;
            foreach (char alpha in roomId) {
                if (char.IsLetter(alpha)) {
                    include++;
                }
                else {
                    break;
                }
            }

            return roomId.Substring(0, include);
        }
    }
}