using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Interfaces;
using Extensions;

namespace Scripts {
    public class LevelUpScript : ScriptBase {
         private static List<TempLvlChar> usersLevelingUp = new List<TempLvlChar>();

		 public static LevelUpScript levelUpScript = null;

         public static LevelUpScript GetScript() {
             return levelUpScript ?? (levelUpScript = new LevelUpScript());
		 }

         public void AddUserToScript(IUser user) {
             var temp = usersLevelingUp.Where(u => u.user.UserID == user.UserID).SingleOrDefault();
             if (temp == null){
                 usersLevelingUp.Add(new TempLvlChar(user));
             }
         }


		 public UserState InsertResponse(string response, ObjectId userId) {
             UserState state = UserState.LEVEL_UP;
             if (string.IsNullOrEmpty(response)) return state;
			 
			 TempLvlChar currentUser = usersLevelingUp.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault();
			 currentUser.Response = response;
			 var stepDoc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("Scripts", "LevelUp"), s => s["_id"] == currentUser.lastStep.ToString());

            if (currentUser != null && currentUser.currentStep == ScriptSteps.AwaitingResponse) {
				 state = (UserState)ParseStepDocument(stepDoc, currentUser, levelUpScript);
			 }
			 currentUser.Response = "";
             return state;
         }

         public string ExecuteScript(ObjectId userId) {
             string message = "";

			 TempLvlChar currentUser = usersLevelingUp.Where(u => u.user.UserID == userId).SingleOrDefault();
			 //get the document for the step
			 var stepDoc = MongoUtils.MongoData.RetrieveObject<BsonDocument>(MongoUtils.MongoData.GetCollection<BsonDocument>("Scripts", "LevelUp"), s => s["_id"] == currentUser.lastStep.ToString());

			 if (currentUser != null && currentUser.lastStep != currentUser.currentStep && currentUser.currentStep != ScriptSteps.AwaitingResponse) {
				 message = (string)ParseStepDocument(stepDoc, currentUser, levelUpScript);
			 }
			 else if (currentUser.currentStep != ScriptSteps.AwaitingResponse) {
				 if (currentUser != null) {
					 if (currentUser.currentStep == ScriptSteps.Step1) {
						 message = "That is not a valid selection.";
					 }
				 }
			 }
			 
             return message;
         }

		 private void ExitScript(TempLvlChar currentUser) {
			 currentUser.user.CurrentState = UserState.TALKING;
			 usersLevelingUp.Remove(currentUser);
		 }

		 private UserState IncreaseStatResponse(string response, TempLvlChar currentUser) {
			 UserState state = UserState.LEVEL_UP;
			 int stat = -1;
			 decimal increase = 0m;
			 int.TryParse(response, out stat);
			 if (stat >= 1 && stat <= currentUser.maxOptions + 1) {
				 string attribute = "";
				 var attributesPlayerHas = currentUser.user.Player.GetAttributes();
				 int i = 1;

				 foreach (var index in attributesPlayerHas) {
					 if (i == stat) {
						 attribute = index.Name;
						 break;
					 }
					 i++;
				 }

				 if (!string.IsNullOrEmpty(attribute)) {
					 increase = RankIncrease(currentUser, attribute);
				 }
				 else {
					 //player chose to quit while still having points to spend
					 state = UserState.TALKING;
					 usersLevelingUp.Remove(currentUser);
					 return state;
				 }

				 if (increase > 0) {
					 currentUser.user.MessageHandler(String.Format("You've increased your {0} by {1} points", attribute, increase));
				 }
				 else {
					 currentUser.user.MessageHandler("You don't have enough points to increase the rank of " + attribute);
					 //this will put us back at the level up stats page
					 currentUser.currentStep = ScriptSteps.Step1;
					 currentUser.lastStep = ScriptSteps.None;
				 }
				 if (currentUser.user.Player.PointsToSpend == 0) {
					 state = UserState.TALKING;
					 usersLevelingUp.Remove(currentUser);
					 currentUser.user.MessageHandler("");

				 }
			 }
			 else {
				 currentUser.currentStep = ScriptSteps.Step1;
				 currentUser.lastStep = ScriptSteps.None;
			 }

			 return state;
		 }
		 
			
		 //private static object ParseStepDocument(BsonDocument stepDoc, TempLvlChar currentUser) {
		 //	object returnObject = null;
		 //	//if we have a message pass it to the message handler.
		 //	if (!string.IsNullOrEmpty(stepDoc["Message"].AsString)) {
		 //		currentUser.user.MessageHandler(stepDoc["Message"].AsString);
		 //	}

