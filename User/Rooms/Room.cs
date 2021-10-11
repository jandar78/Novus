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
using Interfaces;
using Sockets;
using Triggers;

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
namespace Rooms
{

    //Will create a room object that has all the necessary information that way it can inherit from interfaces and let mongoDB do all the heavy lifting like with items.
    //Will keep GetPlayers, GetNPCS and GetItems as static calls though.
    public class Room : IRoom, IWeather
    {
        private string _description;
        private string _title;
        public List<ObjectId> players { get; set; }
        public List<ObjectId> npcs { get; set; }
        public List<ObjectId> items { get; set; }

        [BsonIgnoreIfDefault]
        public string Id { get; set; }
        public string Title
        {
            get
            {
                if (IsDark)
                {
                    return "Somewhere Dark";
                }
                return _title;
            }
            set
            {
                _title = value;
            }
        }

        public string Zone
        {
            get
            {
                return GetZoneCode(Id);
            }
        }

        public int RoomId
        {
            get
            {
                return ParseRoomID(Id);
            }
        }

        public BsonArray Descriptions { get; set; }
        public BsonArray Conditions { get; set; }
        public string CurrentCondition { get; set; }
        public DateTime ConditionTimeEnd { get; set; }
        public string WeatherMessage { get; set; }
        public string Description
        {
            get
            {
                if (IsDark)
                {
                    return "It is too dark to see anything!";
                }

                return _description + (WeatherMessage ?? "");

            }
            set
            {
                _description = value;
            }
        }
        public bool IsDark
        {
            get
            {
                if (!IsLightSourcePresent && ((IsOutdoors && Calendar.Calendar.IsNight()) ||
               (GetRoomType() & RoomTypes.DARK_CAVE) == RoomTypes.DARK_CAVE))
                {
                    return true;
                }
                return false;
            }
        }
        public bool IsLightSourcePresent
        {
            get
            {
                return Task.Run(async () => await LightSourcePresent()).Result;
            }
        }

        public bool IsOutdoors
        {
            get
            {
                return (GetRoomType() & RoomTypes.OUTDOORS) == RoomTypes.OUTDOORS;
            }
        }
        public string Type { get; set; }
        public List<Exits> RoomExits { get; set; }
        public BsonArray Modifiers { get; set; }
        public int WeatherIntensity { get; set; }
        public string Weather { get; set; }
        public List<GeneralTrigger> Triggers { get; set; }

        //constructor
        public Room() { }

