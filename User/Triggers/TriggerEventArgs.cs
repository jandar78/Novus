using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triggers {
    public class TriggerEventArgs: EventArgs {
        public ObjectId Id {
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

		public ObjectId InstigatorID {
			get;
			set;
		}

		public IDType InstigatorType { get; set; }

        public TriggerEventArgs(ObjectId id, IDType type, ObjectId instigatorID, IDType instigatorType, string message = null) {
            IdType = type;
            Id = id;
            Message = message;
			InstigatorID = instigatorID; //this will always be a player, we are not going to give a quest to an NPC.
			InstigatorType = instigatorType;
        }

        public enum IDType { None, Npc, Room, Player };
    }
}
