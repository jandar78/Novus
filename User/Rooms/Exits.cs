﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using System.Reflection;
using Commands;
using System.Threading;
using Phydeaux.Utilities;

namespace Rooms {
    public class Exits {
		 
		 public Dictionary<string, Room> availableExits;
		 public Dictionary<string, Door> doors;

        //item1 is the phrase to match, item2 is the action to execute

         public bool HasDoor {
             get {
                 return doors.Count > 0;
             }
         }

		 public string Description {
			 get;
			 set;
		 }

		 public string Direction {
			 get;
			 set;
		 }

        public Exits() {
			  availableExits = new Dictionary<string, Room>();
			  doors = new Dictionary<string, Door>();
        }
    }

    public class Door {
        public List<Tuple<string, string, List<object>>> phraseList;

        public string Id { get;  set; }

        public string Examine { get;  set; }

        public bool Breakable { get;  set; }

        public bool Openable { get;  set; }

        public bool Climable { get;  set; }

        public bool Crawlable { get;  set; }

        public bool Lockable { get;  set; }

        public bool IsPeekable {
            get {
                if (Open || HasKeyHole) {
                    return true;
                }
                else {
                    return false;
                }
            }
        }

        public string Type { get; set; }

        public double Hitpoints { get; set; }

        public bool Open { get; set; }

        public bool Locked { get; set; }

        public string Name { get; set; }

        public bool RequiresKey { get; set; }

        public string GetDescription {
            get {
                if (Hitpoints > 0) {
                    return Description;
                }
                else {
                    return DescriptionDestroyed;
                }
            }
        }

        public bool HasKeyHole { get; set; }

        public bool Destroyed { get; set; }
       
        public bool Listener {
            get;
            protected set;
        }

        public BsonArray Phrases { get; set; }

        public string Description { get; set; }
        public string DescriptionDestroyed { get; set; }

        public Door() { }

        public static Door GetDoor(string doorID, string doorID2 = ""){
            Door door = null;

            MongoCollection roomCollection = MongoUtils.MongoData.GetCollection("World", "Doors");
            IMongoQuery doorQuery = Query.Or(Query.EQ("_id", doorID), Query.EQ("_id", doorID2));
            BsonDocument roomDocument = roomCollection.FindOneAs<BsonDocument>(doorQuery);

            if (roomDocument != null) {
                door = BsonSerializer.Deserialize<Door>(roomDocument);
                if (door.Listener) door.FillUpPhrases();
            }

            return door;
        }

        private void FillUpPhrases() {
            phraseList = new List<Tuple<string, string, List<object>>>();
            List<object> parameters = new List<object>();
            foreach (BsonDocument phrase in Phrases) {
                foreach (BsonDocument method in phrase["Action"].AsBsonArray.Where(a => a.AsBsonDocument.Count() > 0)) {
                    foreach (BsonDocument param in method["Parameters"].AsBsonArray.Where(p => p.AsBsonDocument.Count() > 0)) {
                        switch (param["Param"].BsonType) {
                            case BsonType.Boolean:
                                parameters.Add(param["Param"].AsBoolean);
                                break;
                            case BsonType.Int32:
                                parameters.Add(param["Param"].AsInt32);
                                break;
                            case BsonType.Double:
                                parameters.Add(param["Param"].AsDouble);
                                break;
                            case BsonType.String:
                            default:
                                parameters.Add(param["Param"].AsString);
                                break;
                        }

                    }
                    phraseList.Add(Tuple.Create(phrase["Phrase"].AsString, method["Method"].AsString, new List<object>(parameters)));
                    parameters.Clear();
                }
            }
        }

        public List<string> ApplyDamage(double damage) {
            List<string> message = new List<string>();
            if (!Breakable) {
                message.Add("You could hit this " + this.Name.ToLower() + " all day and not even dent it.");
                message.Add("{0} could hit this " + this.Name.ToLower() + " all day and it would not even dent.");
            }
            else {
                Hitpoints = 0;
                message.Add("You hit the " + this.Name.ToLower() + " and it smashes to smithereens!");
                message.Add("{0} hits the " + this.Name.ToLower() + " and it smashes to smithereens!");
                Destroyed = true;
                Open = true;
                Locked = false;
            }
            return message;
        }

