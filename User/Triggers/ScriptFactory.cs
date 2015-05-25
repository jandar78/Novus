using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Triggers {
	public class ScriptFactory {
		public static IScript GetScript(string scriptID, string scriptCollection){
			IScript script = null;
			MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            
			if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptTypes scriptType = (ScriptTypes)Enum.Parse(typeof (ScriptTypes), doc["Type"].ToString());
				switch (scriptType) {
				case ScriptTypes.Lua:
					script = new LuaScript((byte[])doc["Bytes"].AsBsonBinaryData);
					break;
				case ScriptTypes.Roslyn:
					script = new RoslynScript((byte[])doc["Bytes"].AsBsonBinaryData);
					break;
				default:
					break;
				}
			}

			return script;
		}

		public enum ScriptTypes { Lua, Roslyn, None };

	}
}
