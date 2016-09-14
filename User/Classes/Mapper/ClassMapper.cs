using MongoDB.Bson.Serialization;
using Interfaces;
using Quests;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson;
using Character;

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
            RegisterAttributeMapping();
            RegisterBonusesMapping();
            RegisterCharacterMapping();
            RegisterNPCMapping();

        }

        private static void RegisterCharacterMapping()
        {
            BsonClassMap.RegisterClassMap<Character.Character>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(p => p.Id);
                cm.MapMember(p => p.Bonuses).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<BonusTypes, Bonus>>(DictionaryRepresentation.ArrayOfArrays));
            });       
        }

        private static void RegisterNPCMapping() {
            BsonClassMap.RegisterClassMap<NPC>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(n => n.Id);
                cm.MapMember(n => n.LastCombatTime).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                cm.MapMember(n => n.NextAiAction).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                cm.MapMember(n => n.XpTracker).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<ObjectId, double>>(DictionaryRepresentation.ArrayOfDocuments));
                cm.MapMember(n => n.Bonuses).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<BonusTypes, Bonus>>(DictionaryRepresentation.ArrayOfArrays));
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

        private static void RegisterAttributeMapping() {
            BsonClassMap.RegisterClassMap<Character.Attribute>(cm => {
                cm.AutoMap();
            });
        }

        private static void RegisterBonusesMapping() {
            BsonClassMap.RegisterClassMap<Character.Bonus>(cm => {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<Character.StatBonuses>(cm => {
                cm.AutoMap();
            });
        }
    }
}

