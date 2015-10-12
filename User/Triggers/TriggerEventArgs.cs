using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triggers {
    public class TriggerEventArgs: EventArgs {
        public string Id {
            get; private set;
        }

        public IDType IdType {
            get;
            private set;
        }

        public string Message {
            get;
            private set;
        }

		public string InstigatorID {
			get;
			set;
		}

		public IDType InstigatorType { get; set; }

        public TriggerEventArgs(string id, IDType type, string instigatorID, IDType instigatorType, string message = null) {
            IdType = type;
            Id = id;
            Message = message;
			InstigatorID = instigatorID; //this will always be a player, we are not going to give a quest to an NPC.
			InstigatorType = instigatorType;
        }

        public enum IDType { Npc, Room, None };
    }
}
