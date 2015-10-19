using System;
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
using LuaInterface;
using Triggers;

namespace Rooms {
	public class Exits {

		public Dictionary<RoomExits, Room> availableExits;
		public Dictionary<RoomExits, Door> doors;

		public bool HasDoor {
			get {
				return doors.Count > 0;
			}
		}

		public string Description { get; set; }

		public string Direction { get; set; }

		public Exits() {
			availableExits = new Dictionary<RoomExits, Room>();
			doors = new Dictionary<RoomExits, Door>();
		}
	}

	public class Door {
		#region Public Properties
		public string Id { get; set; }

		public string Examine { get; set; }

		public bool Breakable { get; set; }

		public bool Openable { get; set; }

		public bool Climable { get; set; }

		public bool Crawlable { get; set; }

		public bool Lockable { get; set; }

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
		public BsonArray Triggers { get; set; }

		public string Description { get; set; }
		public string DescriptionDestroyed { get; set; }
		#endregion Public Properties

		private List<Triggers.ITrigger> _exitTriggers = new List<Triggers.ITrigger>();

		public Door() { }

		public static Door GetDoor(string doorID, string doorID2 = "") {
			Door door = null;

			MongoCollection roomCollection = MongoUtils.MongoData.GetCollection("World", "Doors");
			IMongoQuery doorQuery = Query.Or(Query.EQ("_id", doorID), Query.EQ("_id", doorID2));
			BsonDocument roomDocument = roomCollection.FindOneAs<BsonDocument>(doorQuery);

			if (roomDocument != null) {
				door = BsonSerializer.Deserialize<Door>(roomDocument);
				if (door.Listener)
					door.LoadTriggers();
			}

			return door;
		}

		private void LoadTriggers() {
			foreach (BsonDocument doc in Triggers) {
				Triggers.GeneralTrigger trigger = new Triggers.GeneralTrigger(doc, TriggerType.Door);
				trigger.script.AddVariable(this, "door");
				if (trigger.script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
					LuaScript luaScript = trigger.script as LuaScript;
					luaScript.RegisterMarkedMethodsOf(new DoorHelpers());

				}
				_exitTriggers.Add(trigger);
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
			return MongoUtils.MongoData.GetCollection("World", "Doors");
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

		public string CheckPhrase(string message) {
			string result = "";
			if (string.IsNullOrEmpty(message)) {
				return result;
			}

			message = message.Replace("\"", "").Trim();

			bool hasOn = false;
			bool hasAnd = false;
			foreach (ITrigger trigger in _exitTriggers) {
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

			return result;

		}

		#region Helper Methods for door scripts
		public class DoorHelpers {
			[LuaAccessible]
			public static void OpenDoor(string roomID, string direction) {
				CommandParser.OpenDoorOverride(roomID, direction);
			}

			[LuaAccessible]
			public static void CloseDoor(string roomID, string direction) {
				CommandParser.CloseDoorOverride(roomID, direction);
			}

			[LuaAccessible]
			public static void LockDoor(string roomID, string direction) {
				CommandParser.LockDoorOverride(roomID, direction);

			}

			[LuaAccessible]
			public static void UnlockDoor(string roomID, string direction) {
				CommandParser.UnlockDoorOverride(roomID, direction);

			}

			[LuaAccessible]
			public static void Wait(int seconds) {
				Thread.Sleep((int)seconds * 1000);
			}

			[LuaAccessible]
			public static void CreateNPC(int mobTypeID, string location, int amount) {
				amount = amount * Rooms.Room.GetRoom(location).GetObjectsInRoom(Room.RoomObjects.Players).Count;

				for (int i = 0; i < amount; i++) {
					Character.Iactor actor = Character.NPCUtils.CreateNPC(mobTypeID);
					if (actor != null) {
						actor.Location = location;
						//meh this whole AI stuff may need to be changed depending on how AI will handle triggers
						actor.LastCombatTime = DateTime.MinValue.ToUniversalTime();
						Character.Inpc npc = actor as Character.Inpc;
						npc.Fsm.state = AI.FindTarget.GetState();
						actor.Save();
					}
				}
			}
		}
		#endregion Helper methods for door scripts
	}
}
