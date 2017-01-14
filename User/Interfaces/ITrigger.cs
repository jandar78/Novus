using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Interfaces {
    public interface ITrigger {
        List<string> TriggerOn { get; set; }
		List<string> And { get; set; }
		List<string> NotOn { get; set; }
		double ChanceToTrigger { get; set; }
        BsonArray MessageOverrides { get; set; }
        List<string> MessageOverrideAsString { get; set; }
        string StateToExecute { get; set; }
		string Type { get; set; }
		bool AutoProcess { get; set; }
        string TriggerId { get; set; }

        void HandleEvent(object o, EventArgs e);
        void HandleEvent();
    }

    public enum TriggerType {
        Items, Quests, Door, NPC, Room
    };
}
