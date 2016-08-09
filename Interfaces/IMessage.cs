﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public enum ObjectType { Player, Npc, Room, Item, None }

    public interface IMessage {
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
        public string Self { get; set; }
        public string Target { get; set; }
        public string Room { get; set; }
        public string InstigatorID { get; set; }
        public string TargetID { get; set; }
        public ObjectType InstigatorType { get; set; }
        public ObjectType TargetType { get; set; }

        public Message() { }

        public Message(List<string> messages, string instigatorID = null, ObjectType instigatorType = ObjectType.None, string targetid = null, ObjectType targetType = ObjectType.None) { }
    }
}
