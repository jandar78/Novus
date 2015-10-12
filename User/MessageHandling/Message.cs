using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientHandling {
	public class Message {
		private string[] _messages;

		public string Self {
			get {
				return _messages[0];
			}
			set {
				_messages[0] = value;
			}
		}

		public string Target {
			get {
				return _messages[1];
			}
			set {
				_messages[1] = value;
			}
		}

		public string Room {
			get {
				return _messages[2];
			}
			set {
				_messages[2] = value;
			}
		}

		//The ID of the actor/room/item that caused the message to happen
		public string InstigatorID {
			get;
			set;
		}

		public string TargetID {
			get;
			set;
		}

		public ObjectType InstigatorType {
			get;
			set;
		}

		public ObjectType TargetType {
			get;
			set;
		}


		public Message() {
			_messages = new string[3];
		}

		/// <summary>
		/// Hold the messages with easy to get properties rather than using an index on a list.  
		/// Messages must have three elements, make them an empty string if necessary.
		/// </summary>
		/// <param name="messages"></param>
		public Message(List<string> messages, string instigatorID = null, ObjectType instigatorType = ObjectType.None, string targetid = null, ObjectType targetType = ObjectType.None) {
			_messages = new string[3];
			Self = string.IsNullOrEmpty(messages[0]) ? null : messages[0];
			Target = string.IsNullOrEmpty(messages[1]) ? null : messages[1];
			Room = string.IsNullOrEmpty(messages[2]) ? null : messages[2];

			InstigatorID = instigatorID;
			InstigatorType = instigatorType;

			TargetID = targetid;
			TargetType = targetType;
		}

		public enum ObjectType {
			Player, Npc, Room, Item, None
		}
	}
}

