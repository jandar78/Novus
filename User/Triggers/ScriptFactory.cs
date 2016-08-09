using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Interfaces;

namespace Triggers {
	public class ScriptFactory {
		public static IScript GetScript(string scriptID, string scriptCollection){
			IScript script = null;
			MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            
            script = GetScript((byte[])doc["Bytes"].AsBsonBinaryData, (ScriptTypes)Enum.Parse(typeof(ScriptTypes), doc["Type"].ToString()));
			return script;
		}

        public static IScript GetScript(byte[] scriptBytes, ScriptTypes scriptType) {
            IScript script = null;
            
            if (scriptBytes != null) {
                switch (scriptType) {
                    case ScriptTypes.Lua:
                        script = new LuaScript(scriptBytes);
                        break;
                    case ScriptTypes.Roslyn:
                        script = new RoslynScript(scriptBytes);
                        break;
                    default:
                        break;
                }
            }

            return script;
        }

        public static IScript GetScript(BsonDocument scriptDocument) {
            IScript script = null;
            if (scriptDocument != null && scriptDocument["Bytes"].AsBsonBinaryData != null) {
                script = GetScript((byte[])scriptDocument["Bytes"].AsBsonBinaryData, (ScriptTypes)Enum.Parse(typeof(ScriptTypes), scriptDocument["Type"].ToString()));
            }

            return script;
        }


      //  public enum ScriptTypes { Lua, Roslyn, None };

	}
}
