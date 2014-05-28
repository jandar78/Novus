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
    public class ItemTrigger : ITrigger {
        public string TriggerOn { get; set; }
        public double ChanceToTrigger { get; set; }
        public BsonArray MessageOverrides { get; set; }
        public List<string> MessageOverrideAsString { get; set; }
        public string StateToExecute { get; set; }
        public Script script;

        public ItemTrigger(BsonDocument doc) {
            MessageOverrideAsString = new List<string>();

            if (doc != null && doc.ElementCount > 0) {
                TriggerOn = doc["TriggerOn"].AsString;
                ChanceToTrigger = doc["ChanceToTrigger"].AsDouble;
                script = new Script(doc["ScriptID"].AsString);
                
                foreach (var overrides in doc["Overrides"].AsBsonArray) {
                    MessageOverrideAsString.Add(overrides.AsString);
                }
            }
        }

        public void HandleEvent(object o, EventArgs e) {
            Items.ItemEventArgs ie = e as Items.ItemEventArgs;
            if (ie.ItemEvent == (Items.ItemEvent)Enum.Parse(typeof(Items.ItemEvent), TriggerOn)) {
                ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });
            }
        }
    }

    public class SpeechTrigger : ITrigger {
        public string TriggerOn { get; set; }
        public double ChanceToTrigger { get; set; }
        public BsonArray MessageOverrides { get; set; }
        public List<string> MessageOverrideAsString { get; set; }
        public string StateToExecute { get; set; }
        public Script script;

        public SpeechTrigger(BsonDocument doc) {
            MessageOverrideAsString = new List<string>();

            if (doc != null && doc.ElementCount > 0) {
                TriggerOn = doc["TriggerOn"].AsString;
                ChanceToTrigger = (double)doc["ChanceToTrigger"].AsInt32;
                script = new Script(doc["ScriptID"].AsString);

                foreach (var overrides in doc["Overrides"].AsBsonArray) {
                    MessageOverrideAsString.Add(overrides.AsString);
                }
            }
        }

        public void HandleEvent(object o, EventArgs e) {
            ThreadPool.QueueUserWorkItem(delegate { script.RunScript(); });
        }
    }
}
