using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;

namespace MongoUtils { 
    public class ClassMapper {
        public static void RegisterMappings() {
            RegisterScriptMapping();
            RegisterTriggerMappings();
            RegisterItemMapping();
            RegisterRoomModifierMapping();
            RegisterExitsMapping();
            RegisterDoorMapping();
            RegisterRoomMapping();
            RegisterQuestStepMapping();
            RegisterQuestMapping();
            RegisterCharacterMapping();
            RegisterNPCMapping();
        }

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
            BsonClassMap.RegisterClassMap<Items>(cm =>
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

        private static void RegisterScriptMapping() {
            BsonClassMap.RegisterClassMap<Script>(cm => {
                cm.AutoMap();
                cm.MapIdMember(s => s.ID);
                cm.GetMemberMap(s => s.ScriptByteArray).SetElementName("Bytes");
            });
        }

        private static void RegisterTriggerMappings() {
            BsonClassMap.RegisterClassMap<GeneralTrigger>(cm => {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<QuestTrigger>(cm => {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<ItemTrigger>(cm => {
                cm.AutoMap();
            });
        }

        private static void RegisterRoomModifierMapping() {
            BsonClassMap.RegisterClassMap<RoomModifier>(cm => {
                cm.AutoMap();
                cm.GetMemberMap(r => r.TimeInterval).SetElementName("Timer");
            });
        }

        private static void RegisterExitsMapping() {
            BsonClassMap.RegisterClassMap<Exits>(cm => {
                cm.AutoMap();
            });
        }
    }
}

