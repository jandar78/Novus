using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triggers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using ClientHandling;

namespace Quests {

    //Quest is made up of several steps that have their own scripts and triggers to run.  Each step will consist of an action the NPC/Item will do.
    //Quest FindTheBarnKey will have step 1, 2, 3 and 4
    //Step 1 will trigger when a player walks into the room the NPC is in and cause the NPC to say "Will you help me?"
    //Step 2 will trigger on the response of "Yes" or "No"
    //Step 3 will trigger upon player giving Key to NPC, NPC will say "Thank you!"
    //Upon quest completion XP will awarded or further steps will cause NPC to give player item reward
    //Each time a player moves to a new step they should not be re-prompted for previous steps unless it is an informational step.


	//I've figured it out. only NPC's or Rooms can use the Quest and QuestTrigger.  Items can use their own triggers to then point a player toward a quest but the
	//item itself can not keep track of a quest.  Also with the way we are doing things an NPC may be responsible for starting a quest and then it can be finished somewhere
	//else.  In these cases the first NPC should probably hand something to the player to give someone else or tell them to go retrieve it and take it to someone else.
	//This will allow for the creation of more dynamic quests which players can complete any way they want. (kill everyone, sneak, confusion spell, disguise, mind control, etc.)
	//Also quests will not be assisted it is up to the player to remember what quests are currently ongoing and what to do.  They may go back to NPCs to find out what they had to do,
	//if the NPC wants to give that information or is even still there or alive.

    public class Quest : IQuest {
        private List<QuestStep> _questSteps;
        private Dictionary<string, int> _currentPlayerStep;

        private List<QuestStep> QuestSteps {
            get {
                if (_questSteps == null) {
                    _questSteps = new List<QuestStep>();
                }

                return _questSteps;
            }
        }

		//this needs to be stored on the NPC as a Dictionary<questID, Dictionary<string,int>>
		//so that we can fill it up upon NPC load and save it to the DB as well.
		public Dictionary<string, int> CurrentPlayerStep {
			get {
				if (_currentPlayerStep == null) {
					_currentPlayerStep = new Dictionary<string, int>();
				}

				return _currentPlayerStep;
			}
			set {
				_currentPlayerStep = value;
			}
		}
        
        public string QuestID {
            get;
            private set;
        }

        private int CurrentStep {
			get;
			set;
        }

        private short TotalSteps {
            get;
            set;
        }

        public Quest(string questID, Dictionary<string, int> playerSteps) {
            QuestID = questID;
			CurrentPlayerStep = playerSteps;
            LoadQuestSteps();
        }

		/// <summary>
		/// Allows players to complete the quest out of order.  If they return the item without starting the quest it still completes.
		/// </summary>
		private bool AllowOutOfOrder {
			get; set;
		}

		/// <summary>
		/// Only one player at a time can ever be doing this quest.
		/// </summary>
		private bool IsUnique {
			get; set;
		}
        
        private void LoadQuestSteps() {
            BsonDocument doc = MongoUtils.MongoData.GetCollection("Quests", "QuestProgress")
                                   .FindOneAs<BsonDocument>(Query.EQ("_id", QuestID));

			if (doc != null) {
				AllowOutOfOrder = doc["AllowOutOfOrder"].AsBoolean;
				IsUnique = doc["IsUnique"].AsBoolean;

				foreach (BsonDocument stepDoc in doc["Steps"].AsBsonArray) {
					QuestStep temp = new QuestStep();
					temp = new QuestStep();
					temp.QuestID = QuestID;
					temp.Trigger = new Triggers.QuestTrigger((stepDoc["Trigger"].AsBsonDocument));

					QuestSteps.Add(temp);
				}
			}
        }

        public int AddPlayerToQuest(string playerID) {
			int stepNumber = 0;
            foreach (QuestStep step in QuestSteps) {
                if (step.AddPlayerToQuest(playerID)) {
					if (CurrentPlayerStep.ContainsKey(playerID)) {
						CurrentPlayerStep[playerID] = stepNumber;
					}
					else {
						CurrentPlayerStep.Add(playerID, stepNumber);
					}
                    break;
                }
				stepNumber++;
            }
			
			return stepNumber;
        }

		public void ProcessQuestStep(Message message, Character.Iactor npc) {
			AI.MessageParser parser = null;
			CurrentStep = 0;
			if (message.InstigatorType == Message.ObjectType.Player) {
				//find the step that contains the trigger
				foreach (QuestStep step in QuestSteps) {
					parser = new AI.MessageParser(message, npc, new List<ITrigger> { step.Trigger });
					parser.FindTrigger();

					if (parser.TriggerToExecute != null) {
						CurrentStep = AddPlayerToQuest(message.InstigatorID);
						if (!AllowOutOfOrder && CurrentStep != CurrentPlayerStep[message.InstigatorID]) {
							//we will not execute the trigger since they need to start this quest from the beginning
							break;
						}
						TriggerEventArgs e = new TriggerEventArgs(npc.ID, TriggerEventArgs.IDType.Npc, message.InstigatorID, (TriggerEventArgs.IDType)Enum.Parse(typeof(TriggerEventArgs.IDType), message.InstigatorType.ToString()), message.Room);
                        parser.TriggerToExecute.HandleEvent(null, e);
					}

					CurrentStep++;
				}
			}
		}

		public void StartQuest(string playerID) {
            //maybe some quests put some other events in motion
            //or have to check to see if something in the world is set before it can even be given out
			//spawn any necessary things in the world
			//can just load up a script and run it that can make that happen
        }

        public void EndQuest(string playerID) {
			//give player XP reward
			//update the world or reset certain things
			//can just load up a script and run it
		}
	}

    internal class QuestStep {
        private HashSet<string> _playerIDList;

        public QuestStep() { }  

        public HashSet<string> PlayerIDList {
            get {
                if (_playerIDList == null) {
                    _playerIDList = new HashSet<string>();
                }

                return _playerIDList;
            }
        }

        //For the trigger to work we'll have to force the event to happen from wherever it is we are looking through the events that have happened
        //that we may care about.
        public ITrigger Trigger {
            get;
            set;
        }

        public string QuestID {
            get;
            set;
        }

        public int Step {
            get;
            set;
        }

        //mainly for when we want to process a step that is not dependent on a trigger.
        //we may have some NPC dialogue along with emotes that in between check to see if the player they are talking
        //to is still around or saying something in which case a single script that did all that would not be very feasible.
        //but how do we get this to execute from the previous step?
        public void ProcessStep() {
            Trigger.HandleEvent();
        }

        public bool AddPlayerToQuest(string playerID) {
            return PlayerIDList.Add(playerID);
        }
    }
}
