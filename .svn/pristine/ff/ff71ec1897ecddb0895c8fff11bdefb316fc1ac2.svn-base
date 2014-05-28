using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterEnums;
using Character;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CharacterFactory {

	public abstract class Factory {
		public static Character.Iactor CreateCharacter(CharacterEnums.CharacterType characterType, int MobTypeID = 0) {
			Character.Iactor character = null;

			switch (characterType) {
				case CharacterType.NPC:
                    if (MobTypeID != 0) {
                        character = CreateNPCCharacter(MobTypeID);
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

        internal static Character.Iactor CreateNPCCharacterHusk() {

            CharacterEnums.CharacterClass charClass = CharacterEnums.CharacterClass.FIGHTER;
            CharacterEnums.EyeColors EyeColor = CharacterEnums.EyeColors.BLACK;
            CharacterEnums.Genders Gender = CharacterEnums.Genders.MALE;
            CharacterEnums.HairColors HairColor = CharacterEnums.HairColors.BLACK;
            CharacterEnums.CharacterRace Race = CharacterEnums.CharacterRace.DWARF;
            CharacterEnums.SkinColors SkinColor = CharacterEnums.SkinColors.BLACK;
            CharacterEnums.SkinType SkinType = CharacterEnums.SkinType.FEATHERS;
            CharacterEnums.Languages Language = CharacterEnums.Languages.COMMON;
            CharacterEnums.BodyBuild Build = CharacterEnums.BodyBuild.ATHLETIC;

            Character.Iactor actor = new Character.NPC(Race, charClass, Gender, Language, SkinColor, SkinType, HairColor, EyeColor, Build);
            Character.Inpc npc = actor as Character.Inpc;
            npc.Fsm.state = AI.Wander.GetState();

            return actor;
        }

        internal static Character.Iactor CreateNPCCharacter(int id) {
			
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("NPCs");
            IMongoQuery query = Query.EQ("_id", id);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(query);

            if (doc["Current"].AsInt32 >= doc["Max"].AsInt32) {
                return null; //we've exceeded the world limit for this type of NPC don't create it
            }

            //we are gonna make the NPC so update the count
            doc["Current"] = doc["Current"].AsInt32 + 1;
            collection.Save(doc);

            collection = db.GetCollection("NPCTemplates");
            query = Query.EQ("MobTypeID", doc["_id"]);
            BsonDocument template = collection.FindOneAs<BsonDocument>(query);

            
            CharacterEnums.CharacterClass charClass = (CharacterEnums.CharacterClass)Enum.Parse(typeof(CharacterEnums.CharacterClass), template["Class"].AsString.ToUpper());
            CharacterEnums.EyeColors EyeColor = (CharacterEnums.EyeColors)Enum.Parse(typeof(CharacterEnums.EyeColors), template["EyeColor"].AsString.ToUpper());
            CharacterEnums.Genders Gender = (CharacterEnums.Genders)Enum.Parse(typeof(CharacterEnums.Genders), template["Gender"].AsString.ToUpper());
            CharacterEnums.HairColors HairColor = (CharacterEnums.HairColors)Enum.Parse(typeof(CharacterEnums.HairColors), template["HairColor"].AsString.ToUpper());         
            CharacterEnums.CharacterRace Race = (CharacterEnums.CharacterRace)Enum.Parse(typeof(CharacterEnums.CharacterRace), template["Race"].AsString.ToUpper());           
            CharacterEnums.SkinColors SkinColor = (CharacterEnums.SkinColors)Enum.Parse(typeof(CharacterEnums.SkinColors), template["SkinColor"].AsString.ToUpper());
            CharacterEnums.SkinType SkinType = (CharacterEnums.SkinType)Enum.Parse(typeof(CharacterEnums.SkinType), template["SkinType"].AsString.ToUpper());
            CharacterEnums.Languages Language = (CharacterEnums.Languages)Enum.Parse(typeof(CharacterEnums.Languages), template["Language"].AsString.ToUpper());
            CharacterEnums.BodyBuild Build = (CharacterEnums.BodyBuild)Enum.Parse(typeof(CharacterEnums.BodyBuild), template["Build"].AsString.ToUpper());
            
            Character.NPC npc = new Character.NPC(Race, charClass, Gender, Language, SkinColor, SkinType, HairColor, EyeColor, Build);

            npc.FirstName = template["FirstName"].AsString;
            BsonArray descriptions = template["Descriptions"].AsBsonArray;
            if (descriptions.Count > 1) {
                npc.Description = descriptions[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, descriptions.Count)]["Description"].AsString;
            }
            else {
                npc.Description = descriptions[0]["Description"].AsString;
            }
            npc.MobTypeID = template["MobTypeID"].AsInt32;
            npc.Location = template["Location"].AsInt32;
            npc.Weight = template["Weight"].AsDouble;
            npc.Height = template["Height"].AsDouble;
            npc.LastCombatTime = DateTime.MinValue.ToUniversalTime();
            npc.Experience = template["Experience"].AsInt64;
            npc.Level = template["Level"].AsInt32;
            npc.IsNPC = true;
            BsonArray attribArray = template["Attributes"].AsBsonArray;

            foreach (BsonDocument attribute in attribArray) {
                npc.SetAttributeValue(attribute["Name"].AsString, attribute["Value"].AsDouble);
                npc.SetMaxAttributeValue(attribute["Name"].AsString, attribute["Max"].AsDouble);
                npc.SeAttributeRegenRate(attribute["Name"].AsString, attribute["RegenRate"].AsDouble);
            }

            npc.Fsm.state = AI.Wander.GetState();

			return npc;
		}

        //mob should be same as NPC except they are not allowed to interact with player other than through combat
        internal static Character.Iactor CreateMobCharacter(CharacterRace race, CharacterClass characterClass, Genders gender) {
			Character.Character mobCharacter = new Character.Character();
			//Todo:
			//load mob info from database
			return mobCharacter;
		}

	}
}

	

