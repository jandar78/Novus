using MongoDB.Bson;
using System;

namespace Scripts {
	interface IScript {
		 object ParseStepDocument<T>(BsonDocument stepDoc, TempLvlChar currentUser, T owningScript);
		 object[] GetParameters(BsonArray parameterArray, Type thisType, TempLvlChar specificUser);
	}
}
