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
        public GeneralTrigger(BsonDocument doc, string triggerType) {
            MessageOverrideAsString = new List<string>();
            if (doc != null && doc.ElementCount > 0 && doc.Contains("TriggerOn")) {
                TriggerOn = doc["TriggerOn"].AsString;
                ChanceToTrigger = doc["ChanceToTrigger"].AsInt32;
                script = ScriptFactory.GetScript(doc["ScriptID"].AsString, triggerType);
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
        public IScript script; //this will have to be casted to the proper script type unless triggers can only be LUA script

        public virtual void HandleEvent(object o, EventArgs e) {
            ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });           
        }
    }


    public class ItemTrigger : GeneralTrigger {
        public ItemTrigger(BsonDocument doc):base(doc, "Items") {}

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

            ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });
        }
    }
}
