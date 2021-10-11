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
	public class TriggerScriptFactory {
		public static IScript GetScript(string scriptID, string scriptCollection){
			IScript script = null;
			var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("Scripts", scriptCollection);
            var doc = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(collection, s => s["_id"] == scriptID).Result;
            if (doc != null) 
            {
                script = GetScript((byte[])doc["Bytes"].AsBsonBinaryData, (ScriptTypes)Enum.Parse(typeof(ScriptTypes), doc["Type"].ToString()));
            }
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

        public static IScript CreateScript(ScriptTypes type) {
            IScript newScript;

            switch (type) {
                case ScriptTypes.Roslyn:
                    newScript = new RoslynScript();
                    break;
                case ScriptTypes.Lua:
                default:
                    newScript = new LuaScript();
                    break;
            }

            return newScript;
        }

      //  public enum ScriptTypes { Lua, Roslyn, None };

	}
}
