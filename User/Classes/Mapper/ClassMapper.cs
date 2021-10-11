using MongoDB.Bson.Serialization;
using Interfaces;
using Quests;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson;
using Character;
using Triggers;

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
            RegisterInventoryMapping();
            RegisterEquipmentMapping();
            RegisterCharacterMapping();
            RegisterNPCMapping();

        }

        private static void RegisterInventoryMapping()
        {
            BsonClassMap.RegisterClassMap<Inventory>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty(i => i.inventory);
            });
        }

        private static void RegisterEquipmentMapping()
        {
            BsonClassMap.RegisterClassMap<Equipment>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty(e => e.Equipped);
            });
        }

        private static void RegisterCharacterMapping()
        {
            BsonClassMap.RegisterClassMap<Character.Character>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id);
                cm.SetIgnoreExtraElements(true);
                cm.UnmapProperty(c => c.Bonuses); //Get this working like Inventory and Equipment
                cm.GetMemberMap(c => c.Equipment).SetElementName("Equipment");
                cm.GetMemberMap(c => c.Inventory).SetElementName("Inventory");
            });       
        }

        private static void RegisterNPCMapping() {
            BsonClassMap.RegisterClassMap<NPC>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.MapIdMember(n => n.Id);
                cm.UnmapProperty(n => n.Bonuses);
                cm.UnmapProperty(n => n.Fsm);
                cm.UnmapProperty(n => n.Messages);
                cm.MapMember(n => n.LastCombatTime).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                cm.MapMember(n => n.NextAiAction).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            });
        }

        private static void RegisterRoomMapping() {
            BsonClassMap.RegisterClassMap<Rooms.Room>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.UnmapProperty(r => r.players);
                cm.UnmapProperty(r => r.npcs);
                cm.UnmapProperty(r => r.items);
                cm.MapIdMember(r => r.Id).SetIgnoreIfDefault(true); ;
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
                cm.MapIdMember(i => i.Id);
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
                cm.MapIdMember(s => s.Id);
                cm.UnmapProperty(s => s.MemStreamAsByteArray);
                cm.UnmapProperty(s => s.MemStreamAsString);
                cm.GetMemberMap(s => s.ScriptByteArray).SetElementName("Bytes");
                cm.GetMemberMap(s => s.ScriptType).SetElementName("Type");
            });

            BsonClassMap.RegisterClassMap<TriggerScript>(cm => {
                cm.AutoMap();
                cm.MapIdMember(s => s.Id);
                cm.UnmapProperty(s => s.MemStreamAsByteArray);
                cm.UnmapProperty(s => s.MemStreamAsString);
                cm.GetMemberMap(s => s.ScriptByteArray).SetElementName("Bytes");
                cm.GetMemberMap(s => s.ScriptType).SetElementName("Type");
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
            BsonClassMap.RegisterClassMap<Rooms.RoomModifier>(cm => {
                cm.AutoMap();
                cm.SetDiscriminator("RoomModifier");
                cm.GetMemberMap(r => r.Timer).SetElementName("Timer");
                cm.GetMemberMap(r => r.Id).SetElementName("id");
            });
        }

        private static void RegisterExitsMapping() {
            BsonClassMap.RegisterClassMap<Rooms.Exits>(cm => {
                cm.AutoMap();
                cm.UnmapProperty(e => e.AvailableExits);
                cm.UnmapProperty(e => e.Doors);
            });
        }

        private static void RegisterAttributeMapping() {
            BsonClassMap.RegisterClassMap<Character.Attribute>(cm => {
                cm.AutoMap();
            });
        }

        private static void RegisterBonusesMapping() {
            BsonClassMap.RegisterClassMap<Character.StatBonus>(cm => {
                cm.AutoMap();
            });
        }
    }
}

