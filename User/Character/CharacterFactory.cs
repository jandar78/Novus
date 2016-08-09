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
using Interfaces;

namespace CharacterFactory {

	public abstract class Factory {
		public static IActor CreateCharacter(CharacterType characterType, int MobTypeID = 0) {
			IActor character = null;

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

        internal static IActor CreateNPCCharacterHusk() {

            CharacterClass charClass = CharacterClass.Fighter;
            EyeColors EyeColor = EyeColors.Black;
            Genders Gender = Genders.Male;
            HairColors HairColor = HairColors.Black;
            CharacterRace Race = CharacterRace.Dwarf;
            SkinColors SkinColor = SkinColors.Black;
            SkinType SkinType = SkinType.Feathers;
            Languages Language = Languages.Common;
            BodyBuild Build = BodyBuild.Athletic;

            IActor actor = new Character.NPC(Race, charClass, Gender, Language, SkinColor, SkinType, HairColor, EyeColor, Build);
            INpc npc = actor as INpc;
            npc.Fsm.state = AI.Wander.GetState();

            return actor;
        }

        internal static IActor CreateNPCCharacter(int id) {
			
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

            
            CharacterClass charClass = (CharacterClass)Enum.Parse(typeof(CharacterClass), template["Class"].AsString);
            EyeColors EyeColor = (EyeColors)Enum.Parse(typeof(EyeColors), template["EyeColor"].AsString);
            Genders Gender = (Genders)Enum.Parse(typeof(Genders), template["Gender"].AsString);
            HairColors HairColor = (HairColors)Enum.Parse(typeof(HairColors), template["HairColor"].AsString);         
            CharacterRace Race = (CharacterRace)Enum.Parse(typeof(CharacterRace), template["Race"].AsString);           
            SkinColors SkinColor = (SkinColors)Enum.Parse(typeof(SkinColors), template["SkinColor"].AsString);
            SkinType SkinType = (SkinType)Enum.Parse(typeof(SkinType), template["SkinType"].AsString);
            Languages Language = (Languages)Enum.Parse(typeof(Languages), template["Language"].AsString);
            BodyBuild Build = (BodyBuild)Enum.Parse(typeof(BodyBuild), template["Build"].AsString);
            
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
           // npc.Location = template["Location"].AsString;
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
	}
}

	

