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
		public List<ITrigger> TriggersToExecute { get; set; }

		public MessageParser() {
			TriggersToExecute = new List<ITrigger>();
		}

		public MessageParser(Message message, Character.Iactor actor, List<ITrigger> triggers) {
			MessageFull = message;
			Actor = actor;
			Triggers = triggers;
			TriggersToExecute = new List<ITrigger>();
		}

		public void FindTrigger() {
			//iterate through the triggers and see if we get a match
			bool hasOn = false;
			bool hasAnd = false;
			foreach (ITrigger trigger in Triggers) {
				if (trigger.AutoProcess) { //special case where we want to trigger a script but don't have triggers like the NPC saying the next line of dialogue or doing something
					TriggersToExecute.Add(trigger);
					continue;
				}

				string message = null;
				if (trigger.Type.Contains("Room")) {
					message = MessageFull.Room;
				}
				if (trigger.Type.Contains("Self")) {
					message = MessageFull.Self;
				}
				if (trigger.Type.Contains("Target")) {
					message = MessageFull.Target;
				}

				foreach (string on in trigger.TriggerOn) {
					if (message.Contains(on)) {
						hasOn = true;
						break;
					}
				}
				if (trigger.AndOn.Count > 0) {
					foreach (string and in trigger.AndOn) {
						if (message.Contains(and)) {
							hasAnd = true;
							break;
						}
					}
				}
				else {
					hasAnd = true;
				}
				foreach (string not in trigger.NotOn) {
					if (message.Contains(not)) {
						hasOn = false;
						break;
					}
				}


				if (hasOn && hasAnd) { //it triggered
					if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 100) <= trigger.ChanceToTrigger) {
						TriggersToExecute.Add(trigger);
					}
				}
			}
		}
	}
}