        public IExit GetRoomExit(RoomExits direction = Interfaces.RoomExits.None)
        {
            GetRoomExits();
            IExit result = null;
            if (direction != Interfaces.RoomExits.None)
            {
                result = RoomExits.Where(e => e.Direction.ToUpper() == direction.ToString().ToUpper()).SingleOrDefault();
            }
            else
            {
                result = RoomExits.FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// This method should be called after the room has been instantiated to prevent a stack overflow since it will become a recursive call otherwise.
        /// </summary>
        public void GetRoomExits()
        {
            List<Exits> exitList = new List<Exits>();

            foreach (var doc in RoomExits)
            {
                var exit = new Exits();
                exit.AvailableExits.Add((RoomExits)Enum.Parse(typeof(RoomExits), doc.Name), GetRoom(doc.LeadsToRoom)); //causing stackoverflow because exits point to each other
                //if it has door grab that as well
                //this query looks for a door with an id of either "roomid-adjecentroomid" or "adjacentroomid-roomid"
                string oneWay = Id.ToString() + "-" + exit.AvailableExits[(RoomExits)Enum.Parse(typeof(RoomExits), doc.Name)].Id;
                string anotherWay = exit.AvailableExits[(RoomExits)Enum.Parse(typeof(RoomExits), doc.Name)].Id + "-" + Id.ToString();

                IDoor door = Door.GetDoor(oneWay, anotherWay);
                RoomExits exitDirection = (RoomExits)Enum.Parse(typeof(RoomExits), doc.Name);
                if (door != null)
                {
                    exit.Doors.Add(exitDirection, door);
                }

                exit.Direction = exitDirection.ToString().ToLower();

                //door description overrides it unless it's blank
                if (exit.Doors.Count > 0)
                {
                    string doorDescription = exit.Doors.ContainsKey(exitDirection) ? (exit.Doors[exitDirection].Destroyed == true ? exit.Doors[exitDirection].Description + " that leads to " + exit.Description : exit.Doors[exitDirection].Description) : "";
                    if (!string.IsNullOrEmpty(doorDescription)) exit.Description = doorDescription;
                }

                exitList.Add(exit);
            }
            RoomExits = exitList;
        }

        public string GetDirectionOfDoor(string doorId)
        {
            GetRoomExits(); //populate the Exit list
            RoomExits direction = Interfaces.RoomExits.None;

            //only get the exits that have doors
            foreach (var exit in RoomExits)
            {
                if (!exit.HasDoor)
                {
                    continue;
                }
                direction = exit.Doors.Where(d => d.Value.Id.Contains(doorId)).Select(d => d.Key).SingleOrDefault();
            }

            return direction.ToString();
        }

        public RoomTypes GetRoomType()
        {
            string[] types = Type.Split(' ');
            RoomTypes roomType = RoomTypes.NONE;

            if (types.Count() > 0)
            {
                foreach (string type in types)
                {
                    RoomTypes parsedEnum = (RoomTypes)Enum.Parse(typeof(RoomTypes), type.ToUpper());
                    roomType = roomType | parsedEnum;
                }

                roomType = roomType ^ RoomTypes.NONE; //let's get rid of the NONE
            }

            return roomType;
        }

        private async Task<bool> LightSourcePresent()
        {
            //foreach player/npc/item in room check to see if it is lightsource and on
            bool lightSource = false;

            //check th eplayers and see if anyones equipment is a lightsource
            if (players != null)
            {
                foreach (var id in players)
                {
                    IUser temp = Server.GetAUser(id);
                    Dictionary<Wearable, IItem> equipped = temp.Player.Equipment.GetEquipment(temp.Player);
                    foreach (IItem item in equipped.Values)
                    {
                        IIluminate light = item as IIluminate;
                        if (light != null && light.IsLit)
                        {
                            lightSource = true;
                            break;
                        }
                    }
                    if (lightSource) break;
                }
            }
            //check the NPCS and do the same as we did with the players
            if (!lightSource)
            {
                if (npcs != null)
                {
                    foreach (var id in npcs)
                    {
                        IActor actor = Character.NPCUtils.GetAnNPCByID(id);
                        Dictionary<Wearable, IItem> equipped = actor.Equipment.GetEquipment(actor);
                        foreach (IItem item in equipped.Values)
                        {
                            IIluminate light = item as IIluminate;
                            if (light != null && light.IsLit)
                            {
                                lightSource = true;
                                break;
                            }
                        }
                        if (lightSource) break;
                    }
                }
            }

            //finally check for items in the room (just ones laying in the room, if a container is open check in container)
            if (!lightSource)
            {
                if (items != null)
                {
                    foreach (var id in items)
                    {
                        IItem item = await Items.Items.GetByID(id);
                        IContainer container = item as IContainer;
                        IIluminate light = item as IIluminate; //even if container check this first. Container may have light enchanment.
                        if (light != null && light.IsLit)
                        {
                            lightSource = true;
                            break;
                        }
                        if (container != null && (container.Contents != null && container.Contents.Count > 0) && container.Opened)
                        {//let's look in the container only if it's open
                            foreach (var innerId in container.Contents)
                            {
                                IItem innerItem = await Items.Items.GetByID(id);
                                light = innerItem as IIluminate;
                                if (light != null && light.IsLit)
                                {
                                    lightSource = true;
                                    break;
                                }
                            }
                        }

                        if (lightSource) break;
                    }
                }
            }
            return lightSource;
        }

        //just an overload since Lua will return any of our lists as objects. We just cast and call the real method.
        //I tried just using generic methods like Table2List<T>() but it didn't work out, Lua still complained.
        public void InformPlayersInRoom(IMessage message, List<object> ignoreId)
        {
            InformPlayersInRoom(message, ignoreId.Select(s => (ObjectId)s).ToList());
        }

        public void InformPlayersInRoom(IMessage message, List<ObjectId> ignoreId)
        {
            if (!string.IsNullOrEmpty(message.Room))
            {
                GetPlayersInRoom();
                foreach (var id in players)
                {
                    if (!ignoreId.Contains(id))
                    {
                        IUser otherUser = Server.GetAUser(id);
                        if (otherUser != null && otherUser.CurrentState == UserState.TALKING)
                        {
                            otherUser.MessageHandler(message.Room);
                        }
                    }
                }

                GetNPCsInRoom();
                foreach (var id in npcs)
                {
                    if (!ignoreId.Contains(id))
                    {
                        Interfaces.IUser otherUser = Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { id });
                        if (otherUser != null && otherUser.CurrentState == Interfaces.UserState.TALKING)
                        {
                            otherUser.MessageHandler(message);
                        }
                    }
                }

                //Here we want to see if this room has any triggers or if any of the exits have any triggers
                CheckRoomTriggers(message.Room);
                GetRoomExits();
                if (RoomExits != null)
                {
                    RoomExits exitDirection = Interfaces.RoomExits.None;
                    foreach (IExit exit in RoomExits)
                    {
                        exitDirection = (Interfaces.RoomExits)Enum.Parse(typeof(Interfaces.RoomExits), exit.Direction.CamelCaseWord());
                        if (exit.Doors.Count > 0 && exit.Doors[exitDirection].Listener)
                        {
                            string methodToCall = exit.Doors[exitDirection].CheckPhrase(message.Room);
                        }
                    }
                }
            }
        }

        //for items we can have triggers subscribe to certain events and then fire off based on them, but for rooms since they aren't always in memory
        //we have to iterate through each trigger and then fire off each one that's a hit.  Same concept is going to apply to the exits.
        private void CheckRoomTriggers(string message)
        {
            if (Triggers != null)
            {
                foreach (ITrigger trigger in Triggers)
                {
                    bool hasOn = false;
                    bool hasAnd = false;
                    foreach (string on in trigger.TriggerOn)
                    {
                        if (message.Contains(on))
                        {
                            hasOn = true;
                            break;
                        }
                    }
                    if (trigger.And.Count > 0)
                    {
                        foreach (string and in trigger.And)
                        {
                            if (message.Contains(and))
                            {
                                hasAnd = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        hasAnd = true;
                    }
                    foreach (string not in trigger.NotOn)
                    {
                        if (message.Contains(not))
                        {
                            hasOn = false;
                            break;
                        }
                    }
                    if (hasOn && hasAnd)
                    {
                        if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 100) <= trigger.ChanceToTrigger)
                        {
                            trigger.HandleEvent(null, null);
                        }
                    }
                }
            }
        }

        public List<ObjectId> GetObjectsInRoom(string objectType, double percentage = 100)
        {
            return GetObjectsInRoom((RoomObjects)Enum.Parse(typeof(RoomObjects), objectType), percentage);
        }

        public List<ObjectId> GetObjectsInRoom(RoomObjects objectType, double percentage = 100)
        {
            List<ObjectId> result = new List<ObjectId>();
            object objectList = null;

            switch (objectType)
            {
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

            foreach (ObjectId id in (dynamic)objectList)
            {
                if (RandomNumber.GetRandomNumber().NextNumber(0, 100) <= percentage)
                {
                    result.Add(id);
                }
            }

            return result;
        }

        public void Save()
        {
            MongoUtils.MongoData.Save<Room>(MongoUtils.MongoData.GetCollection<Room>("Rooms", GetZoneCode(Id)), r => r.Id == Id, this);
        }

        //private void Save(BsonDocument roomDoc) {
        //    MongoUtils.MongoData.ConnectToDatabase();
        //    var db = MongoUtils.MongoData.GetDatabase("World");
        //    var collection = db.GetCollection("Rooms");

        //    collection.Save(roomDoc);
        //}

        private void GetNPCsInRoom()
        {
            var npcsInRoom = MongoUtils.MongoData.RetrieveObjects<NPC>(MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters"), n => n.Location == Id);

            if (npcs == null)
            {
                npcs = new List<ObjectId>();
            }

            foreach (var npc in npcsInRoom)
            {
                if (!npcs.Contains(npc.Id))
                {
                    npcs.Add(npc.Id);
                }
            }
        }

        private void GetPlayersInRoom()
        {
            var playersInRoom = MongoUtils.MongoData.RetrieveObjects<Character.Character>(MongoUtils.MongoData.GetCollection<Character.Character>("Characters", "PlayerCharacter"), n => n.Location == Id);
            players = new List<ObjectId>();

            foreach (var player in playersInRoom)
            {
                IUser temp = Server.GetAUser(player.Id);
                if (temp != null && (temp.CurrentState != UserState.LIMBO || temp.CurrentState != UserState.LOGGING_IN ||
                     temp.CurrentState != UserState.CREATING_CHARACTER || temp.CurrentState != UserState.JUST_CONNECTED))
                {
                    players.Add(player.Id);
                }
            }
        }

        private void GetItemsInRoom()
        {
            var itemsInRoom = MongoUtils.MongoData.RetrieveObjects<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), i => i.Location == Id);

            items = new List<ObjectId>();

            foreach (var item in itemsInRoom)
            {
                items.Add(item.Id);
            }
        }


        //static methods that don't apply to creating a room object
        public static void ApplyRoomModifiers(int tick)
        {
            var roomCollection = MongoUtils.MongoData.GetCollections("Rooms").Result;
            var modifierCollection = MongoUtils.MongoData.GetCollection<RoomModifier>("World", "RoomModifiers");
            roomCollection.ForEach(collection =>
            {
                var roomsFound = MongoUtils.MongoData.RetrieveObjectsAsync<Room>("Rooms", collection["name"].AsString, r => r.Modifiers.Count > 0).Result;

                //allright this isn't as bad as it seems this actually executed pretty fast and it's running on a separate thread anyways since it's
                //coming off a timer event
                IRoom room = null;
                IMessage message = new Message();

                foreach (var temp in roomsFound)
                {
                    room = Rooms.Room.GetRoom(temp.Id);

                    var modArray = temp.Modifiers.Values;
                    if (modArray.Count() > 0)
                    {
                        foreach (var mods in modArray)
                        {
                            var modFound = MongoUtils.MongoData.RetrieveObject<RoomModifier>(modifierCollection, m => m.Id == mods["id"].AsInt32);

                            if (modFound.Timer > 0 && tick % modFound.Timer == 0)
                            { //we only want to go through the rooms where the timer has hit
                                var affectArray = modFound.Affects;
                                //we want to show the value always as positive to the players, only internally should they be negative
                                foreach (var affect in affectArray)
                                {
                                    double makePositive = 1;

                                    if ((double)affect["Value"] < 0)
                                    {
                                        makePositive = -1;
                                    }

                                    foreach (var playerid in room.players)
                                    {
                                        IUser user = Server.GetAUser(playerid);

                                        if (user != null)
                                        {
                                            user.Player.ApplyEffectOnAttribute("Hitpoints", (double)affect["Value"]);
                                            user.MessageHandler(String.Format(affect["DescriptionSelf"].ToString(), (double)affect["Value"] * makePositive));
                                            message.Room = String.Format(affect["DescriptionOthers"].ToString(), user.Player.FirstName,
                                                                user.Player.Gender.ToString() == "Male" ? "his" : "her");
                                            message.InstigatorID = user.Player.Id.ToString();
                                            message.InstigatorType = user.Player.IsNPC ? ObjectType.Npc : ObjectType.Player;
                                            room.InformPlayersInRoom(message, new List<ObjectId>() { user.UserID });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        public static List<Dictionary<string, object>> GetModifierEffects(string roomId)
        {
            var affects = new List<Dictionary<string, object>>();
            foreach (RoomModifier mod in GetModifiers(roomId))
            {
                foreach (var dic in mod.Affects)
                {
                    if (mod.Timer == 0)
                    {
                        affects.Add(dic);
                    }
                }
            }

            return affects;
        }

        public static List<IRoomModifier> GetModifiers(string roomId)
        {
            List<IRoomModifier> roomModifierList = new List<IRoomModifier>();

            var roomFound = MongoUtils.MongoData.RetrieveObject<Room>(MongoUtils.MongoData.GetCollection<Room>("Rooms", GetZoneCode(roomId)), r => r.Id == roomId);
            var modifiers = roomFound.Modifiers;

            var modifierCollection = MongoUtils.MongoData.GetCollection<RoomModifier>("World", "RoomModifiers");
            if (modifiers.Count() > 0)
            {
                foreach (var mod in modifiers)
                {
                    var modifier = MongoUtils.MongoData.RetrieveObject<RoomModifier>(modifierCollection, m => m.Id == mod["id"]);
                    if (modifier != null)
                    { //just in case you never know what stupid thing a builder might have done
                        roomModifierList.Add(modifier);
                    }
                }
            }
            return roomModifierList;
        }

        public static IRoom GetRoom(string roomID)
        {
            var roomCollection = MongoUtils.MongoData.GetCollection<Room>("Rooms", GetZoneCode(roomID));
            Room room = MongoUtils.MongoData.RetrieveObject<Room>(roomCollection, r => r.Id == roomID);

            room.UpdateDescription();
            room.GetPlayersInRoom();
            room.GetNPCsInRoom();
            room.GetItemsInRoom();
            room.LoadTriggers();

            return room;
        }

        private List<ITrigger> LoadTriggers()
        {
            List<ITrigger> triggerList = new List<ITrigger>();
            if (Triggers != null)
            {
                foreach (GeneralTrigger trigger in Triggers)
                {
                    //Triggers.GeneralTrigger triggerToAdd = new Triggers.GeneralTrigger(doc, TriggerType.Room);
                    triggerList.Add(trigger);
                }
            }
            return triggerList;
        }

        /// <summary>
        /// This method updates the room description depending on the currentDescriptionID.
        /// Any script that wants to change the room description just needs to update the currentDescriptionID in the DB and this will take care of the rest.
        /// </summary>
        private void UpdateDescription()
        {
            if (Conditions != null && Conditions.Count > 0)
            {
                //we are going to do a check to see if the timer expired
                DateTime conditionTime = ConditionTimeEnd.ToUniversalTime();
                if (conditionTime <= DateTime.UtcNow && conditionTime != DateTime.MaxValue)
                {
                    BsonDocument currentCondition = Conditions.Where(c => c.AsBsonDocument["id"] == CurrentCondition).SingleOrDefault().AsBsonDocument;
                    if (currentCondition != null)
                    {
                        BsonDocument nextCondition = Conditions.Where(c => c.AsBsonDocument["id"] == currentCondition["NextCondition"].AsString).SingleOrDefault().AsBsonDocument;
                        if (nextCondition != null)
                        {
                            CurrentCondition = nextCondition["id"].AsString;
                            if (nextCondition["Duration"].AsInt32 != -1)
                            {
                                ConditionTimeEnd = DateTime.UtcNow.AddMilliseconds(nextCondition["Duration"].AsInt32);
                            }
                            else
                            {
                                ConditionTimeEnd = DateTime.MaxValue;
                            }
                        }
                    }
                }

                BsonDocument currentDescription = Descriptions.Where(d => d.AsBsonDocument["id"].AsString == CurrentCondition).SingleOrDefault().AsBsonDocument;
                Description = currentDescription["Description"].AsString;
                Title = currentDescription["Title"].AsString;
            }

            MongoUtils.MongoData.Save<Room>(MongoUtils.MongoData.GetCollection<Room>("Rooms", GetZoneCode(Id)), r => r.Id == Id, this);
        }

        private static int ParseRoomID(string roomId)
        {
            int ignore = 0;
            foreach (char alpha in roomId)
            {
                if (!char.IsDigit(alpha))
                {
                    ignore++;
                }
                else
                {
                    break;
                }
            }

            int id = 0;
            int.TryParse(roomId.Substring(ignore), out id);

            return id;
        }

        private static string GetZoneCode(string roomId)
        {
            int include = 0;
            foreach (char alpha in roomId)
            {
                if (char.IsLetter(alpha))
                {
                    include++;
                }
                else
                {
                    break;
                }
            }

            return roomId.Substring(0, include);
        }
    }
}