        private BsonDocument GetDoorFromDB() {
            MongoCollection doorCollection = GetDoorCollection();

            IMongoQuery query = Query.EQ("_id", this.Id);

            return doorCollection.FindOneAs<BsonDocument>(query);
        }

        private MongoCollection GetDoorCollection() {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase worldDB = MongoUtils.MongoData.GetDatabase("World");
            return worldDB.GetCollection("Doors");
        }

        public void UpdateDoorStatus() {
            BsonDocument door = GetDoorFromDB();

            //these are the only things that players can change
            door["Open"] = this.Open;
            door["Locked"] = this.Locked;
            door["Hitpoints"] = this.Hitpoints;
            door["Destroyed"] = this.Destroyed;

            GetDoorCollection().Save(door, WriteConcern.Acknowledged);
        }

        public string CheckPhrase(string message, out List<object> paramsOut) {
            string result = "";
            paramsOut = null;
            if (string.IsNullOrEmpty(message)) {
                return result;
            }

            message = message.Substring(message.IndexOf("says") + 4).Replace("\"", "").Trim();
            foreach (Tuple<string, string, List<object>> phrase in phraseList) {
                if (string.Equals(phrase.Item1, message, StringComparison.InvariantCultureIgnoreCase)) { //phrase matches let's perform the action
                    ThreadPool.QueueUserWorkItem(delegate { IterateThroughActions(phrase.Item1); });
                    break;
                }
            }
            return result;
        }

        private void IterateThroughActions(string match) {
            Type type = typeof(Rooms.Room);
            foreach (Tuple<string, string, List<object>> phrase in phraseList.Where(p => p.Item1 == match)) {
                var method = Phydeaux.Utilities.Dynamic<DoorHelpers>.Static.Function<object>.Explicit<List<object>>.CreateDelegate(phrase.Item2);
                method(phrase.Item3); 
            }
        }
    }

    #region Helper Methods for Dynamically bound methods for doors
    //These methods below are used to call the appropriate methods without having to cycle through all the classes to find the correct method why dynamically binding them
    //so they are all kept here and make the call to each class that has
    public class DoorHelpers {
        
        public static object OpenDoor(List<object> parameters) {
            if (CommandParser.OpenDoorOverride((int)parameters[1], (string)parameters[0])) {
                InformAllPlayersInRoom(parameters);
            }
            return null;
        }

        public static object CloseDoor(List<object> parameters) {
            if (CommandParser.CloseDoorOverride((int)parameters[1], (string)parameters[0])) {
                InformAllPlayersInRoom(parameters);
            }
            return null;
        }

        public static object LockDoor(List<object> parameters) {
            if (CommandParser.LockDoorOverride((int)parameters[1], (string)parameters[0])) {
                InformAllPlayersInRoom(parameters);
            }
            return null;
        }

        public static object UnlockDoor(List<object> parameters) {
            if (CommandParser.UnlockDoorOverride((int)parameters[1], (string)parameters[0])) {
                InformAllPlayersInRoom(parameters);
            }
            return null;
        }

        public static object InformAllPlayersInRoom(List<object> parameters) {
            Room.GetRoom((int)parameters[1]).InformPlayersInRoom((string)parameters[0], new List<string>(new string[] { "" }));
            return null;
        }

        public static object Wait(List<object> parameters) {
            Thread.Sleep((int)parameters[0] * 1000);
            return null;
        }

        public static object CreateNPC(List<object> parameters) {
            int mobTypeID = (int)parameters[1];
            int location = (int)parameters[2];
            int amount = (int)parameters[0] * Rooms.Room.GetRoom(location).GetObjectsInRoom("PLAYERS").Count;

            for (int i = 0; i < amount; i++) {
                Character.Iactor actor = Character.NPCUtils.CreateNPC(mobTypeID);
                if (actor != null) {
                    actor.Location = location;
                    actor.LastCombatTime = DateTime.MinValue.ToUniversalTime();
                    Character.Inpc npc = actor as Character.Inpc;
                    npc.Fsm.state = AI.FindTarget.GetState();
                    actor.Save();
                }
            }
            return null;
        }
    }
    #endregion
}
