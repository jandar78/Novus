using System;
using System.Collections.Generic;
using System.Linq;
using Triggers;
using MongoDB.Bson;
using Interfaces;

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
        private List<IQuestStep> _questSteps;
        private Dictionary<ObjectId, int> _currentPlayerStep;

        public List<IQuestStep> QuestSteps {
            get {
                if (_questSteps == null) {
                    _questSteps = new List<IQuestStep>();
                }

                return _questSteps;
            }
        }

		//this needs to be stored on the NPC as a Dictionary<questID, Dictionary<string,int>>
		//so that we can fill it up upon NPC load and save it to the DB as well.
		public Dictionary<ObjectId, int> CurrentPlayerStep {
			get {
				if (_currentPlayerStep == null) {
					_currentPlayerStep = new Dictionary<ObjectId, int>();
				}

				return _currentPlayerStep;
			}
			set {
				_currentPlayerStep = value;
			}
		}
        
        public string Id { get; set; }
        public string QuestID {
            get;
            set;
        }

        public int CurrentStep {
			get;
			set;
        }

        public short TotalSteps {
            get;
            set;
        }

		public bool AutoProcessNextStep {
			get {
				return AutoProcessPlayer.Count > 0;
			}
		}

        public Quest() { }

        public Quest(string questID, Dictionary<ObjectId, int> playerSteps) {
			AutoProcessPlayer = new Queue<ObjectId>();
			QuestID = questID;
			CurrentPlayerStep = playerSteps;
          //  LoadQuestSteps();
			
        }

		/// <summary>
		/// Allows players to complete the quest out of order.  If they return the item without starting the quest it still completes.
		/// </summary>
		public bool AllowOutOfOrder {
			get; set;
		}

		public Queue<ObjectId> AutoProcessPlayer { get; set; }

		/// <summary>
		/// Only one player at a time can ever be doing this quest.
		/// </summary>
		private bool IsUnique {
			get; set;
		}
        
   //     private void LoadQuestSteps() {
   //         var quest = MongoUtils.MongoData.RetrieveObject<Quest>(MongoUtils.MongoData.GetCollection<Quest>("Quests", "QuestProgress"), q => q.QuestID == QuestID);
            
			//if (quest != null) {
			//	//AllowOutOfOrder = doc["AllowOutOfOrder"].AsBoolean;
			//	//IsUnique = doc["IsUnique"].AsBoolean;
			//	int stepNumber = 0;
			//	foreach (BsonDocument stepDoc in doc["Steps"].AsBsonArray) {
			//		QuestStep temp = new QuestStep();
			//		temp = new QuestStep();
			//		temp.QuestID = QuestID;
			//		temp.AppliesToNPC = stepDoc["AppliesToNPC"].AsBoolean;
			//		temp.IfPreviousCompleted = stepDoc["OnlyIfPreviousCompleted"].AsBoolean;
			//		temp.Trigger = new Triggers.QuestTrigger((stepDoc["Trigger"].AsBsonDocument));
			//		temp.Step = stepNumber;
			//		temp.AutoProcess = stepDoc["AutoProcess"].AsBoolean;
			//		QuestSteps.Add(temp);

			//		stepNumber++;
			//	}
			//}
   //     }

        public int AddPlayerToQuest(ObjectId playerID, int stepNumber) {
			foreach (QuestStep step in QuestSteps.Where(s => s.Step > stepNumber)) {
                if (step.AddPlayerToQuest(playerID)) {
					if (CurrentPlayerStep.ContainsKey(playerID)) {
						CurrentPlayerStep[playerID] = step.Step;
					}
					else {
						CurrentPlayerStep.Add(playerID, step.Step);
					}
                    break;
                }
				stepNumber++;
            }

			return CurrentPlayerStep[playerID];
        }

		public void AutoProcessQuestStep(IActor npc) {
			var id = AutoProcessPlayer.Dequeue();

			IUser player = Sockets.Server.GetAUser(id);

			if (player == null) {
				player = Character.NPCUtils.GetUserAsNPCFromList(new List<ObjectId> { id });
			}

			int stepToProcess = CurrentPlayerStep[player.UserID];
			TriggerEventArgs e = new TriggerEventArgs(npc.Id, TriggerEventArgs.IDType.Npc, player.UserID, player.Player.IsNPC ? TriggerEventArgs.IDType.Npc : TriggerEventArgs.IDType.Player);
			QuestSteps[stepToProcess - 1].ProcessStep(null, e);
		}

		public void ProcessQuestStep(IMessage message, IActor npc) {
			AI.MessageParser parser = null;
			int currentStep = 0;
			if (CurrentPlayerStep.ContainsKey(ObjectId.Parse(message.InstigatorID))) {
				currentStep = CurrentPlayerStep[ObjectId.Parse(message.InstigatorID)];
				if (currentStep >= QuestSteps.Count) {
					currentStep = QuestSteps.Count - 1;
					CurrentPlayerStep[ObjectId.Parse(message.InstigatorID)] = currentStep;
				}
			}
			//find the step that contains the trigger
			if (QuestSteps.Count > 0)
			{
				IQuestStep step = QuestSteps[currentStep];
				if (message.InstigatorType == ObjectType.Player || (step.AppliesToNPC && message.InstigatorType == ObjectType.Npc))
				{ //only do it for players or if we specifically say it can trigger for NPC's
					parser = new AI.MessageParser(message, npc, new List<ITrigger> { step.Trigger });
					parser.FindTrigger();

					foreach (ITrigger trigger in parser.TriggersToExecute)
					{
						if (trigger.AutoProcess && currentStep == step.Step)
						{
							AutoProcessPlayer.Enqueue(ObjectId.Parse(message.InstigatorID));
							((NPC)npc).Fsm.ChangeState(AI.Questing.GetState(), npc as NPC);
							currentStep = AddPlayerToQuest(ObjectId.Parse(message.InstigatorID), currentStep);
							break;
						}

						if (!AllowOutOfOrder && currentStep != CurrentPlayerStep[ObjectId.Parse(message.InstigatorID)] || currentStep > step.Step)
						{
							//we will not execute the trigger since they need to start this quest from the beginning
							break;
						}

						if (step.IfPreviousCompleted && CurrentStep < (step.Step - 1))
						{ //this step won't process if the previous step was not completed
							break;
						}

						currentStep = AddPlayerToQuest(ObjectId.Parse(message.InstigatorID), currentStep);
						TriggerEventArgs e = new TriggerEventArgs(npc.Id, TriggerEventArgs.IDType.Npc, ObjectId.Parse(message.InstigatorID), (TriggerEventArgs.IDType)Enum.Parse(typeof(TriggerEventArgs.IDType), message.InstigatorType.ToString()), message.Room);
						trigger.HandleEvent(null, e);
					}
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

    internal class QuestStep : IQuestStep {
        private HashSet<ObjectId> _playerIDList;

        public QuestStep() { }  

        public HashSet<ObjectId> PlayerIDList {
            get {
                if (_playerIDList == null) {
                    _playerIDList = new HashSet<ObjectId>();
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

		public bool AppliesToNPC {
			get;
			set;
		}

		public bool AutoProcess { get; set; }

		public bool IfPreviousCompleted { get; set; }

        //mainly for when we want to process a step that is not dependent on a trigger.
        //we may have some NPC dialogue along with emotes that in between check to see if the player they are talking
        //to is still around or saying something in which case a single script that did all that would not be very feasible.
        //but how do we get this to execute from the previous step?
        public void ProcessStep(object sender, EventArgs e) {
            Trigger.HandleEvent(sender, e);
        }

        public bool AddPlayerToQuest(ObjectId playerID) {
            return PlayerIDList.Add(playerID);
        }
    }
}
