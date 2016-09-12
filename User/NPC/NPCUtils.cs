using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Extensions;
using Interfaces;

namespace Character {
	public class NPCUtils {
		private static NPCUtils Instance;
		private static ConcurrentBag<string> _npcList;

		private NPCUtils() {
			ProcessingAI = false;
			_npcList = new ConcurrentBag<string>();
		}

		public static NPCUtils GetInstance() {
			return Instance ?? (Instance = new NPCUtils());
		}

		public bool ProcessingAI {
			get;
			set;
		}

		public void LoadNPCs() {
			GetNPCList();
		}

		public void ProcessAIForNPCs() {
			if (!ProcessingAI) {
				ProcessingAI = true;
				LoadNPCs();
				//loop through each NPC and call the Update() method
				foreach (string id in _npcList) {
					IActor actor = Factories.Factory.CreateCharacter(CharacterType.NPC);
					actor.Load(id);
					INpc npc = actor as INpc;
					if (DateTime.Now.ToUniversalTime() > npc.NextAiAction) {
						npc.Update();
						//in case the Rot Ai state cleaned this guy out of the DB.
						if (GetAnNPCByID(id) != null) {
							actor.Save();
						}
					}
				}
				ProcessingAI = false;
			}
		}

		//this creates a new type of NPC as long as it hasn't hit the max world amount permissible
		public static IActor CreateNPC(int MobTypeID, string state = null) {
			MongoUtils.MongoData.ConnectToDatabase();
			var npcCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "NPCs");
            var npcMobs = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(npcCollection, n => n["MobTypeID"] == MobTypeID).Result;

			IActor actor = null;

			if (npcMobs["Current"].AsInt32 < npcMobs["Max"].AsInt32) {
				actor = Factories.Factory.CreateNPCCharacter(MobTypeID).Result;
				INpc npc = actor as INpc;
				if (state != null) {//give it a starting state, so it can be something other than Wander
					npc.Fsm.state = npc.Fsm.GetStateFromName(state.CamelCaseWord());
				}
				npcMobs["Current"] = npcMobs["Current"].AsInt32 + 1;
				var saveResult = MongoUtils.MongoData.SaveAsync<BsonDocument>(npcCollection, n => n["MobTypeId"] == MobTypeID, npcMobs).Result;
			}

			return actor;
		}

		public void RegenerateAttributes() {
			if (_npcList != null) {
				foreach (string id in _npcList) {
					IActor actor = Factories.Factory.CreateCharacter(CharacterType.NPC);
					actor.Load(id);
					foreach (KeyValuePair<string, IAttributes> attrib in actor.GetAttributes()) {
						actor.ApplyRegen(attrib.Key);
					}
					actor.Save();
				}
			}
		}

		public void CleanupBonuses() {
			if (_npcList != null) {
				foreach (string id in _npcList) {
					IActor actor = Factories.Factory.CreateCharacter(CharacterType.NPC);
					actor.Load(id);
					actor.CleanupBonuses();
					actor.Save();
				}
			}
		}

		private void GetNPCList() {
			var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");

			if (!_npcList.IsEmpty) {
				_npcList = new ConcurrentBag<string>(); //new it up to clear it
			}

            
            foreach (var npc in MongoUtils.MongoData.FindAll<NPC>(npcCollection).Result) {
				_npcList.Add(npc.ID);
			}
		}

		public static List<IActor> GetAnNPCByName(string name, string location = null) {
			List<IActor> npcList = null;
            IEnumerable<NPC> results;
			var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Charcaters", "NPCCharacters");
            if (!string.IsNullOrEmpty(location)) {
                results = MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(npcCollection, n => n.FirstName == name.CamelCaseWord() && n.Location == location).Result;
            }
            else {
                results = MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(npcCollection, n => n.FirstName == name.CamelCaseWord()).Result;
            }

			if (results.Count() > 0) {
				npcList = new List<IActor>();
				foreach (var found in results) {
					npcList.Add(found);
				}
			}

			return npcList;
		}

		public static IUser GetUserAsNPCFromList(List<string> id) {
			if (id.Count > 0) {
				IUser result = new Sockets.User();
				result.Player = GetAnNPCByID(id[0]);
				result.CurrentState = UserState.TALKING;
				return result;
			}

			return null;
		}

		public static IActor GetAnNPCByID(string id) {
			if (string.IsNullOrEmpty(id)) {
				return null;
			}

			var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
			var result = MongoUtils.MongoData.RetrieveObjectAsync<NPC>(npcCollection, n => n.ID == id).Result;
			
			//if (results != null) {
			//	npc = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
			//	npc.Load(results["_id"].AsObjectId.ToString());
			//}

			return result;
		}

		public async static void AlertOtherMobs(string location, int mobType, string id) {
			var collection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
			var results = MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(collection, n => n.Location == location && n.MobTypeID == mobType).Result;

			foreach (var npc in results) {
				npc.CurrentTarget = id;
				npc.Fsm.ChangeState(AI.Combat.GetState(), npc);
				npc.NextAiAction = DateTime.Now.ToUniversalTime();
				await MongoUtils.MongoData.SaveAsync<NPC>(collection, n => n.ID == npc.ID, npc);
			}
		}
	}
}
