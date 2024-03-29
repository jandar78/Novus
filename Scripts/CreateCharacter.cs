﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Interfaces;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Scripts {
    public class CreateCharacter {
        private static IMongoCollection<BsonDocument> _generalCollection;
        private static IMongoCollection<BsonDocument> _scriptCollection;

        private static List<TempChar> usersCreatingChars = new List<TempChar>();

        public static CreateCharacter creationScript = null;

        public static CreateCharacter GetScript() {
            return creationScript ?? (creationScript = new CreateCharacter());
        }

        public void AddUserToScript(IUser user) {
            usersCreatingChars.Add(new TempChar(user));
        }

        private CreateCharacter() {
            _generalCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("Characters", "General");
            
            _scriptCollection = MongoUtils.MongoData.GetCollection<BsonDocument>("Scripts", "CreateCharacter");

        }
        public UserState InsertResponse(string response, string userId) {
            UserState state = UserState.CREATING_CHARACTER;

            if (string.IsNullOrEmpty(response)) return state;

            TempChar specificUser = usersCreatingChars.Where(u => u.user.UserID == userId).SingleOrDefault();

            //Get the confirmation from the previous selection
            if (specificUser.confirmStep != CreateCharSteps.NONE) {
                if (!ConfirmSelection(response)) {
                    specificUser.currentStep = specificUser.confirmStep; //go back to the previous selection
                    specificUser.lastStep = CreateCharSteps.NONE;
                }
                else {
                    specificUser.currentStep = specificUser.nextStep;
                }
                specificUser.confirmStep = CreateCharSteps.NONE;
                return state;
            }


            if (specificUser != null && specificUser.currentStep == CreateCharSteps.AWAITINGRESPONSE) {

                switch (specificUser.lastStep) {
                    case CreateCharSteps.FIRST_NAME:
                        specificUser.FirstName = response;
                        specificUser.currentStep = CreateCharSteps.LAST_NAME;
                        specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                        break;
                    case CreateCharSteps.LAST_NAME:
                        if (ValidatePlayerName(specificUser.user.UserID, response)) {
                            if (String.Compare(response, "name", true) != 0) {
                                specificUser.LastName = response;
                                specificUser.currentStep = CreateCharSteps.PASSWORD;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.FIRST_NAME;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                        }
                        else {
                            specificUser.currentStep = CreateCharSteps.FIRST_NAME;
                            specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                        }
                        break;
                    case CreateCharSteps.PASSWORD:
                        specificUser.Password = response;
                        specificUser.currentStep = CreateCharSteps.PASSWORD_CHECK;
                        specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                        break;

                    case CreateCharSteps.PASSWORD_CHECK:
                        if (ValidatePlayerPassword(specificUser.user.UserID, response)) {
                            specificUser.Password = response;
                            specificUser.currentStep = CreateCharSteps.CLASS;
                            specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                        }
                        else {
                            specificUser.currentStep = CreateCharSteps.PASSWORD;
                            specificUser.lastStep = CreateCharSteps.PASSWORD;
                        }
                        break;
                    case CreateCharSteps.CLASS: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                int max = (GetMaxEnum<CharacterClass>() / 8) + 1;
                                if (selection >= 1 && selection <= max) {
                                    specificUser.Class = (CharacterClass)(1 << selection);
                                    specificUser.confirmStep = CreateCharSteps.CLASS;
                                    specificUser.currentStep = CreateCharSteps.CLASS;
                                    specificUser.nextStep = CreateCharSteps.GENDER;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.CLASS;
                                    specificUser.lastStep = CreateCharSteps.CLASS;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.FIRST_NAME;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.GENDER: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<Genders>()) {
                                    specificUser.Gender = (Genders)selection;
                                    specificUser.currentStep = CreateCharSteps.RACE;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.GENDER;
                                    specificUser.lastStep = CreateCharSteps.GENDER;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.CLASS;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.RACE: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<CharacterRace>()) {
                                    specificUser.Race = (CharacterRace)selection;
                                    specificUser.confirmStep = CreateCharSteps.RACE;
                                    specificUser.currentStep = CreateCharSteps.RACE;
                                    specificUser.nextStep = CreateCharSteps.LANGUAGE;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.RACE;
                                    specificUser.lastStep = CreateCharSteps.RACE;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.GENDER;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }

                            break;
                        }
                    case CreateCharSteps.LANGUAGE: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<Languages>()) {
                                    specificUser.Language = (Languages)selection;
                                    specificUser.currentStep = CreateCharSteps.SKIN_TYPE;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.LANGUAGE;
                                    specificUser.lastStep = CreateCharSteps.LANGUAGE;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.RACE;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.SKIN_TYPE: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<SkinType>()) {
                                    specificUser.SkinType = (SkinType)selection;
                                    specificUser.currentStep = CreateCharSteps.SKIN_COLOR;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.SKIN_TYPE;
                                    specificUser.lastStep = CreateCharSteps.SKIN_TYPE;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.LANGUAGE;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.SKIN_COLOR: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<SkinColors>()) {
                                    specificUser.SkinColor = (SkinColors)selection;
                                    specificUser.currentStep = CreateCharSteps.HAIR_COLOR;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.SKIN_COLOR;
                                    specificUser.lastStep = CreateCharSteps.SKIN_COLOR;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.SKIN_TYPE;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.HAIR_COLOR: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<HairColors>()) {
                                    specificUser.HairColor = (HairColors)selection;
                                    specificUser.currentStep = CreateCharSteps.EYE_COLOR;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.HAIR_COLOR;
                                    specificUser.lastStep = CreateCharSteps.HAIR_COLOR;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.SKIN_COLOR;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.EYE_COLOR: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<EyeColors>()) {
                                    specificUser.EyeColor = (EyeColors)selection;
                                    specificUser.currentStep = CreateCharSteps.BUILD;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.EYE_COLOR;
                                    specificUser.lastStep = CreateCharSteps.EYE_COLOR;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.HAIR_COLOR;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.BUILD: {
                            if (String.Compare(response.Substring(0, 1), "b", true) != 0) {
                                int selection = 0;
                                int.TryParse(response, out selection);
                                selection--;
                                if (selection >= 0 && selection <= GetMaxEnum<BodyBuild>()) {
                                    specificUser.Build = (BodyBuild)selection;
                                    specificUser.currentStep = CreateCharSteps.WEIGHT;
                                    specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                                }
                                else {
                                    specificUser.currentStep = CreateCharSteps.BUILD;
                                    specificUser.lastStep = CreateCharSteps.BUILD;
                                }
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.EYE_COLOR;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            break;
                        }
                    case CreateCharSteps.WEIGHT: {
                            double temp = 0;
                            double.TryParse(response, out temp);
                            var doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(_generalCollection, w => w["_id"] == "BodyWeight");
                            BsonArray arr = doc["Genders"].AsBsonArray;
                            BsonArray arr2 = arr.Where(a => a["type"].AsString == specificUser.Gender.ToString().CamelCaseWord()).SingleOrDefault()["Weights"].AsBsonArray;
                            doc = arr2.Where(a => a.AsBsonDocument["name"] == specificUser.Build.ToString().CamelCaseWord()).SingleOrDefault().AsBsonDocument;
                            double min = doc["min"].AsInt32; //these need to be converted to doubles in DB
                            double max = doc["max"].AsInt32;

                            if (temp >= min && temp <= max) {
                                specificUser.Weight = temp;
                                specificUser.currentStep = CreateCharSteps.HEIGHT;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.WEIGHT;
                                specificUser.lastStep = CreateCharSteps.WEIGHT;
                            }
                            break;
                        }
                    case CreateCharSteps.HEIGHT: {
                            double temp = 0;
                            double.TryParse(response, out temp);
                            //get the min and max height for each race from DB and validate
                            BsonDocument doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(_generalCollection, h => h["_id"] == "Heights");
                            BsonArray arr = doc["Types"].AsBsonArray;
                            double min = 0.0d, max = 0.0d;

                            foreach (BsonDocument height in arr) {
                                if (height["Name"] == specificUser.Race.ToString().ToUpper()) {
                                    min = height["Min"].AsDouble; //these need to be converted to doubles in DB
                                    max = height["Max"].AsDouble;
                                    break;
                                }
                            }

                            if (temp >= min && temp <= max) {
                                specificUser.Height = temp;
                                specificUser.currentStep = CreateCharSteps.SUCCEEDED;
                                specificUser.lastStep = CreateCharSteps.AWAITINGRESPONSE;
                            }
                            else {
                                specificUser.currentStep = CreateCharSteps.HEIGHT;
                                specificUser.lastStep = CreateCharSteps.HEIGHT;
                            }

                            break;
                        }
                    default:
                        //something has gone terribly wrong if we get here
                        break;
                }
            }
            return state;
        }

        public string ExecuteScript(string userId) {
            string message = null;
            TempChar specificUser = usersCreatingChars.Where(u => u.user.UserID == userId).SingleOrDefault();

            if (specificUser.confirmStep != CreateCharSteps.NONE && specificUser.currentStep != CreateCharSteps.AWAITINGRESPONSE) {
                switch (specificUser.confirmStep) {
                    case CreateCharSteps.CLASS:
                        message = DisplayClassInfo(specificUser.Class);
                        break;
                    case CreateCharSteps.RACE:
                        message = DisplayRaceInfo(specificUser.Race);
                        break;
                }
                specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                return message;
            }

            if (specificUser != null && specificUser.lastStep != specificUser.currentStep) {
                switch (specificUser.currentStep) {
                    case CreateCharSteps.FIRST_NAME:
                        message = AskForFirstName();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.FIRST_NAME;
                        break;
                    case CreateCharSteps.LAST_NAME:
                        message = AskForLastName();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.LAST_NAME;
                        break;
                    case CreateCharSteps.PASSWORD:
                        message = AskForPassword();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.PASSWORD;
                        break;
                    case CreateCharSteps.PASSWORD_CHECK:
                        message = ReEnterPassword();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.PASSWORD_CHECK;
                        break;
                    case CreateCharSteps.CLASS:
                        message = AskForClass();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.CLASS;
                        break;
                    case CreateCharSteps.RACE:
                        message = AskForRace();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.RACE;
                        break;
                    case CreateCharSteps.GENDER:
                        message = AskForGender();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.GENDER;
                        break;
                    case CreateCharSteps.LANGUAGE:
                        message = AskForLanguage();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.LANGUAGE;
                        break;
                    case CreateCharSteps.SKIN_TYPE:
                        message = AskForSkinType();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.SKIN_TYPE;
                        break;
                    case CreateCharSteps.SKIN_COLOR:
                        message = AskForSkinColor();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.SKIN_COLOR;
                        break;
                    case CreateCharSteps.BUILD:
                        message = AskForBuildType();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.BUILD;
                        break;
                    case CreateCharSteps.HAIR_COLOR:
                        message = AskForHairColor();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.HAIR_COLOR;
                        break;
                    case CreateCharSteps.EYE_COLOR:
                        message = AskForEyeColor();
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.EYE_COLOR;
                        break;
                    case CreateCharSteps.WEIGHT:
                        message = AskForWeight(specificUser.Gender, specificUser.Build);
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.WEIGHT;
                        break;
                    case CreateCharSteps.HEIGHT:
                        message = AskForHeight(specificUser.Race);
                        specificUser.currentStep = CreateCharSteps.AWAITINGRESPONSE;
                        specificUser.lastStep = CreateCharSteps.HEIGHT;
                        break;
                    case CreateCharSteps.SUCCEEDED:
                        IActor newChar = new Character.Character(specificUser.Race, specificUser.Class, specificUser.Gender, specificUser.Language, specificUser.SkinColor, specificUser.SkinType, specificUser.HairColor, specificUser.EyeColor, specificUser.Build);
                        newChar.FirstName = specificUser.FirstName;
                        newChar.LastName = specificUser.LastName;
                        newChar.Weight = specificUser.Weight;
                        newChar.Height = specificUser.Height;
                        newChar.Password = specificUser.Password;
                        newChar.Save(); //this creates the ID
                        specificUser.user.Player.ID = newChar.ID;
                        specificUser.user.UserID = specificUser.user.Player.ID;

                        specificUser.user.Player.Load(specificUser.user.UserID);
                        AssignStatPoints(specificUser.user.Player);
                        specificUser.user.Player.Save(); //we updated the stats now save them.
                        message = "Character created!  Welcome " + specificUser.user.Player.FirstName + " " + specificUser.user.Player.LastName + "!";
                        specificUser.user.CurrentState = UserState.TALKING;
                        specificUser.user.InBuffer = "look\r\n";
                        usersCreatingChars.Remove(specificUser);
                        break;
                    case CreateCharSteps.AWAITINGRESPONSE:
                    default:
                        break;
                }
            }
            else {
                if (specificUser != null) {
                    if (specificUser.currentStep == CreateCharSteps.LAST_NAME) {
                        message = "A character with that name combination already exists! Select a different last name or name combination.";
                    }
                    else if (specificUser.lastStep == CreateCharSteps.PASSWORD) {
                        message = "Passwords did not match!  Please try again.";
                        specificUser.currentStep = CreateCharSteps.PASSWORD;
                    }
                    else if (specificUser.currentStep == CreateCharSteps.RACE || specificUser.currentStep == CreateCharSteps.GENDER || specificUser.currentStep == CreateCharSteps.LANGUAGE ||
                       specificUser.currentStep == CreateCharSteps.SKIN_TYPE || specificUser.currentStep == CreateCharSteps.SKIN_COLOR || specificUser.currentStep == CreateCharSteps.BUILD) {
                        message = "Invalid selection! Please try again.";
                    }
                    else if (specificUser.currentStep == CreateCharSteps.WEIGHT) {
                        message = "That is an invalid weight for the body type selected! Please choose a weight within the range.";
                    }

                    specificUser.lastStep = CreateCharSteps.NONE;
                }
            }

            return message;
        }

        internal string DisplayConfirmSelection() {
            return "Confirm this selection? (Y/N)";
        }

        internal bool ConfirmSelection(string response) {
            if (!string.IsNullOrEmpty(response) && response.ToUpper()[0] == 'Y') return true;
            return false;
        }

        internal async Task<BsonDocument> GrabFromDatabase(string database, string collection, string keyMatch, string valueMatch) {
            return await MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>(database, collection), o => o[keyMatch] == valueMatch);
        }

        internal async void AssignStatPoints(IActor specificUser) {
            var document = await MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("Characters", "General"), d => d["_id"] == "Class");
            //This is where we adjust the attributes
            AdjustClassPoints(specificUser, document);

            document = await MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("Characters", "General"), d => d["_id"] == "Race");
            AdjustRacePoints(specificUser, document);
            //not sure about these two below
            //AdjustSkinPoints(specificUser, document); endurance/dexterity?
            //AdjustBuildPoints(specificUser, document); strength/dexterity/endurance?

            //increase the max to reflect the new values
            IncreaseAttributeMaxToValues(specificUser);
        }

        internal void AdjustClassPoints(IActor specificUser, BsonDocument document) {
            var classes = document["Classes"].AsBsonArray;
            foreach (BsonDocument doc in classes) {
                if (doc["Name"].AsString == specificUser.Class) {
                    specificUser.ApplyEffectOnAttribute("Strength", doc["Strength"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Dexterity", doc["Dexterity"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Endurance", doc["Endurance"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Charisma", doc["Charisma"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Intelligence", doc["Intelligence"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Hitpoints", doc["Hitpoints"].AsDouble);
                    break;
                }
            }
        }

        internal void AdjustRacePoints(IActor specificUser, BsonDocument document) {
            var races = document["Races"].AsBsonArray;
            foreach (BsonDocument doc in races) {
                if (doc["Name"].AsString == specificUser.Race) {
                    specificUser.ApplyEffectOnAttribute("Strength", doc["Strength"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Dexterity", doc["Dexterity"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Endurance", doc["Endurance"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Charisma", doc["Charisma"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Intelligence", doc["Intelligence"].AsDouble);
                    specificUser.ApplyEffectOnAttribute("Hitpoints", doc["Hitpoints"].AsDouble);
                    break;
                }
            }
        }

        internal void IncreaseAttributeMaxToValues(IActor specificUser) {
            foreach (var attrib in specificUser.GetAttributes()) {
                attrib.Value.Max = attrib.Value.Value;
            }
        }

        internal bool ValidatePlayerPassword(string userID, string response) {
            string temp = usersCreatingChars.Where(u => u.user.UserID == userID).Select(u => u.Password).SingleOrDefault();
            if (String.Compare(temp, response, false) == 0) {
                return true;
            }

            return false;
        }

        internal bool ValidatePlayerName(string userID, string response) {
            string temp = usersCreatingChars.Where(u => u.user.UserID == userID).Select(u => u.FirstName).SingleOrDefault();

            var found = MongoUtils.MongoData.RetrieveObject<Character.Character>(MongoUtils.MongoData.GetCollection<Character.Character>("Characters", "PlayerCharacter"), c => c.FirstName.ToLower() == temp.ToLower() && c.LastName.ToLower() == response.ToLower());

            if (found != null) {
                return false; //uh-oh someone has that name already
            }

            return true;
        }



        internal string DisplayClassInfo(CharacterClass charClass) {
            BsonDocument doc = GrabFromDatabase("Characters", "General", "_id", "ClassInfo").Result;
            StringBuilder sb = DisplayInfo(doc, charClass.ToString());
            sb.AppendLine(DisplayConfirmSelection());

            return sb.ToString();
        }

        internal string DisplayRaceInfo(CharacterRace charRace) {
            BsonDocument doc = GrabFromDatabase("Characters", "General", "_id", "RaceInfo").Result;
            StringBuilder sb = DisplayInfo(doc, charRace.ToString());

            sb.AppendLine(DisplayConfirmSelection());

            return sb.ToString();
        }

        internal StringBuilder DisplayInfo(BsonDocument doc, string charInfoToGrab) {
            StringBuilder sb = new StringBuilder();

            foreach (BsonDocument info in doc["info"].AsBsonArray) {
                if (info["Name"].AsString != charInfoToGrab.ToUpper()) {
                    continue;
                }

                sb.AppendLine("\nDescription:\n\r" + info["Description"].AsString + "\n\r");
                foreach (BsonDocument stats in info["Stats"].AsBsonArray) {
                    sb.AppendLine(stats["Name"].AsString + ": " + (stats["Value"] > 0 ? "+" : "") + stats["Value"]);
                }

                break;
            }

            return sb;
        }

        #region Ask for stuff
        internal string AskForFirstName() {
            return "Enter a first name: ";
        }

        internal string AskForLastName() {
            return "Enter a last name (Type 'name' to go back to first name): ";
        }

        internal string AskForPassword() {
            return "Enter a password: ";
        }

        internal string AskForClass() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your class");
            sb.Append(DisplayChoices<CharacterClass>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForLanguage() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your primary language");
            sb.Append(DisplayChoices<Languages>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForSkinType() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your skin type");
            sb.Append(DisplayChoices<SkinType>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForSkinColor() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your skin color");
            sb.Append(DisplayChoices<SkinColors>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForBuildType() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your body build type");
            sb.Append(DisplayChoices<BodyBuild>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForHairColor() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your hair color");
            sb.Append(DisplayChoices<HairColors>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }
        internal string AskForEyeColor() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your eye color");
            sb.Append(DisplayChoices<EyeColors>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForWeight(Genders gender, BodyBuild build) {
            //will probably want to do some logic for the weight ranges and calculate them on the fly based on build, height and race?
            BsonDocument doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(_generalCollection, g => g["_id"] == "BodyWeight");
            BsonArray arr = doc["Genders"].AsBsonArray;
            BsonArray arr2 = arr.Where(a => a["type"].AsString == gender.ToString().CamelCaseWord()).SingleOrDefault()["Weights"].AsBsonArray;
            doc = arr2.Where(a => a.AsBsonDocument["name"] == build.ToString().CamelCaseWord()).SingleOrDefault().AsBsonDocument;
            double min = doc["min"].AsInt32; //these need to be converted to doubles in DB
            double max = doc["max"].AsInt32;

            return "Enter your weight (range:" + min + "-" + max + ")";
        }

        internal string AskForHeight(CharacterRace race) {
            //will probably want to do some logic for the weight ranges and calculate them on the fly based on build, height and race?
            BsonDocument doc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(_generalCollection, r => r["_id"] == "Heights");
            BsonArray arr = doc["Types"].AsBsonArray;
            double min = 0.0d, max = 0.0d;

            foreach (BsonDocument height in arr) {
                if (height["Name"] == race.ToString().ToUpper()) {
                    min = height["Min"].AsDouble; //these need to be converted to doubles in DB
                    max = height["Max"].AsDouble;
                    break;
                }
            }

            return "Enter your height (inches)(range:" + min + "-" + max + ")";
        }

        internal string AskForRace() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your race");
            sb.Append(DisplayChoices<CharacterRace>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string AskForGender() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Select your gender");
            sb.Append(DisplayChoices<Genders>());
            sb.AppendLine("(B)ack");
            return sb.ToString();
        }

        internal string ReEnterPassword() {
            return "Re-enter your password: ";
        }
        #endregion Ask for stuff

        #region Enum Utility Functions
        internal string DisplayChoices<T>() {
            int i = 1;
            StringBuilder sb = new StringBuilder();
            foreach (T choice in Enum.GetValues(typeof(T))) {
                sb.AppendLine(String.Format("{0}) {1}", i, choice.ToString().Replace("_", "").CamelCaseString()));
                i++;
            }
            return sb.ToString();
        }

        internal IEnumerable<T> GetEnums<T>() {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        internal int GetMaxEnum<T>() {
            return Enum.GetValues(typeof(T)).Cast<int>().Last();
        }
        #endregion
    }

    internal enum CreateCharSteps { FIRST_NAME, LAST_NAME, PASSWORD, PASSWORD_CHECK, RACE, GENDER, LANGUAGE, EYE_COLOR, SKIN_TYPE, SKIN_COLOR, HAIR_COLOR, WEIGHT, HEIGHT, BUILD, CLASS, AWAITINGRESPONSE, SUCCEEDED, CONFIRM, NONE };

    internal class TempChar {
        public IUser user = null;
        public CreateCharSteps currentStep { get; set; }
        public CreateCharSteps lastStep { get; set; }
        public CreateCharSteps confirmStep { get; set; }
        public CreateCharSteps nextStep { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public CharacterRace Race { get; set; }
        public CharacterClass Class { get; set; }
        public Genders Gender { get; set; }
        public Languages Language { get; set; }
        public EyeColors EyeColor { get; set; }
        public SkinColors SkinColor { get; set; }
        public SkinType SkinType { get; set; }
        public HairColors HairColor { get; set; }
        public BodyBuild Build { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }

        public TempChar(IUser player) {
            this.user = player;
            currentStep = CreateCharSteps.FIRST_NAME;
            lastStep = CreateCharSteps.NONE;
            confirmStep = CreateCharSteps.NONE;
            nextStep = CreateCharSteps.NONE;
        }
    }
}
