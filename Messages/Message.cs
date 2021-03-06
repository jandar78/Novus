﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages {
    public enum ObjectType { Player, Npc, Room, Item, None }

    //This interface also exists in the Interfaces to prevent a circular dependency.
    //It's either duplicate code or putting everything into one huge project.
    public interface IMessage {
        string Id { get; set; }
        string[] _messages { get; set; }

        string Self { get; set; }

        string Target { get; set; }
        string Room { get; set; }
        string InstigatorID { get; set; }
        string TargetID { get; set; }
        ObjectType InstigatorType { get; set; }
        ObjectType TargetType { get; set; }
    }

    public class Message : IMessage {
		public string[] _messages { get; set; }

        public string Id { get; set; }

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
			_messages = new string[] { string.Empty, string.Empty, string.Empty };
		}

		/// <summary>
		/// Hold the messages with easy to get properties rather than using an index on a list.  
		/// Messages must have three elements, make them an empty string if necessary.
		/// </summary>
		/// <param name="messages"></param>
		public Message(List<string> messages, string instigatorID = null, ObjectType instigatorType = ObjectType.None, string targetid = null, ObjectType targetType = ObjectType.None) {
			_messages = new string[3];
			Self = string.IsNullOrEmpty(messages[0]) ? string.Empty : messages[0];
			Target = string.IsNullOrEmpty(messages[1]) ? string.Empty : messages[1];
			Room = string.IsNullOrEmpty(messages[2]) ? string.Empty : messages[2];

			InstigatorID = instigatorID;
			InstigatorType = instigatorType;

			TargetID = targetid;
			TargetType = targetType;
		}
	}
}

