using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;

namespace AI {
    public class FSM : IFsm {
        public Dictionary<string, IState> cachedStates { get; set; }
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

        public static IFsm GetInstance() {
            return _fsm ?? (_fsm = new FSM());
        }

        public void CacheStates() {
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
            if (stateName != null && cachedStates.ContainsKey(stateName)){
                return cachedStates[stateName];
            }

            return null;
        }

        public void ChangeState(IState newState, IActor Actor) {
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

        public void Update(IActor Actor) {
            if (state != null) {
                state.Execute(Actor);
            }
            if (globalState != null) {
                globalState.Execute(Actor);
            }
        }

        public void InterpretMessage(IMessage message, IActor actor) {
            NPC npc = actor as NPC;
			List<ITrigger> triggers = new List<ITrigger>();
			npc.Triggers.ForEach((t) => triggers.Add(t));

            MessageParser parser = new MessageParser(message, actor, triggers);
            
            parser.FindTrigger();
            
            //Here's the rub. This call is a sequential call up to this point, maybe we want to
            //kick off a separate thread so that then it won't hold up the players actions and
            //then we can even execute states that operate on a delay.

            if (parser.TriggersToExecute.Count > 0) {
				foreach (ITrigger trigger in parser.TriggersToExecute) {
					IState state = GetStateFromName(trigger.StateToExecute);
					if (state != null) {
						ChangeState(state, npc);
						npc.NextAiAction = DateTime.Now.ToUniversalTime().AddSeconds(-1); //this state will execute next now
						state.Execute(npc, trigger);
					}
				}
            }
        }
    }

   
}

