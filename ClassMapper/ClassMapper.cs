using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Interfaces;

namespace MongoUtils { 
    public class ClassMapper {
        public static void RegisterMappings() {
            RegisterCharacterMapping();
            RegisterNPCMapping();
            RegisterItemMapping();
            RegisterRoomMapping();
            RegisterDoorMapping();
            RegisterQuestMapping();        }

        private static void RegisterCharacterMapping()
        {
            BsonClassMap.RegisterClassMap<Character>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(p => p.ID);
            });       
        }

        private static void RegisterNPCMapping() {
            BsonClassMap.RegisterClassMap<NPC>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(n => n.ID);
            });

        }

        private static void RegisterRoomMapping() {
            BsonClassMap.RegisterClassMap<Room>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.Id);
            });
        }

        private static void RegisterDoorMapping()
        {
            BsonClassMap.RegisterClassMap<Door>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.Id);
            });
        }

        private static void RegisterItemMapping()
        {
            BsonClassMap.RegisterClassMap<Item>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.Id);
            });
        }

        private static void RegisterQuestMapping()
        {
            BsonClassMap.RegisterClassMap<Quest>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.QuestID);
            });
        }

        private static void RegisterQuestStepMapping()
        {
            BsonClassMap.RegisterClassMap<QuestStep>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.QuestID);
            });
        }
    }
}

