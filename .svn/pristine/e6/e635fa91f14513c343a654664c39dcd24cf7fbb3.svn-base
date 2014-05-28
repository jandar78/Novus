using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Scripts
{
    public class Script
    {
		 private List<ScriptUsers> _userList; 
         private Dictionary<string, int> _enumerations;
        		 

         public Script() {
             _userList = new List<ScriptUsers>(); ;
             _enumerations = new Dictionary<string, int>();
         }

         public void AddUserToScript(User.User user) {
             _userList.Add(new ScriptUsers(user));
         }

         private void LoadSteps() {
             int step = 0;
             _enumerations.Add("NONE", 0);
             //get the rest of the steps from the DB
             //use step to keep incrementing as if it were a regular enum
         }

		 public User.User.UserState ExecuteResponse(string response, string userId) {
			 User.User.UserState state = User.User.UserState.LOGGING_IN;

             ScriptUsers specificUser = _userList.Where(u => u.user.UserID == userId).SingleOrDefault();
			 
			 if (specificUser != null && specificUser.currentStep != "NONE") {
				 //a switch won't work here, what needs ot happen is we go to the DB and get the logic for this current step
                 //that will do something based on th euser input
			 }
			 return state;
		 }

		 public string ExecuteDisplay(string userId) {
			 string message = null;
             ScriptUsers specificUser = _userList.Where(u => u.user.UserID == userId).SingleOrDefault();
			 if (specificUser != null && specificUser.lastStep != specificUser.currentStep) {
                 //the logic here would execute whatever logic the current step has that will display something to the user
			 }
			 return message;
		 }


    }

	internal class ScriptUsers{
		public User.User user = null;
		public string currentStep;
		public string lastStep;

		public ScriptUsers(User.User user) {
			this.user = user;
            currentStep = "NONE";
			lastStep = "NONE";
		}
	}
}
