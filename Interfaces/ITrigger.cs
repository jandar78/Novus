using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Interfaces {
    public interface ITrigger {
        List<string> TriggerOn { get; set; }
		List<string> AndOn { get; set; }
		List<string> NotOn { get; set; }
		double ChanceToTrigger { get; set; }
        BsonArray MessageOverrides { get; set; }
        List<string> MessageOverrideAsString { get; set; }
        string StateToExecute { get; set; }
		string Type { get; set; }
		bool AutoProcess { get; set; }

        void HandleEvent(object o, EventArgs e);
        void HandleEvent();
    }

    public class Trigger : ITrigger
    {
       public List<string> TriggerOn { get; set; }
       public List<string> AndOn { get; set; }
       public List<string> NotOn { get; set; }
       public double ChanceToTrigger { get; set; }
       public BsonArray MessageOverrides { get; set; }
       public List<string> MessageOverrideAsString { get; set; }
       public string StateToExecute { get; set; }
       public string Type { get; set; }
       public bool AutoProcess { get; set; }

        public void HandleEvent(object o, EventArgs e) { }
        public void HandleEvent() { }
    }
}
