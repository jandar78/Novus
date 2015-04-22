using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Scripts {
	public abstract class ScriptBase : IScript {
		public object ParseStepDocument<T>(BsonDocument stepDoc, TempLvlChar currentUser, T owningScript) {
			BsonArray documentToUse = null;
			object returnObject = null;
			//if we have a message pass it to the message handler.
			if (!string.IsNullOrEmpty(stepDoc["Message"].AsString)) {
				currentUser.user.MessageHandler(stepDoc["Message"].AsString);
			}

			if (currentUser.currentStep != ScriptSteps.AwaitingResponse) {
				documentToUse = stepDoc["MethodToRun"].AsBsonArray;
			}
			else {
				documentToUse = stepDoc["AwaitingResponse"].AsBsonArray;
				documentToUse = documentToUse[0].AsBsonDocument["MethodToRun"].AsBsonArray;
			}

			//we have a method we want to run, time to do some reflection
			if (documentToUse.Count > 0) {
				foreach (BsonDocument methodDoc in documentToUse) {
					Type t = owningScript.GetType();
					System.Reflection.MethodInfo method = t.GetMethod(methodDoc["Name"].AsString, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					if (method != null) {
						returnObject = method.Invoke(owningScript, GetParameters(methodDoc["Parameters"].AsBsonArray, t, currentUser));
					}
				}
			}

			if (currentUser.currentStep != ScriptSteps.None) {
				currentUser.lastStep = currentUser.currentStep;
			}
			//this method can be called for either InsertResponse or ExecuteScript so we want to set the current step accordingly
			if (currentUser.currentStep == ScriptSteps.AwaitingResponse) {
				currentUser.currentStep = (ScriptSteps)Enum.Parse(typeof(ScriptSteps), stepDoc["NextStep"].AsString);
			}
			else {
				currentUser.currentStep = ScriptSteps.AwaitingResponse;
			}

			return returnObject;
		}

		public object[] GetParameters(BsonArray parameterArray, Type thisType, TempLvlChar specificUser) {
			List<object> parameters = new List<object>();
			foreach (BsonDocument doc in parameterArray) {
				if (string.Equals(doc["Name"].AsString, "CurrentUser", StringComparison.InvariantCultureIgnoreCase)) {
					parameters.Add(specificUser);
					continue;
				}
				if (string.Equals(doc["Name"].AsString, "Response", StringComparison.InvariantCultureIgnoreCase)) {
					parameters.Add(specificUser.Response);
					continue;
				}
				//the parameters for any of the methods being called should be available in this containing class
				System.Reflection.PropertyInfo p = thisType.GetProperty(doc["Name"].AsString, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				if (p != null) {
					parameters.Add(p.GetValue(null, null));
				}
			}

			return parameters.ToArray();
		}
	}
}
