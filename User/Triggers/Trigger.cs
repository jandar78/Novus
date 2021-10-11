using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using LuaInterface;
using Interfaces;
using Sockets;
using Rooms;

//Trigger can be used to kick off scripts for pretty much anything, ranging from Quests to a special action an item can perform
//based on what the trigger is

namespace Triggers
{
    public class GeneralTrigger : ITrigger
    {
        //public GeneralTrigger(BsonDocument doc, TriggerType triggerType)
        //{
        //    TriggerOn = new List<string>();
        //    And = new List<string>();
        //    NotOn = new List<string>();
        //    MessageOverrides = new List<string>();
        //    if (doc != null && doc.ElementCount > 0 && doc.Contains("TriggerOn"))
        //    {
        //        foreach (var on in doc["TriggerOn"].AsBsonArray)
        //        {
        //            TriggerOn.Add(on.AsString);
        //        }
        //        foreach (var and in doc["And"].AsBsonArray)
        //        {
        //            And.Add(and.AsString);
        //        }
        //        foreach (var not in doc["NotOn"].AsBsonArray)
        //        {
        //            NotOn.Add(not.AsString);
        //        }
        //        AutoProcess = doc.Contains("AutoProcess") ? doc["AutoProcess"].AsBoolean : false;
        //        ChanceToTrigger = doc["ChanceToTrigger"].AsInt32;

        //        script = TriggerScriptFactory.GetScript(doc["ScriptID"].AsString, triggerType.ToString());

        //        foreach (var overrides in doc["MessageOverrides"].AsBsonArray)
        //        {
        //            MessageOverrides.Add(overrides.AsString);
        //        }
        //        Type = doc["Type"].AsString;
        //    }
        //}

        public GeneralTrigger()
        {
            TriggerOn = new List<string>();
            And = new List<string>();
            NotOn = new List<string>();
            MessageOverrides = new List<string>();
        }

        public string TriggerId { get; set; }
        public List<string> TriggerOn { get; set; }
        public List<string> And { get; set; }
        public List<string> NotOn { get; set; }
        public double ChanceToTrigger { get; set; }
        public List<string> MessageOverrides { get; set; }
        public string StateToExecute { get; set; }
        public string Type { get; set; }
        public bool AutoProcess { get; set; }
        public string ScriptID { get; set; }
        public string ScriptType { get; set; }
        public IScript script;

        public async virtual void HandleEvent(object o, EventArgs e)
        {
            IMessage message = new Message();
            var typeEventCaller = ((TriggerEventArgs)e).IdType;
            var callerID = ((TriggerEventArgs)e).Id;
            object caller = null;

            switch (typeEventCaller)
            {
                case TriggerEventArgs.IDType.Npc:
                    caller = Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { callerID });
                    break;
                case TriggerEventArgs.IDType.Room:
                    caller = Room.GetRoom(callerID.ToString());
                    break;
                default:
                    break;
            }

            if (script == null)
            {
                script = TriggerScriptFactory.GetScript(ScriptID, ScriptType);
            }

            if (MessageOverrides.Count > 0)
            {
                script.AddVariable(MessageOverrides, "messageOverrides");
            }

            script.AddVariable(message, "Message");

            if (caller is IRoom)
            {
                script.AddVariable((IRoom)caller, "room");
                script.AddVariable("room", "callerType");
            }
            else if (caller is User)
            {
                script.AddVariable((User)caller, "npc");
                script.AddVariable("npc", "callerType");
            }

            if (((TriggerEventArgs)e).InstigatorType == TriggerEventArgs.IDType.Player)
            {
                script.AddVariable(Server.GetAUser(((TriggerEventArgs)e).InstigatorID), "player");
            }
            else if (((TriggerEventArgs)e).InstigatorType == TriggerEventArgs.IDType.Npc)
            {
                script.AddVariable(Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { ((TriggerEventArgs)e).InstigatorID }), "player");
            }

            await Task.Run(() => script.RunScript());
        }

        public virtual void HandleEvent()
        {
            HandleEvent(null, null);
        }
    }


    public class QuestTrigger : GeneralTrigger
    {
        public QuestTrigger() { }

        public async override void HandleEvent(object o, EventArgs e)
        {
            //for items we want to add the item and the owner into the script as variables
            IMessage message = new Message();
            var typeEventCaller = ((TriggerEventArgs)e).IdType;
            var callerID = ((TriggerEventArgs)e).Id;
            object caller = null;

            switch (typeEventCaller)
            {
                case TriggerEventArgs.IDType.Npc:
                    caller = Character.NPCUtils.GetAnNPCByID(callerID);
                    break;
                case TriggerEventArgs.IDType.Room:
                    caller = Room.GetRoom(callerID.ToString());
                    break;
                default:
                    break;
            }

            if (MessageOverrides.Count > 0)
            {
                script.AddVariable(MessageOverrides, "messageOverrides");
            }

            script.AddVariable(message, "Message");

            if (caller is IRoom)
            {
                script.AddVariable((IRoom)caller, "room");
            }
            else if (caller is IActor)
            {
                script.AddVariable(Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { ((IActor)caller).Id }), "npc");
            }

            //add the player (instigator) to the script
            if (((TriggerEventArgs)e).InstigatorType == TriggerEventArgs.IDType.Player)
            {
                script.AddVariable(Server.GetAUser(((TriggerEventArgs)e).InstigatorID), "player");
            }
            else if (((TriggerEventArgs)e).InstigatorType == TriggerEventArgs.IDType.Npc)
            {
                script.AddVariable(Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId>() { ((TriggerEventArgs)e).InstigatorID }), "player");
            }

            //add a message if there is one
            if (!string.IsNullOrEmpty(((TriggerEventArgs)e).Message))
            {
                script.AddVariable(((TriggerEventArgs)e).Message, "messages");
            }

            //trying this instead of the ThreadPool
            await Task.Run(() => script.RunScript());
        }
    }

    public class ItemTrigger : GeneralTrigger
    {

        public ItemTrigger() { }

        //public ItemTrigger(BsonDocument doc) : base(doc, TriggerType.Items) { }

        public async override void HandleEvent(object o, EventArgs e)
        {
            //for items we want to add the item and the owner into the script as variables
            var item = await Items.Items.GetByID(((ItemEventArgs)e).ItemID);
            if (item != null)
            {
                script.AddVariable(item, "item");

                if (MessageOverrides.Count > 0)
                {
                    script.AddVariable(MessageOverrides, "messageOverrides");
                }

                IUser player = Server.GetAUser(item.Owner);
                if (player != null)
                {//the owner could be another item and not a player
                    if (script.ScriptType == ScriptTypes.Lua)
                    {
                        script.AddVariable(player.Player, "player");
                    }
                    else
                    {
                        script.AddVariable(player.Player.Id, "playerID");
                    }
                }

                await Task.Run(() => script.RunScript());
            }
        }
    }
}
