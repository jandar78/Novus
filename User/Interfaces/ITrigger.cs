using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace Interfaces {
    public interface ITrigger {
        List<string> TriggerOn { get; set; }
		List<string> And { get; set; }
		List<string> NotOn { get; set; }
		double ChanceToTrigger { get; set; }
        List<string> MessageOverrides { get; set; }
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