		 //	//we have a method we want to run, time to do some reflection
		 //	if (stepDoc["MethodToRun"].AsBsonArray.Count > 0) {
		 //		foreach (BsonDocument methodDoc in stepDoc["MethodToRun"].AsBsonArray) {
		 //			Type t = levelUpScript.GetType();
		 //			System.Reflection.MethodInfo method = t.GetMethod(methodDoc["Name"].AsString, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		 //			if (method != null) {						
		 //			   returnObject = method.Invoke(levelUpScript, GetParameters(methodDoc["Parameters"].AsBsonArray, t, currentUser));
		 //			}
		 //		}
		 //	}

		 //	currentUser.lastStep = currentUser.currentStep;
		 //	currentUser.currentStep = (ScriptSteps)Enum.Parse(typeof(ScriptSteps), stepDoc["NextStep"].AsString);

		 //	return returnObject;
		 //}

		 //private static object[] GetParameters(BsonArray parameterArray, Type thisType, TempLvlChar specificUser) {
		 //	List<object> parameters = new List<object>();
		 //	foreach (BsonDocument doc in parameterArray) {
		 //		if (string.Equals(doc["Name"].AsString, "CurrentUser", StringComparison.InvariantCultureIgnoreCase)){
		 //			parameters.Add(specificUser);
		 //			continue;
		 //		}
		 //		//the parameters for any of the methods being called should be available in this containing class
		 //		System.Reflection.PropertyInfo p = thisType.GetProperty(doc["Name"].AsString, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
		 //		if (p != null) {
		 //			parameters.Add(p.GetValue(null,null));
		 //		}
		 //	}

		 //	return parameters.ToArray();
		 //}

         private int RankIncrease(TempLvlChar specificUser, string attributeName) {
             double addToMax = 0.0d;

             if (specificUser.user.Player.PointsToSpend >= GetRankCost(specificUser.user.Player.GetAttributes(), attributeName)) {
                 specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().IncreaseRank();
                 specificUser.user.Player.PointsToSpend = -GetRankCost(specificUser.user.Player.GetAttributes(), attributeName);
                 double max = specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().Max;
                 int rank = specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().Rank;
                 double calculated = (double)rank / 10;
                 addToMax = max * calculated;
                 specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().IncreaseMax(addToMax);
                 specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().Value = specificUser.user.Player.GetAttributes().Where(a => a.Name == attributeName.CamelCaseWord()).Single().Max;
                 specificUser.user.Player.Save();
             }           
             
             return (int)Math.Round(addToMax, 2, MidpointRounding.AwayFromZero);
         }

         private string DisplayLevelStats(TempLvlChar user) {
             StringBuilder sb = new StringBuilder();
			    
             //The rank of the attribute also determines how many points it costs to increase it to the next rank.  The higher the rank the more expensive
             //the upgrade is limiting you to choose wisely.
			 
			 var attributesPlayerHas = user.user.Player.GetAttributes();
			 int i = 1;
			 sb.AppendLine("Level: " + user.user.Player.Level);
			 sb.AppendLine("Points Available: " + user.user.Player.PointsToSpend);

			 foreach (var attrib in attributesPlayerHas) {
				 sb.AppendLine(i + ") " + attrib.Name  + " : " + user.user.Player.GetAttributeValue(attrib.Name) + "\tCost: " + GetRankCost(user.user.Player.GetAttributes(), attrib.Name));
				 i++;
			 }
			 			
			 sb.AppendLine(i + ") Quit");
             sb.AppendLine("Which stat would you like to increase?: ");
             return sb.ToString();
         }

         private int GetRankCost(List<Character.Attribute> attributes, string attributeName) {
             int currentRank = attributes.Where(a => a.Name == attributeName.CamelCaseWord()).Single().Rank;
             //ranks cost more points as they increase
			 //TODO: these should come from the DB
             int cost = (int)Math.Ceiling(currentRank / 3.0d);          
             return cost;
         }
    }


   // public enum LevelUpSteps { STEP1, STEP2, STEP3, AWAITINGRESPONSE, NONE};

	public enum ScriptSteps {
		None,
		AwaitingResponse,
		Step1,
		Step2,
		Step3,
		Step4,
		Step5,
		Step6,
		Step7,
		Step8,
		Step9,
		Step10,
		Step11,
		Step12,
		Step13,
		Step14,
		Step15,
		Step16,
		Step17,
		Step18,
		Step19,
		Step20,
		Step21,
		Step22,
		Step23,
		Step24,
		Step25,
		Step26,
		Step27,
		Step28,
		Step29,
		Step30
	};

    public class TempLvlChar {
        public IUser user = null;
		public ScriptSteps currentStep;
		public ScriptSteps lastStep;
        public int maxOptions;

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
		public string Response { get; set; }

        public TempLvlChar(IUser player) {
            user = player;
			currentStep = ScriptSteps.None;
			lastStep = ScriptSteps.Step1;
            maxOptions = player.Player.GetAttributes().Count; 

        }

    }
}
