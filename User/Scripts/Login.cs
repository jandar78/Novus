using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Extensions;
using Interfaces;
using MongoDB.Bson;

namespace Scripts
{
    public class Login
    {
		 private static List<UserScript> usersLoggingIn = new List<UserScript>();

		 public static Login loginScript = null;

		 public static Login GetScript() {
			 return loginScript ?? (loginScript = new Login());
		 }

		 public void AddUserToScript(IUser user){
			 usersLoggingIn.Add(new UserScript(user));
		 }

		 public UserState InsertResponse(string response, ObjectId userId) {
			 UserState state = UserState.LOGGING_IN; 
			 
			 UserScript specificUser = usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault();
			 
			 if (specificUser != null && specificUser.currentStep == Steps.AWAITINGRESPONSE) {
				 
				 switch (specificUser.lastStep) { 
                     case Steps.NAME:
						 if (ValidatePlayerName(specificUser.user.UserID, response)) {
							 userId = specificUser.user.UserID;
							 specificUser.user.LogID = specificUser.user.UserID; 
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().currentStep = Steps.PASSWORD;
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().lastStep = Steps.AWAITINGRESPONSE;
						 }
						 else {
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().currentStep = Steps.NAME;
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().lastStep = Steps.NAME;
						 }
						 break;
					 
					 case Steps.PASSWORD:
						 if (ValidatePlayerPassword(specificUser.user.UserID, response)) {
							 specificUser.currentStep = Steps.SUCCEEDED;
							 specificUser.lastStep = Steps.PASSWORD; 
						 }
						 else {
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().currentStep = Steps.PASSWORD;
							 usersLoggingIn.Where(u => u.user.UserID == userId).SingleOrDefault().lastStep = Steps.PASSWORD;
						 }
						 break;
                     case Steps.CREATECHAR:
                         if (String.Compare(response[0].ToString(), "y", true) == 0) {
                             state =UserState.CREATING_CHARACTER;
                             usersLoggingIn.Remove(specificUser);
                         }
                         else {
                             specificUser.currentStep = Steps.NAME;
                             specificUser.lastStep = Steps.NONE;
                         }
                         break;
					 default:
						 //something has gone terribly wrong if we get here
						 break;
				 }
			 }
			 return state;
		 }

		 public string ExecuteScript(ObjectId userId) {
			 string message = null;
			 UserScript specificUser = usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault();
			 if (specificUser != null && specificUser.lastStep != specificUser.currentStep) {
				 switch (specificUser.currentStep) {
                     case Steps.SPLASH:
                         message = SplashScreen();
                         usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().currentStep = Steps.NAME;
						 usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().lastStep = Steps.NONE;
                         break;
					 case Steps.NAME:
						 message = AskForFirstName();
						 usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().currentStep = Steps.AWAITINGRESPONSE;
						 usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().lastStep = Steps.NAME;
						 break;
					 case Steps.PASSWORD:
						 message = AskForPassword();
						 usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().currentStep = Steps.AWAITINGRESPONSE;
						 usersLoggingIn.Where(u => u.user.UserID.Equals(userId)).SingleOrDefault().lastStep = Steps.PASSWORD;
						 break;
					 case Steps.SUCCEEDED:
						 //let's see if they are connecting back to a limbo character first and set things right
						 IUser user = new Sockets.User();
						 user = Sockets.Server.GetAUserPlusState(specificUser.user.UserID, UserState.LIMBO);
						 if (user != null) {
							 Sockets.Server.UpdateUserSocket(specificUser.user.UserID);
							 specificUser.user = user; 
						 }
						 else {
                             specificUser.user.Player.Load(specificUser.user.Player.Id);
						 }
						 message = "Welcome " + specificUser.user.Player.FirstName + " " + specificUser.user.Player.LastName + "!";
						 specificUser.user.CurrentState = UserState.TALKING;
						 specificUser.user.InBuffer = "look\r\n";
						 usersLoggingIn.Remove(specificUser);
						 break;
					 case Steps.AWAITINGRESPONSE:
					 default:
						 break;
				 }
			 }
			 else {
				 if (specificUser != null && specificUser.currentStep == Steps.NAME) {
					 message = "No character with that name exists!\n\rDo you want to create a new character? (Y/N)";
					 specificUser.currentStep = Steps.AWAITINGRESPONSE;
                     specificUser.lastStep = Steps.CREATECHAR;
				 }
				 else if (specificUser != null && specificUser.currentStep == Steps.PASSWORD) {
					 message = "Incorrect Password!";
                     specificUser.lastStep = Steps.NONE;
				 }
			 }

			 return message;
		 }

