using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Triggers;
using ClientHandling;

namespace AI {
    public class MessageParser {

        private Message MessageFull { get; set; }
        private Character.Iactor Actor { get; set; }

        private List<ITrigger> Triggers { get; set; }
        public ITrigger TriggerToExecute { get; set; }

        public MessageParser() { }

        public MessageParser(Message message, Character.Iactor actor, List<ITrigger> triggers) {
            MessageFull = message;
            Actor = actor;
            Triggers = triggers;
        }

        public void FindTrigger() {
            //iterate through the triggers and see if we get a match
            foreach (ITrigger trigger in Triggers) {
                if (trigger.TriggerOn != null && (MessageFull.Self != null && MessageFull.Self.Contains(trigger.TriggerOn) || (MessageFull.Room != null && MessageFull.Room.Contains(trigger.TriggerOn)))) {
                    //check chance to trigger
                    if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 100) <= trigger.ChanceToTrigger) {
                        TriggerToExecute = trigger;
                    }
                    break;
                }
            }
        }
    }
}
