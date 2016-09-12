using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Interfaces;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Options;

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
            BsonClassMap.RegisterClassMap<Character.Character>(cm =>
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
            BsonClassMap.RegisterClassMap<Rooms.Room>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.Id);
            });
        }

        private static void RegisterDoorMapping()
        {
            BsonClassMap.RegisterClassMap<Rooms.Door>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(r => r.Id);
            });
        }

        private static void RegisterItemMapping()
        {
            BsonClassMap.RegisterClassMap<Items.Items>(cm =>
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
            BsonClassMap.RegisterClassMap<Triggers.GeneralTrigger>(cm => {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<Triggers.QuestTrigger>(cm => {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<Triggers.ItemTrigger>(cm => {
                cm.AutoMap();
            });
        }

        private static void RegisterRoomModifierMapping() {
            BsonClassMap.RegisterClassMap<Rooms.RoomModifier>(cm => {
                cm.AutoMap();
                cm.SetDiscriminator("RoomModifier");
                cm.GetMemberMap(r => r.TimeInterval).SetElementName("Timer");
                cm.GetMemberMap(r => r.Id).SetElementName("id");
            });
        }

        private static void RegisterExitsMapping() {
            BsonClassMap.RegisterClassMap<Rooms.Exits>(cm => {
                cm.AutoMap();
            });
        }
    }
}

