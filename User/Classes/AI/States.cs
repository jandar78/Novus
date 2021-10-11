using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI.PathFinding;
using Interfaces;
using Extensions;
using MongoDB.Bson;
using Rooms;

namespace AI
{
    public class Greet : IState
    {

        private static Greet _greet;
        private Greet() { }
        public static Greet GetState()
        {
            return _greet ?? (_greet = new Greet());
        }
        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(0, 2)).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Greet";
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            NPC npc = actor as NPC;

            if (npc.StanceState != CharacterStanceState.Laying_unconcious &&
                npc.StanceState != CharacterStanceState.Laying_dead &&
                npc.StanceState != CharacterStanceState.Decomposing)
            {
                if (DateTime.Now.ToUniversalTime() > ((NPC)actor).NextAiAction)
                {
                    string message = "Hey pal, you looking for trouble?";
                    if (trigger.MessageOverrides.Count > 0)
                    {
                        message = trigger.MessageOverrides[RandomNumber.GetRandomNumber().NextNumber(0, trigger.MessageOverrides.Count)];
                    }
                    Commands.CommandParser.ExecuteCommand(actor, "say", message);
                }
            }
            //either way we are not staying in this state, it's just a blip state
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime(); //set when we want this action to execute next
            ((NPC)actor).Fsm.RevertState();
            actor.Save();
        }
    }

    public class Questing : IState
    {

        private static Questing _questing;
        private Questing() { }
        public static Questing GetState()
        {
            return _questing ?? (_questing = new Questing());
        }
        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(1, 5)).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Questing";
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            var npc = actor as NPC;
            if (npc.StanceState != CharacterStanceState.Laying_unconcious &&
                npc.StanceState != CharacterStanceState.Laying_dead &&
                npc.StanceState != CharacterStanceState.Decomposing)
            {
                if (DateTime.Now.ToUniversalTime() > ((NPC)actor).NextAiAction)
                {//so it's time for this AI state to execute
                 //get the questID and step that we want to process		
                    npc.Quests.Where(q => q.AutoProcessNextStep == true).SingleOrDefault().AutoProcessQuestStep(actor as IActor);

                }
            }
            //either way we are not staying in this state, it's just a blip state
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(1, 5)).ToUniversalTime(); //set when we want this action to execute next
            ((NPC)actor).Fsm.RevertState();
            actor.Save();
        }
    }

    public class WalkTo : IState
    {

        private static WalkTo _walkTo;
        private WalkTo() { }
        public static WalkTo GetState()
        {
            return _walkTo ?? (_walkTo = new WalkTo());
        }
        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(0, 2)).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "WalkTo";
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            var npc = actor as NPC;
            if (npc.StanceState != CharacterStanceState.Laying_unconcious &&
                npc.StanceState != CharacterStanceState.Laying_dead &&
                npc.StanceState != CharacterStanceState.Decomposing)
            {
                if (DateTime.Now.ToUniversalTime() > ((NPC)actor).NextAiAction)
                {
                    //it's time for this AI state to execute
                    //will move the NPC towards a specific room, this should be used when doing AI pathfinding and we have found a path
                    //it will need an end location since the start is the NPC current location.
                    string message = "";
                    //Get NPC path Queue
                    //Peek next room ID to walk to
                    //Find that roomID from the available exits
                    //Walk in that direction -new State
                    //if path is blocked - perform some action (unlock, open, emote)
                    //Find new path or wait until another trigger causes a new state change

                    if (trigger.MessageOverrides.Count > 0)
                    {
                        message = trigger.MessageOverrides[RandomNumber.GetRandomNumber().NextNumber(0, trigger.MessageOverrides.Count)];
                    }

                    Commands.CommandParser.ExecuteCommand(actor, "directionToGo", message);
                }
            }
            //either way we are not staying in this state, it's just a blip state
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime(); //set when we want this action to execute next
            ((NPC)actor).Fsm.RevertState();
            actor.Save();
        }
    }

    public class FindPath
    {
        private TreeTraverser _tree;
        public string NextRoomInPath { get; set; }

        public FindPath(string startPoint, string endPoint)
        {
            IRoom room = Room.GetRoom(startPoint);
            IRoom endRoom = Room.GetRoom(endPoint);
            TreeNode rootNode = new TreeNode(room);
            rootNode.Parent = rootNode;
            _tree = new TreeTraverser(rootNode, endRoom.Id, room.Zone == endRoom.Zone);

            var result = GetPath();

            NextRoomInPath = result.Result[0];
        }

        private async Task<List<string>> GetPath()
        {
            return await _tree.TraverseTree();//should be the path to our endPoint.....I hope.		
        }
    }


    public class Wander : IState
    {
        private static Wander _wander;
        private Wander() { }

        public static Wander GetState()
        {
            return _wander ?? (_wander = new Wander());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            if (!actor.IsDead() && !actor.InCombat)
            {
                IRoom room = Room.GetRoom(actor.Location);
                room.GetRoomExits();
                var AvailableExits = room.RoomExits;
                if (DateTime.Now.ToUniversalTime() > ((NPC)actor).NextAiAction)
                {//so it's time for this AI state to execute
                    Commands.CommandParser.ExecuteCommand(actor, AvailableExits[RandomNumber.GetRandomNumber().NextNumber(0, AvailableExits.Count)].Direction);
                    ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime(); //set when we want this action to execute next
                    if (!FSM.ContinueWithThisState())
                    {
                        ((NPC)actor).Fsm.ChangeState(Speak.GetState(), actor);
                    }
                }
            }

            if (actor.InCombat)
            {
                ((NPC)actor).Fsm.ChangeState(Combat.GetState(), actor);
            }
        }

        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Wander";
        }

    }

    public class Stay : IState
    {
        private static Stay _stay;
        private Stay() { }

        public static Stay GetState()
        {
            return _stay ?? (_stay = new Stay());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            if (!actor.IsDead() && !actor.InCombat)
            {
                //Will stay in place until forced into combat or a script puts it in another state
                ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(5).ToUniversalTime();
            }

            if (actor.InCombat)
            {
                ((NPC)actor).Fsm.ChangeState(Combat.GetState(), actor);
            }
        }

        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(5).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Stay";
        }

    }

    public class Speak : IState
    {
        private static Speak _speak;
        private Speak() { }

        public static Speak GetState()
        {
            return _speak ?? (_speak = new Speak());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            if (!actor.IsDead() && !actor.InCombat)
            {
                if (DateTime.Now.ToUniversalTime() > ((NPC)actor).NextAiAction)
                {
                    //eventually these literals will be retrieved from the literals table for each different NPC
                    Commands.CommandParser.ExecuteCommand(actor, "SAY", "brains...");
                    Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "reaches out attempting to grab something");
                    ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(15, 60)).ToUniversalTime();
                    if (!FSM.ContinueWithThisState())
                    {
                        ((NPC)actor).Fsm.ChangeState(Wander.GetState(), actor);
                    }
                }
            }
        }

        public void Enter(IActor actor)
        {
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(RandomNumber.GetRandomNumber().NextNumber(15, 60)).ToUniversalTime();
        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Speak";
        }
    }

    public class Combat : IState
    {
        private static Combat _combat;
        private Combat() { }

        public static Combat GetState()
        {
            return _combat ?? (_combat = new Combat());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            //no target then switch to finding a target first
            if (actor.CurrentTarget == null)
            {
                ((NPC)actor).Fsm.ChangeState(FindTarget.GetState(), actor);
            }
            else
            {//ok we have someone we can kill, let's do that
                //if this type of NPC may alert other NPC of the same type in the room to also join in on the fun
                /*
                if (actor.AlertOthers){
                    NPCUtils.AlertOtherMobs(actor.Location, actor.MobType, actor.CurrentTarget);
                }*/
                Commands.CommandParser.ExecuteCommand(actor, "KILL", "target");
            }

        }

        public void Enter(IActor actor)
        {
            //no target, no fighting

        }

        public void Exit(IActor actor)
        {
        }

        public override string ToString()
        {
            return "Combat";
        }
    }

    public class FindTarget : IState
    {
        private static FindTarget _findTarget;
        private FindTarget() { }

        public static FindTarget GetState()
        {
            return _findTarget ?? (_findTarget = new FindTarget());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            //first let's check to see if we got any messages telling us we are being attacked and use that
            //person attacking us as the target
            //if that gets us nowhere, we need to then just kill the first non npc we find in our same location
            IRoom room = Room.GetRoom(actor.Location);
            var playersAtThisLocation = room.GetObjectsInRoom(RoomObjects.Players);

            double minutesSinceLastCombat = (DateTime.Now.ToUniversalTime() - actor.LastCombatTime).TotalMinutes;
            //let's start by seeing if we had a last target and the last combat time has been less than 5 minutes ago, if so and he's here, it's payback time
            if (actor.LastTarget != null && minutesSinceLastCombat < 5)
            {
                playersAtThisLocation.AddRange(room.GetObjectsInRoom(RoomObjects.Npcs)); //we may have been attacking an npc so let's add them in
                if (playersAtThisLocation.Contains(actor.LastTarget))
                { //yeah our previous target is here
                    actor.CurrentTarget = actor.LastTarget;
                }
            }

            //if we don't have a target and we have never been in combat yet, then we are going to find a target OR
            //we've lost interest in our previous target but still have a blooddlust for another 10 minutes
            //so a random person is going to get attacked unless it's been too long since we last attacked someone
            //at this point we should only have actual players in the list to attack (maybe add something so some NPCs can attack other NPCs)
            if ((actor.LastCombatTime == DateTime.MinValue.ToUniversalTime() && actor.CurrentTarget == null) ||
                (actor.CurrentTarget == null && minutesSinceLastCombat >= 5 && minutesSinceLastCombat < 15))
            {
                if (playersAtThisLocation.Count > 0)
                {
                    actor.CurrentTarget = playersAtThisLocation[RandomNumber.GetRandomNumber().NextNumber(0, playersAtThisLocation.Count)];
                }
            }

            //we have a target let's attack
            if (actor.CurrentTarget != null)
            {
                if (playersAtThisLocation.Contains(actor.CurrentTarget))
                {
                    Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "growls menancingly at " + Sockets.Server.GetAUser(actor.CurrentTarget).Player.FirstName.CamelCaseWord() + "!");
                    ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(10).ToUniversalTime(); //give player time to react, maybe even get the first hit
                    ((NPC)actor).Fsm.ChangeState(Combat.GetState(), actor);
                }
            }
            else
            {
                //no targets in sight let's enter hunt mode until things cool down
                ((NPC)actor).Fsm.ChangeState(Hunt.GetState(), actor);
            }
        }

        public void Enter(IActor actor)
        {
            Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "starts looking around for something to attack");
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(30).ToUniversalTime(); //this way players will have some time to react and/or run away
        }

        public void Exit(IActor actor) { }

        public override string ToString()
        {
            return "FindTarget";
        }
    }

    public class Hunt : IState
    {
        private static Hunt _hunt;

        private Hunt() { }

        public static Hunt GetState()
        {
            return _hunt ?? (_hunt = new Hunt());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            Wander.GetState().Execute(actor); //let's go to another room
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(-2).ToUniversalTime(); //set next action back so we will immediately start searching for a target
            FindTarget.GetState().Execute(actor);//let's look for a target
            ((NPC)actor).NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(10, 31)).ToUniversalTime(); //we are actively looking so the wait time is not long to linger about
        }

        public void Enter(IActor actor)
        {
            //ok we were recently in combat and we are in hunt mode otherwise we passed the cool down period and will go back to wandering around
            if (actor.LastCombatTime != DateTime.MinValue.ToUniversalTime() && (DateTime.Now.ToUniversalTime() - actor.LastCombatTime).TotalMinutes >= 10)
            {
                ((NPC)actor).Fsm.ChangeState(Wander.GetState(), actor);
            }
        }

        public void Exit(IActor actor) { }

        public override string ToString()
        {
            return "Hunt";
        }
    }

    public class Rot : IState
    {
        private static Rot _rot;

        private Rot() { }

        public static Rot GetState()
        {
            return _rot ?? (_rot = new Rot());
        }

        public void Execute(IActor actor, ITrigger trigger = null)
        {
            if (actor.StanceState != CharacterStanceState.Decomposing)
            {
                Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "carcass last bit of flesh has rotted away from its dead corpse.");
                actor.Description = "The only remains of " + actor.FirstName + " are just bones.";
                actor.SetStanceState(CharacterStanceState.Decomposing);
                ((NPC)actor).NextAiAction = DateTime.Now.AddMinutes(5).ToUniversalTime();
            }
            else
            {
                Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "carcass has its bones break down to dust and carried off by the wind.");
                var mobCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "NPCs");
                var doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(mobCollection, n => n["_id"] == ((NPC)actor).MobTypeID);

                doc["Current"] = doc["Current"].AsInt32 - 1;
                actor.Save();

                var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
                var deleteResult = npcCollection.DeleteOne(new BsonDocument("_id", actor.Id));
            }
        }

        public void Enter(IActor actor)
        {
            //ok when we enter this state we will first set the description to the NPC is sitting here rotting, 
            //then decomposing and finally get rid of him on exit
            ((NPC)actor).Description = "The recently dead carcass of " + actor.FirstName + " is rotting as maggots feast on its entrails.";
            ((NPC)actor).NextAiAction = DateTime.Now.AddMinutes(10).ToUniversalTime();
            actor.Save();
        }

        public void Exit(IActor actor) { }

        public override string ToString()
        {
            return "Rot";
        }
    }
}


