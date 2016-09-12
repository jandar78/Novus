using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Interfaces;

namespace Factories {

	public abstract class Factory {
		public static IActor CreateCharacter(CharacterType characterType, int MobTypeID = 0) {
			IActor character = null;

			switch (characterType) {
				case CharacterType.NPC:
                    if (MobTypeID != 0) {
                        character = CreateNPCCharacter(MobTypeID).Result;
                    }
                    else {
                        character = CreateNPCCharacterHusk();
                    }
					break;
                case CharacterType.PLAYER:
                    character = new Character.Character();
                    break;
				default: character = null;
					break;
			}

			return character;
		}

        public static IActor CreateNPCCharacterHusk() {

            CharacterClass charClass = CharacterClass.Fighter;
            EyeColors EyeColor = EyeColors.Black;
            Genders Gender = Genders.Male;
            HairColors HairColor = HairColors.Black;
            CharacterRace Race = CharacterRace.Dwarf;
            SkinColors SkinColor = SkinColors.Black;
            SkinType SkinType = SkinType.Feathers;
            Languages Language = Languages.Common;
            BodyBuild Build = BodyBuild.Athletic;

            IActor actor = new NPC(Race, charClass, Gender, Language, SkinColor, SkinType, HairColor, EyeColor, Build);
            INpc npc = actor as INpc;
            npc.Fsm.state = AI.Wander.GetState();

            return actor;
        }

        public async static Task<IActor> CreateNPCCharacter(int id) {
            var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "NPCs");
            var npc = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(collection, n => n["_id"] == id).Result;
            
            if (npc["Current"].AsInt32 >= npc["Max"].AsInt32) {
                return null; //we've exceeded the world limit for this type of NPC don't create it
            }

            //we are gonna make the NPC so update the count
            npc["Current"] = npc["Current"].AsInt32 + 1;
            await MongoUtils.MongoData.SaveAsync<BsonDocument>(collection, n => n["_id"] == id, npc);

            collection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "NPCTemplates");
            var template = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(collection, m => m["MobTypeID"] == npc["_id"]).Result;
                        
            CharacterClass charClass = (CharacterClass)Enum.Parse(typeof(CharacterClass), template["Class"].AsString);
            EyeColors EyeColor = (EyeColors)Enum.Parse(typeof(EyeColors), template["EyeColor"].AsString);
            Genders Gender = (Genders)Enum.Parse(typeof(Genders), template["Gender"].AsString);
            HairColors HairColor = (HairColors)Enum.Parse(typeof(HairColors), template["HairColor"].AsString);         
            CharacterRace Race = (CharacterRace)Enum.Parse(typeof(CharacterRace), template["Race"].AsString);           
            SkinColors SkinColor = (SkinColors)Enum.Parse(typeof(SkinColors), template["SkinColor"].AsString);
            SkinType SkinType = (SkinType)Enum.Parse(typeof(SkinType), template["SkinType"].AsString);
            Languages Language = (Languages)Enum.Parse(typeof(Languages), template["Language"].AsString);
            BodyBuild Build = (BodyBuild)Enum.Parse(typeof(BodyBuild), template["Build"].AsString);
            
            var newNpc = new NPC(Race, charClass, Gender, Language, SkinColor, SkinType, HairColor, EyeColor, Build);

            newNpc.FirstName = template["FirstName"].AsString;
            BsonArray descriptions = template["Descriptions"].AsBsonArray;
            if (descriptions.Count > 1) {
                newNpc.Description = descriptions[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, descriptions.Count)]["Description"].AsString;
            }
            else {
                newNpc.Description = descriptions[0]["Description"].AsString;
            }
            newNpc.MobTypeID = template["MobTypeID"].AsInt32;
           // npc.Location = template["Location"].AsString;
            newNpc.Weight = template["Weight"].AsDouble;
            newNpc.Height = template["Height"].AsDouble;
            newNpc.LastCombatTime = DateTime.MinValue.ToUniversalTime();
            newNpc.Experience = template["Experience"].AsInt64;
            newNpc.Level = template["Level"].AsInt32;
            newNpc.IsNPC = true;
            BsonArray attribArray = template["Attributes"].AsBsonArray;

            foreach (BsonDocument attribute in attribArray) {
                newNpc.SetAttributeValue(attribute["Name"].AsString, attribute["Value"].AsDouble);
                newNpc.SetMaxAttributeValue(attribute["Name"].AsString, attribute["Max"].AsDouble);
                newNpc.SeAttributeRegenRate(attribute["Name"].AsString, attribute["RegenRate"].AsDouble);
            }

            newNpc.Fsm.state = AI.Wander.GetState();

			return newNpc;
		}
	}
}

	

