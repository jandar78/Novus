using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Extensions;
using Interfaces;
using System.Threading.Tasks;

namespace Character
{
    public class NPCUtils
    {
        private static NPCUtils _instance;

        private NPCUtils()
        {
            ProcessingAI = false;
        }

        public static NPCUtils GetInstance()
        {
            return _instance ?? (_instance = new NPCUtils());
        }

        public bool ProcessingAI
        {
            get;
            set;
        }

        public async void ProcessAIForNPCs()
        {         
            await Task.Run(async () =>
            {
                if (!ProcessingAI)
                {
                    ProcessingAI = true;
                    //loop through each NPC and call the Update() method
                    foreach (var npc in await MongoUtils.MongoData.FindAll<NPC>(MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters")))
                    {
                        if (DateTime.Now.ToUniversalTime() > npc.NextAiAction)
                        {
                            npc.Update();
                            //in case the Rot Ai state cleaned this guy out of the DB.
                            if (GetAnNPCByID(npc.Id) != null)
                            {
                                npc.Save();
                            }
                        }
                    }
                    ProcessingAI = false;
                }
            });
        }

        //this creates a new type of NPC as long as it hasn't hit the max world amount permissible
        public static async Task<IActor> CreateNPC(int MobTypeID, string state = null)
        {
            var npcCollection = MongoUtils.MongoData.GetCollection<NPCWorldCount>("World", "NPCs");
            var npcMobs = await MongoUtils.MongoData.RetrieveObjectAsync<NPCWorldCount>(npcCollection, n => n.Id == MobTypeID);

            IActor actor = null;

            if (npcMobs.Current < npcMobs.Max)
            {
                actor = Factories.Factory.CreateNPCCharacter(MobTypeID).Result;
                INPC npc = actor as INPC;
                
                if (state != null)
                {//give it a starting state, so it can be something other than Wander
                    npc.Fsm.state = npc.Fsm.GetStateFromName(state.CamelCaseWord());
                }

                npcMobs.Current += 1;
                await MongoUtils.MongoData.SaveAsync<NPCWorldCount>(npcCollection, n => n.Id == MobTypeID, npcMobs);
            }

            return actor;
        }

        public async void RegenerateAttributes()
        {
            foreach (var npc in await MongoUtils.MongoData.FindAll<NPC>(MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters")))
            {
                foreach (var attrib in npc.GetAttributes())
                {
                    npc.ApplyRegen(attrib.Name);
                }

                npc.Save();
            }
        }

        public async void CleanupBonuses()
        {
            
            foreach (var npc in await MongoUtils.MongoData.FindAll<NPC>(MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters")))
            {
                npc.CleanupBonuses();
                npc.Save();
            }
        }

        public static List<IActor> GetAnNPCByName(string name, string location)
        {
            List<IActor> npcList = null;
            IEnumerable<NPC> results;
            var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            if (!string.IsNullOrEmpty(location))
            {
                results = MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(npcCollection, n => n.FirstName == name.CamelCaseWord() && n.Location == location).Result;
            }
            else
            {
                results = MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(npcCollection, n => n.FirstName == name.CamelCaseWord()).Result;
            }

            if (results.Count() > 0)
            {
                npcList = new List<IActor>();
                foreach (var npc in results)
                {
                    npcList.Add(npc);
                }
            }

            return npcList;
        }

        public static IUser GetUserAsNPCFromList(List<ObjectId> id)
        {
            if (id.Count > 0)
            {
                IUser result = new Sockets.User();
                result.Player = GetAnNPCByID(id[0]);
                result.CurrentState = UserState.TALKING;
                return result;
            }

            return null;
        }

        public static IActor GetAnNPCByID(ObjectId id)
        {
            NPC npc = null;

            if (id != null)
            {
                var npcCollection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
                npc = MongoUtils.MongoData.RetrieveObjectAsync<NPC>(npcCollection, n => n.Id == id).Result;
            }

            return npc;
        }

        public async static void AlertOtherMobs(string location, int mobType, ObjectId id)
        {
            var collection = MongoUtils.MongoData.GetCollection<NPC>("Characters", "NPCCharacters");
            var npcs = await MongoUtils.MongoData.RetrieveObjectsAsync<NPC>(collection, n => n.Location == location && n.MobTypeID == mobType);

            foreach (var npc in npcs)
            {
                npc.CurrentTarget = id;
                npc.Fsm.ChangeState(AI.Combat.GetState(), npc);
                npc.NextAiAction = DateTime.Now.ToUniversalTime();
                npc.Save();
            }
        }
    }

    public class NPCWorldCount
    {
        public int Id { get; set; }
        public int Max { get; set; }
        public int Current { get; set; }
        public string Name { get; set; }
    }
}
