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
using Interfaces;

namespace Rooms {

    public class Exits : IExit {

        public Dictionary<RoomExits, IRoom> availableExits { get; set; }
        public Dictionary<RoomExits, IDoor> doors { get; set; }

        public bool HasDoor {
            get {
                return doors.Count > 0;
            }
        }

        public string Description { get; set; }
        public string Direction { get; set; }
        public string Name { get; set; }
        public string LeadsToRoom { get; set; }

        public Exits() {
            availableExits = new Dictionary<RoomExits, IRoom>();
            doors = new Dictionary<RoomExits, IDoor>();
        }
    }


    public class Door : IDoor {
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
			set;
		}

		public BsonArray Phrases { get; set; }
		public BsonArray Triggers { get; set; }

		public string Description { get; set; }
		public string DescriptionDestroyed { get; set; }
        #endregion Public Properties

        private List<ITrigger> _exitTriggers = new List<ITrigger>();

		public Door() { }

		public static IDoor GetDoor(string doorID, string doorID2 = "") {
			var doorCollection = MongoUtils.MongoData.GetCollection<Door>("World", "Doors");
			var doorDocument = MongoUtils.MongoData.RetrieveObject<Door>(doorCollection, d => d.Id == doorID || d.Id == doorID2);

			if (doorDocument != null) {
                if (doorDocument.Listener) {
                    doorDocument.LoadTriggers();
                }
			}

			return doorDocument;
		}

		public void LoadTriggers() {
            if (_exitTriggers == null)
            {
                _exitTriggers = new List<ITrigger>();
            }

            if (Triggers != null) {
                foreach (BsonDocument doc in Triggers) {
                    Triggers.GeneralTrigger trigger = new Triggers.GeneralTrigger(doc, TriggerType.Door);
                    trigger.script.AddVariable(this, "door");
                    if (trigger.script.ScriptType == ScriptTypes.Lua) {
                        LuaScript luaScript = trigger.script as LuaScript;
                        luaScript.RegisterMarkedMethodsOf(new DoorHelpers());

                    }
                    _exitTriggers.Add(trigger);
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

		public IDoor GetDoorFromDB() {
			var doorCollection = GetDoorCollection();
            return MongoUtils.MongoData.RetrieveObjectAsync<IDoor>(doorCollection, d => d.Id == this.Id).Result;
		}

		public IMongoCollection<IDoor> GetDoorCollection() {
			return MongoUtils.MongoData.GetCollection<IDoor>("World", "Doors");
		}

		public async void UpdateDoorStatus() {
			var door = GetDoorFromDB() as Rooms.Door;

			//these are the only things that players can change
			door.Open = this.Open;
			door.Locked = this.Locked;
			door.Hitpoints = this.Hitpoints;
			door.Destroyed = this.Destroyed;

			await MongoUtils.MongoData.SaveAsync<IDoor>(GetDoorCollection(), d => d.Id == door.Id, door);
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
				if (trigger.And.Count > 0) {
					foreach (string and in trigger.And) {
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
				amount = amount * Rooms.Room.GetRoom(location).GetObjectsInRoom(RoomObjects.Players, 100).Count;

				for (int i = 0; i < amount; i++) {
					IActor actor = Character.NPCUtils.CreateNPC(mobTypeID);
					if (actor != null) {
						actor.Location = location;
						//meh this whole AI stuff may need to be changed depending on how AI will handle triggers
						actor.LastCombatTime = DateTime.MinValue.ToUniversalTime();
						INpc npc = actor as INpc;
						npc.Fsm.state = AI.FindTarget.GetState();
						actor.Save();
					}
				}
			}
		}
		#endregion Helper methods for door scripts
	}
}
