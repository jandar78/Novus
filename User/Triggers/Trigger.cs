using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using LuaInterface;

//Trigger can be used to kick off scripts for pretty much anything, ranging from Quests to a special action an item can perform
//based on what the trigger is

namespace Triggers {
    public class GeneralTrigger : ITrigger {
        public GeneralTrigger(BsonDocument doc, TriggerType triggerType) {
            MessageOverrideAsString = new List<string>();
			if (doc != null && doc.ElementCount > 0 && doc.Contains("TriggerOn")) {
				TriggerOn = doc["TriggerOn"].AsString;
				ChanceToTrigger = doc["ChanceToTrigger"].AsInt32;
				script = ScriptFactory.GetScript(doc["ScriptID"].AsString, triggerType.ToString());
				foreach (var overrides in doc["Overrides"].AsBsonArray) {
					MessageOverrideAsString.Add(overrides.AsString);
				}
			}
        }

        public GeneralTrigger(){}
        public string TriggerOn { get; set; }
        public double ChanceToTrigger { get; set; }
        public BsonArray MessageOverrides { get; set; }
        public List<string> MessageOverrideAsString { get; set; }
        public string StateToExecute { get; set; }
        public IScript script; 

        public virtual void HandleEvent(object o, EventArgs e) {
			Task.Run(() => script.RunScript());
			// ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });           
		}

        public virtual void HandleEvent() {
            HandleEvent(null, null);
        }
    }


    public class QuestTrigger : GeneralTrigger {
        public QuestTrigger(BsonDocument doc):base(doc, TriggerType.Quests) {
        }

        public override void HandleEvent(object o, EventArgs e) {
            //for items we want to add the item and the owner into the script as variables
            var typeEventCaller = ((TriggerEventArgs)e).IdType;
            var callerID = ((TriggerEventArgs)e).Id;
			object caller = null;

            switch (typeEventCaller) {  
                case TriggerEventArgs.IDType.Npc:
                    caller = Character.NPCUtils.GetAnNPCByID(callerID);
                    break;
                case TriggerEventArgs.IDType.Room:
                    caller = Rooms.Room.GetRoom(callerID);
                    break;
                default:
                    break;
            }

            if (caller is Rooms.IRoom) {
                if (script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
                    script.AddVariable((Rooms.IRoom)caller, "room");
                }
                else {
                    script.AddVariable(((Rooms.IRoom)caller).Id.ToString(), "roomID");
                }
            }
            else if (caller is Character.Iactor) {
                if (script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
                    script.AddVariable((Character.Iactor)caller, "npc");
                }
                else {
                    script.AddVariable(((Character.Iactor)caller).ID.ToString(), "npcID");
                }
            }

			//add the player (instigator) to the script
			if (script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
				script.AddVariable(MySockets.Server.GetAUser(((TriggerEventArgs)e).InstigatorID), "player");
			}
			else {
				script.AddVariable(((TriggerEventArgs)e).InstigatorID, "playerID");
			}

			//add a message if there is one
			if (!string.IsNullOrEmpty(((TriggerEventArgs)e).Message)) {
				script.AddVariable(((TriggerEventArgs)e).Message, "message");
			}

			//trying this instead of the ThreadPool
			Task.Run(() => script.RunScript());

            //ThreadPool.QueueUserWorkItem(delegate {
            //    script.RunScript();
            //});
        }
    }

    public class ItemTrigger : GeneralTrigger {
        public ItemTrigger(BsonDocument doc):base(doc, TriggerType.Items) {}

        public override void HandleEvent(object o, EventArgs e) {
            //for items we want to add the item and the owner into the script as variables
            var item = Items.Items.GetByID(((Items.ItemEventArgs)e).ItemID.ToString());
			if (item != null) {
				if (script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
					script.AddVariable(item, "item");
				}
				else {
					script.AddVariable(item.Id.ToString(), "itemID");
				}
			}

            User.User player = MySockets.Server.GetAUser(item.Owner);
            if (player != null) {//the owner could be another item and not a player
				if (script.ScriptType == ScriptFactory.ScriptTypes.Lua) {
					script.AddVariable(player.Player, "player");
				}
				else {
					script.AddVariable(player.Player.ID, "playerID");
				}
            }

			Task.Run(() => script.RunScript());
			//ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });
        }
    }

    public enum TriggerType { Items, Quests, Door, NPC, Room};
}
