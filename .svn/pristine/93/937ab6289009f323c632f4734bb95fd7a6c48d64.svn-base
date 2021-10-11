using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;

namespace AI {
    public interface IState {
         void Execute(Character.NPC actor, Trigger trigger = null);
         void Enter(Character.NPC actor);
         void Exit(Character.NPC actor);
         string ToString();
    }

    public class Greet : IState {
         
        private static Greet _greet;
        private Greet() { }
        public static Greet GetState() {
            return _greet ?? (_greet = new Greet());
        }
        public void Enter(Character.NPC actor) {
            actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 2)).ToUniversalTime();
        }

        public void Exit(Character.NPC actor) {
        }

        public override string ToString() {
            return "Greet";
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            if (actor.StanceState != CharacterEnums.CharacterStanceState.LAYING_UNCONCIOUS &&
                actor.StanceState != CharacterEnums.CharacterStanceState.LAYING_DEAD &&
                actor.StanceState != CharacterEnums.CharacterStanceState.DECOMPOSING) {
                if (DateTime.Now.ToUniversalTime() > actor.NextAiAction) {//so it's time for this AI state to execute
                    string message = "Hey pal, you looking for trouble?";
                    if (trigger.MessageOverrideAsString.Count > 0) {
                        message = trigger.MessageOverrideAsString[RandomNumber.GetRandomNumber().NextNumber(0, trigger.MessageOverrideAsString.Count)];
                    }
                    Commands.CommandParser.ExecuteCommand(actor, "say", message);
                }
            }
            //either way we are not staying in this state, it's just a blip state
            actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime(); //set when we want this action to execute next
            actor.Fsm.RevertState();
            actor.Save();
        }
    }

    public class Wander : IState {
        private static Wander _wander;
        private Wander() { }

        public static Wander GetState() {
            return _wander ?? (_wander = new Wander());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            if (!actor.IsDead() && !actor.InCombat) {
                Rooms.Room room = Rooms.Room.GetRoom(actor.Location);
                room.GetRoomExits();
                List<Rooms.Exits> availableExits = room.RoomExits;
                if (DateTime.Now.ToUniversalTime() > actor.NextAiAction) {//so it's time for this AI state to execute
                    Commands.CommandParser.ExecuteCommand(actor, availableExits[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, availableExits.Count)].Direction);
                    actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime(); //set when we want this action to execute next
                    if (!FSM.ContinueWithThisState()) {
                        actor.Fsm.ChangeState(Speak.GetState(), actor);
                    }
                }
            }

            if (actor.InCombat) {
                actor.Fsm.ChangeState(Combat.GetState(), actor);
            }
        }

        public void Enter(Character.NPC actor) {
            actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(60, 121)).ToUniversalTime();
        }

        public void Exit(Character.NPC actor) {
        }

        public override string ToString() {
            return "Wander";
        }

    }

    public class Speak : IState {
        private static Speak _speak;
        private Speak() { }

        public static Speak GetState() {
            return _speak ?? (_speak = new Speak());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            if (!actor.IsDead() && !actor.InCombat) {
                if (DateTime.Now.ToUniversalTime() > actor.NextAiAction) {
                    //eventuall this literals will be gotten from the literals table for each different NPC
                    Commands.CommandParser.ExecuteCommand(actor, "SAY", "brains...");
                    Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "reaches out attempting to grab something");
                    actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(15, 60)).ToUniversalTime();
                    if (!FSM.ContinueWithThisState()) {
                        actor.Fsm.ChangeState(Wander.GetState(), actor);
                    }
                }
            }
        }

        public void Enter(Character.NPC actor) {
            actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(15, 60)).ToUniversalTime();
        }

        public void Exit(Character.NPC actor) {
        }

        public override string ToString() {
            return "Speak";
        }
    }

    public class Combat : IState {
        private static Combat _combat;
        private Combat() { }

        public static Combat GetState() {
            return _combat ?? (_combat = new Combat());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            //no target then switch to finding a target first
            if (actor.CurrentTarget == null) {
                actor.Fsm.ChangeState(FindTarget.GetState(), actor);
            }
            else {//ok we have someone we can kill, let's do that
                //if this type of NPC may alert other NPC of the same type in the room to also join in on the fun
                /*
                if (actor.AlertOthers){
                    NPCUtils.AlertOtherMobs(actor.Location, actor.MobType, actor.CurrentTarget);
                }*/
                Commands.CommandParser.ExecuteCommand(actor, "KILL", "target");
            }

        }

        public void Enter(Character.NPC actor) {
            //no target, no fighting
            
        }

        public void Exit(Character.NPC actor) {
        }

        public override string ToString() {
            return "Combat";
        }
    }

    public class FindTarget : IState {
        private static FindTarget _findTarget;
        private FindTarget() { }

        public static FindTarget GetState() {
            return _findTarget ?? (_findTarget = new FindTarget());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            //first let's check to see if we got any messages telling us we are being attacked and use that
            //person attacking us as the target
            //if that gets us nowhere, we need to then just kill the first non npc we find in our same location
            Rooms.Room room = Rooms.Room.GetRoom(actor.Location);
            List<string> playersAtThisLocation = room.GetObjectsInRoom("PLAYERS");

            double minutesSinceLastCombat = (DateTime.Now.ToUniversalTime() - actor.LastCombatTime).TotalMinutes;
            //let's start by seeing if we had a last target and the last combat time has been less than 5 minutes ago, if so and he's here, it's payback time
            if (actor.LastTarget != null && minutesSinceLastCombat < 5) {
                playersAtThisLocation.AddRange(room.GetObjectsInRoom("NPCS")); //we may have been attacking an npc so let's add them in
                if (playersAtThisLocation.Contains(actor.LastTarget)) { //yeah our previous target is here
                    actor.CurrentTarget = actor.LastTarget;
                }
            }

            //if we don't have a target and we have never been in combat yet, then we are going to find a target OR
            //we've lost interest in our previous target but still have a blooddlust for another 10 minutes
            //so a random person is going to get attacked unless it's been too long since we last attacked someone
            //at this point we should only have actual players in the list to attack (maybe add something so some NPCs can attack other NPCs)
            if ((actor.LastCombatTime == DateTime.MinValue.ToUniversalTime() && actor.CurrentTarget == null) ||
                (actor.CurrentTarget == null && minutesSinceLastCombat >= 5 && minutesSinceLastCombat < 15)) {
                if (playersAtThisLocation.Count > 0) {
                    actor.CurrentTarget = playersAtThisLocation[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, playersAtThisLocation.Count)];
                }
            }

            //we have a target let's attack
            if (actor.CurrentTarget != null) {
                if (playersAtThisLocation.Contains(actor.CurrentTarget)) {
                    Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "growls menancingly at " + MySockets.Server.GetAUser(actor.CurrentTarget).Player.FirstName.CamelCaseWord() + "!");
                    actor.NextAiAction = DateTime.Now.AddSeconds(10).ToUniversalTime(); //give player time to react, maybe even get the first hit
                    actor.Fsm.ChangeState(Combat.GetState(), actor);
                }
            }
            else {
                //no targets in sight let's enter hunt mode until things cool down
                actor.Fsm.ChangeState(Hunt.GetState(), actor);
            }
        }

        public void Enter(Character.NPC actor) {
            Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "starts looking around for something to attack");
            actor.NextAiAction = DateTime.Now.AddSeconds(30).ToUniversalTime(); //this way players will have some time to react and/or run away
        }

        public void Exit(Character.NPC actor) { }

        public override string ToString() {
            return "FindTarget";
        }
    }

    public class Hunt : IState {
        private static Hunt _hunt;
        
        private Hunt() {}

        public static Hunt GetState(){
            return _hunt ?? (_hunt = new Hunt());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            Wander.GetState().Execute(actor); //let's go to another room
            actor.NextAiAction = DateTime.Now.AddSeconds(-2).ToUniversalTime(); //set next action back so we will immediately start searching for a target
            FindTarget.GetState().Execute(actor);//let's look for a target
            actor.NextAiAction = DateTime.Now.AddSeconds(Extensions.RandomNumber.GetRandomNumber().NextNumber(10, 31)).ToUniversalTime(); //we are actively looking so the wait time is not long to linger about
        }

        public void Enter(Character.NPC actor) {
            //ok we were recently in combat and we are in hunt mode otherwise we passed the cool down period and will go back to wandering around
            if (actor.LastCombatTime != DateTime.MinValue.ToUniversalTime() && (DateTime.Now.ToUniversalTime() - actor.LastCombatTime).TotalMinutes >= 10) {
                actor.Fsm.ChangeState(Wander.GetState(), actor);
            }
        }

        public void Exit(Character.NPC actor) { }

        public override string ToString() {
            return "Hunt";
        }
    }

    public class Rot : IState {
        private static Rot _rot;

        private Rot() { }

        public static Rot GetState() {
            return _rot ?? (_rot = new Rot());
        }

        public void Execute(Character.NPC actor, Trigger trigger = null) {
            if (actor.StanceState != CharacterEnums.CharacterStanceState.DECOMPOSING) {
                Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "carcass last bit of flesh has rotted away from its dead corpse.");
                actor.Description = "The only remains of " + actor.FirstName + " are just bones.";
                actor.SetStanceState(CharacterEnums.CharacterStanceState.DECOMPOSING);
                actor.NextAiAction = DateTime.Now.AddMinutes(5).ToUniversalTime();
            }
            else {
                Commands.CommandParser.ExecuteCommand(actor, "EMOTE", "carcass has its bones break down to dust and carried off by the wind.");
                MongoUtils.MongoData.ConnectToDatabase();
                MongoDB.Driver.MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
                MongoDB.Driver.MongoCollection npcs = db.GetCollection("NPCs");
                MongoDB.Driver.IMongoQuery query = MongoDB.Driver.Builders.Query.EQ("_id", actor.MobTypeID);

                MongoDB.Bson.BsonDocument doc = npcs.FindOneAs<MongoDB.Bson.BsonDocument>(query);

                doc["Current"] = doc["Current"].AsInt32 - 1;
                npcs.Save(doc);

                db = MongoUtils.MongoData.GetDatabase("Characters");
                npcs = db.GetCollection("NPCCharacters");
                query = MongoDB.Driver.Builders.Query.EQ("_id", MongoDB.Bson.ObjectId.Parse(actor.ID));

                npcs.Remove(query);
            }
        }

        public void Enter(Character.NPC actor) {
            //ok when we enter this state we will first set the description to the NPC is sitting here rotting, 
            //then decomposing and finally get rid of him on exit
            actor.Description = "The recently dead carcass of " + actor.FirstName + " is rotting as maggots feast on its entrails.";
            actor.NextAiAction = DateTime.Now.AddMinutes(10).ToUniversalTime();
            actor.Save();
        }

        public void Exit(Character.NPC actor) { }

        public override string ToString() {
            return "Rot";
        }
    }
}


