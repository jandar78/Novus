using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using MongoDB.Bson;

namespace AI {
    public class FSM {
        private Dictionary<string, IState> cachedStates;
        private static FSM _fsm = null;

        public IState state {
            get;
            set;
        }

        public IState globalState {
            get;
            set;
        }

        public IState previousState { //for blip states mostly
            get;
             set;
        }

        private FSM() { 
            cachedStates = new Dictionary<string, IState>();
            CacheStates();
        }

        public static FSM GetInstance() {
            return _fsm ?? (_fsm = new FSM());
        }

        private void CacheStates() {
            var totalerTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IState).IsAssignableFrom(t) && t.IsClass == true);
            foreach (Type totalerType in totalerTypes) {
                cachedStates.Add(totalerType.Name, (IState)totalerType.GetMethod("GetState").Invoke(totalerType, null)); 
            }    
        }

        public static bool ContinueWithThisState() {
            if (Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 6) == 2 || Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 6) == 5 || Extensions.RandomNumber.GetRandomNumber().NextNumber(0, 6) == 0) {//if this hits then we are going to stop walking and do something else like say something
                return false;
            }
            return true;
        }


        public IState GetStateFromName(string stateName) {
            if (cachedStates.ContainsKey(stateName)){
                return cachedStates[stateName];
            }

            return null;
        }

        public void ChangeState(IState newState, Character.NPC Actor) {
            if (state != null && newState != null) {
                state.Exit(Actor);
                previousState = state;
                state = newState;
                state.Enter(Actor);
                Actor.Save();
            }
        }

        public void RevertState() {
            IState temp = state;
            if (previousState != null) {
                state = previousState;
            }
            else {
                state = Wander.GetState(); //if no previous state we'll just default to wandering around for now
            }

            previousState = temp;
            temp = null;
        }

        public void Update(Character.NPC Actor) {
            if (state != null) {
                state.Execute(Actor);
            }
            if (globalState != null) {
                globalState.Execute(Actor);
            }
        }

        public void InterpretMessage(string message, Character.Iactor actor) {
            Character.NPC npc = actor as Character.NPC;
            MessageParser parser = new MessageParser(message, actor, npc.Triggers);
            
            parser.FindTrigger();
            
            //Here's the rub. This call is a sequential call up to this point, maybe we want to
            //kick off a separate thread so that then it won't hold up the players actions and
            //then we can even execute states that operate on a delay.

            if (parser.TriggerToExecute != null) {
                IState state = GetStateFromName(parser.TriggerToExecute.StateToExecute);
                ChangeState(state, npc);
                npc.NextAiAction = DateTime.Now.ToUniversalTime().AddSeconds(-1); //this state will execute next now
                state.Execute(npc, parser.TriggerToExecute);
            }
        }
    }

   
}

