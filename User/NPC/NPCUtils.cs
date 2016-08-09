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
using CharacterEnums;
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
					IActor actor = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
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
			MongoDatabase character = MongoUtils.MongoData.GetDatabase("World");
			MongoCollection npcCollection = character.GetCollection("NPCs");
			IMongoQuery query = Query.EQ("_id", MobTypeID);
			BsonDocument doc = npcCollection.FindOneAs<BsonDocument>(query);

			IActor actor = null;

			if (doc["Current"].AsInt32 < doc["Max"].AsInt32) {
				actor = CharacterFactory.Factory.CreateNPCCharacter(MobTypeID);
				INpc npc = actor as INpc;
				if (state != null) {//give it a starting state, so it can be something other than Wander
					npc.Fsm.state = npc.Fsm.GetStateFromName(state.CamelCaseWord());
				}
				doc["Current"] = doc["Current"].AsInt32 + 1;
				npcCollection.Save(doc);
			}

			return actor;
		}

		public void RegenerateAttributes() {
			if (_npcList != null) {
				foreach (string id in _npcList) {
					IActor actor = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
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
					IActor actor = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
					actor.Load(id);
					actor.CleanupBonuses();
					actor.Save();
				}
			}
		}

		private void GetNPCList() {
			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase character = MongoUtils.MongoData.GetDatabase("Characters");
			MongoCollection npcCollection = character.GetCollection("NPCCharacters");


			if (!_npcList.IsEmpty) {
				_npcList = new ConcurrentBag<string>(); //new it up to clear it
			}


			foreach (BsonDocument id in npcCollection.FindAllAs<BsonDocument>()) {
				_npcList.Add(id["_id"].AsObjectId.ToString());
			}
		}

		public static List<IActor> GetAnNPCByName(string name, string location = null) {
			List<IActor> npcList = null;
			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase character = MongoUtils.MongoData.GetDatabase("Characters");
			MongoCollection npcCollection = character.GetCollection("NPCCharacters");
			IMongoQuery query;
			if (string.IsNullOrEmpty(location)) {
				query = Query.EQ("FirstName", name.CamelCaseWord());
			}
			else {
				query = Query.And(Query.EQ("FirstName", name.CamelCaseWord()), Query.EQ("Location", location));
			}

			var results = npcCollection.FindAs<BsonDocument>(query);

			if (results != null) {
				npcList = new List<IActor>();
				foreach (BsonDocument found in results) {
					IActor npc = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
					npc.Load(found["_id"].AsObjectId.ToString());
					npcList.Add(npc);
				}
			}

			return npcList;
		}

		public static User.User GetUserAsNPCFromList(List<string> id) {
			if (id.Count > 0) {
				User.User result = new User.User();
				result.Player = GetAnNPCByID(id[0]);
				result.CurrentState = User.User.UserState.TALKING;
				return result;
			}

			return null;
		}

		public static IActor GetAnNPCByID(string id) {
			if (string.IsNullOrEmpty(id)) {
				return null;
			}

			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase character = MongoUtils.MongoData.GetDatabase("Characters");
			MongoCollection npcCollection = character.GetCollection("NPCCharacters");
			IMongoQuery query = Query.EQ("_id", ObjectId.Parse(id));

			BsonDocument results = npcCollection.FindOneAs<BsonDocument>(query);
			Iactor npc = null;

			if (results != null) {
				npc = CharacterFactory.Factory.CreateCharacter(CharacterType.NPC);
				npc.Load(results["_id"].AsObjectId.ToString());
			}

			return npc;
		}

		public static void AlertOtherMobs(int location, int mobType, string id) {
			MongoUtils.MongoData.ConnectToDatabase();
			MongoDatabase db = MongoUtils.MongoData.GetDatabase("Characters");
			MongoCollection collection = db.GetCollection("NPCCharacters");

			IMongoQuery query = Query.And(Query.EQ("Location", location), Query.EQ("MobtypeID", mobType));

			var results = collection.FindAs<BsonDocument>(query);

			foreach (BsonDocument npc in results) {
				npc["CurrentTarget"] = id;
				npc["AiState"] = AI.Combat.GetState().ToString();
				npc["NextAiAction"] = DateTime.Now.ToUniversalTime();
				collection.Save(npc);
			}
		}
	}
}
