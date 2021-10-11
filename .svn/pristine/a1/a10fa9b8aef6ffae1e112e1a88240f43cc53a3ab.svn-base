using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace AI {
    public class MessageParser {

        private string MessageFull { get; set; }
        private Character.Iactor Actor { get; set; }  

        private List<Trigger> Triggers { get; set; }
        public Trigger TriggerToExecute { get; set; }

        public MessageParser(){}

        public MessageParser(string message, Character.Iactor actor, List<Trigger> triggers) {
            MessageFull = message;
            Actor = actor;
            Triggers = triggers;
        }         

        public void FindTrigger() {
            string[] tokenized = MessageFull.Split(' ');
            
            //iterate through the triggers and see if we get a match
            foreach(Trigger trigger in Triggers){
                if (MessageFull.Contains(trigger.TriggerOn)) {
                    //check chance to trigger
                    if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 100) <= trigger.ChanceToTrigger) {
                        TriggerToExecute = trigger;
                    }
                    break;
                }
            }
        }
    }

    public class Trigger {
        public string TriggerOn { get; set; }
        public double ChanceToTrigger { get; set; }
        BsonArray MessageOverrides { get; set; }
        public string StateToExecute { get; set; }
        public List<string> MessageOverrideAsString {get; set;}
       
        public Trigger() {
            MessageOverrideAsString = new List<string>();
        }

    }
}
