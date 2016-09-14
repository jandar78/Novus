using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts {
	interface IScript {
		 object ParseStepDocument<T>(BsonDocument stepDoc, TempLvlChar currentUser, T owningScript);
		 object[] GetParameters(BsonArray parameterArray, Type thisType, TempLvlChar specificUser);
	}
}