		 private bool ValidatePlayerPassword(ObjectId userID, string response) {
			 var found = MongoUtils.MongoData.RetrieveObjectAsync<Character.Character>(MongoUtils.MongoData.GetCollection<Character.Character>("Characters", "PlayerCharacter"), c => c.Id.Equals(userID) && c.Password == response).Result;
			 if (found != null) {
				 return true;
			 }

			 return false;
		 }

        private bool ValidatePlayerName(ObjectId userID, string response) {
            string[] names = response.Split(' ');
            if (names.Count() != 2) {
                return false;
            }

            var found = MongoUtils.MongoData.RetrieveObjectAsync<Character.Character>(MongoUtils.MongoData.GetCollection<Character.Character>("Characters", "PlayerCharcater"), c => c.FirstName.ToLower() == names[0].ToLower() && c.LastName.ToLower() == names[1].ToLower()).Result;

            if (found != null) {
                //set the id to the ID of the one we found in the database for the PlayerCharacter to match up on the Password
                usersLoggingIn.Where(u => u.user.UserID == userID).SingleOrDefault().user.UserID = found.Id;
                userID = found.Id;
                usersLoggingIn.Where(u => u.user.UserID == userID).SingleOrDefault().user.Player.Id = userID;

                //this should prevent a single character from logging in more than once while that character is actively playing
                if ((usersLoggingIn.Where(u => u.user.UserID == userID).SingleOrDefault().user.CurrentState != UserState.TALKING &&
                    usersLoggingIn.Where(u => u.user.UserID == userID).SingleOrDefault().user.CurrentState != UserState.JUST_CONNECTED &&
                    usersLoggingIn.Where(u => u.user.UserID == userID).SingleOrDefault().user.CurrentState != UserState.CREATING_CHARACTER)) {
                    return true;
                }
            }
            return false;
        }

         private string SplashScreen() {
             string splash = @"        Welcome to                                                  
                  _        _______                    _______ 
                 ( (    /|(  ___  )|\     /||\     /|(  ____ \
                 |  \  ( || (   ) || )   ( || )   ( || (    \/
                 |   \ | || |   | || |   | || |   | || (_____ 
                 | (\ \) || |   | |( (   ) )| |   | |(_____  )
                 | | \   || |   | | \ \_/ / | |   | |      ) |
                 | )  \  || (___) |  \   /  | (___) |/\____) |
                 |/    )_)(_______)   \_/   (_______)\_______)

";
            splash = splash.FontColor(Utils.FontForeColor.BLUE).FontStyle(Utils.FontStyles.BOLD);
            return splash;
         }

		 private string AskForFirstName() {
			 return "Enter your characters full name (First Last): ";
		 }

		 private string AskForLastName() {
			 return "Enter your characters last name: ";
		 }

		 private string AskForPassword() {
			 return "Enter your password: ";
		 }
    }

   internal enum Steps { NAME, PASSWORD, AWAITINGRESPONSE, SUCCEEDED, CREATECHAR, SPLASH, NONE };

	internal class UserScript{
		public IUser user = null;
		public Steps currentStep;
		public Steps lastStep;

		public UserScript(IUser user) {
			this.user = user;
			currentStep = Steps.SPLASH;
			lastStep = Steps.NONE;
		}
	}
}
