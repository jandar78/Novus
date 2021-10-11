using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Triggers {
    public interface ITrigger {
        string TriggerOn { get; set; }
        double ChanceToTrigger { get; set; }
        BsonArray MessageOverrides { get; set; }
        List<string> MessageOverrideAsString { get; set; }
        string StateToExecute { get; set; }

        void HandleEvent(object o, EventArgs e);
    }
}
