using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using LuaInterface;
using System.Reflection;
using System.Collections;
using Roslyn.Compilers;
using Roslyn.Scripting.CSharp;

namespace Triggers {

    public class RoslynScript : Script {

		private Roslyn.Scripting.Session _session;
		private ScriptEngine _engine;
		private ScriptMethods _scriptMethods;
		
		public ScriptEngine Engine {
			get {
				if (_engine == null) {
					_engine = new ScriptEngine();
				}
				return _engine;
			}
		}

		public ScriptMethods ScriptMethod {
			get {
				if (_scriptMethods == null) {
					_scriptMethods = new ScriptMethods();
				}
				return _scriptMethods;
			}
			set {
				_scriptMethods = value;
			}
		}

		public Roslyn.Scripting.Session Session {
			get {
				if (_session == null) {					
					_session = Engine.CreateSession(ScriptMethod, ScriptMethod.GetType());
				}
				return _session;
			}
		}

		public override ScriptTypes ScriptType {
			get {
				return ScriptTypes.Roslyn;
			}
		}

        public RoslynScript() { }
        /// <summary>
        /// Pass in FALSE for registerMethods if you would like to provide the methods to register by calling LuaScript.
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="registerMethods"></param>
        public RoslynScript(string scriptID, string scriptCollection, bool registerMethods = true) {
            MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptByteArray = (byte[])doc["Bytes"].AsBsonBinaryData;
				if (registerMethods) {
					RegisterMethods();
				}
            }
        }

        public RoslynScript(byte[] scriptBytes, bool registerMethods = true) {
			ScriptByteArray = scriptBytes;
            if (registerMethods) {
				RegisterMethods();
            }
        }

		public void RegisterMethods() {
			new[]
					{
						 typeof (Type).Assembly,
						 typeof (ICollection).Assembly,
						 typeof (Console).Assembly,
						 typeof (RoslynScript).Assembly,
						 typeof (IEnumerable<>).Assembly,
						 typeof (IQueryable).Assembly,
						 typeof (ScriptMethods).Assembly,
						 typeof(Character.Iactor).Assembly,
						 typeof(Character.Character).Assembly,
						 typeof(Character.NPC).Assembly,
						 typeof(ClientHandling.Message).Assembly,
						 GetType().Assembly
					}.ToList().ForEach(asm => Engine.AddReference(asm));

			//Import common namespaces
			new[]
					{
						 "System", "System.Linq",
						 "System.Collections",
						 "System.Collections.Generic",
						 "System.Text","System.Threading.Tasks","System.IO",
                         "Character", "Rooms", "Items", "ClientHandling"
					 }.ToList().ForEach(ns => Engine.ImportNamespace(ns));
		}

        ~RoslynScript(){
            if (_engine != null) {
				_engine = null;
			}
			if (_session != null) {
				_session = null;
			}
        }
        
        public override void RunScript() {
			using (MemStream = new MemoryStream(ScriptByteArray)) {
				if (_memStream != null) {
					string code = MemStreamAsString;
					try {
						//	var result = Session.CompileSubmission<object>(MemStreamAsString);
						Session.Execute(MemStreamAsString);
					}
					catch { }
				}
			}
        }

        public override void AddVariable(object variable, string variableName) {
			//need to figure out a way to add variables to the session or it may just be something that happens from the
			//script code by calling the scriptmethods we provide.  We might just add variables like player/item ID's
			if (variable != null) {
				if (variable.ToString().Contains("\"")) {
					variable = variable.ToString().Replace("\"", "\\\"");
				}
				ScriptMethod.SetVariable(variableName, variable);
			}
        }

		public void AddNamespace(string nameSpace) {
			Session.ImportNamespace(nameSpace);
		}

		public void AddReference(Assembly assembly) {
			Session.AddReference(assembly);
		}
	}


	//this will be for any scripts that we want builders to create.  It will have limited access to classes.
	public class LuaScript : Script {
        private Lua _lua;
		

		public Lua Engine {
			get {
				if (_lua == null) {
					_lua = new Lua();
				}
				return _lua;
			}
			set {
				_lua = value;
			}
		}

		public override ScriptTypes ScriptType {
			get {
				return ScriptTypes.Lua;
			}
		}

        public LuaScript() { }
        /// <summary>
        /// Pass in FALSE for registerMethods if you would like to provide the methods to register by calling LuaScript.
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="registerMethods"></param>
        public LuaScript(string scriptID, string scriptCollection, bool registerMethods = true) {
            MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptByteArray = (byte[])doc["Bytes"].AsBsonBinaryData;
				if (registerMethods) {
					Engine.RegisterMarkedMethodsOf(new ScriptMethods());
				}
            }
        }

        public LuaScript(byte[] scriptBytes, bool registerMethods = true) {
			ScriptByteArray = scriptBytes;
            if (registerMethods) {
				Engine.RegisterMarkedMethodsOf(new ScriptMethods());
            }
        }

        ~LuaScript(){
            if (_memStream != null) {
                _memStream.Dispose();
            }
			if (_lua != null) {
				_lua.Dispose();
			}
			
        }
        
        public override void RunScript() {
			using (MemStream = new MemoryStream(ScriptByteArray)) {
				if (_memStream != null) {
					Engine.DoString(MemStreamAsString);
				}
			}
        }

        public override void AddVariable(object variable, string variableName) {
			Engine[variableName] = variable;
        }

		public void RegisterFunction(string path, object registerClass, MethodBase function) {
			Engine.RegisterFunction(path, registerClass, function);
		}

		public void RegisterMarkedMethodsOf(object classObject) {
			Engine.RegisterMarkedMethodsOf(classObject);
		}
	}


	public abstract class Script : IScript {
		protected MemoryStream _memStream;
		protected MemoryStream MemStream {
			get {
				if (_memStream == null) {
					_memStream = new MemoryStream();
				}
				return _memStream;
			}
			set {
				_memStream = value;
			}
		}

		public static void SaveScriptToDatabase(string scriptID, string scriptText) {
			MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", "Action");
			BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));

			BsonBinaryData bytesArray = new BsonBinaryData(ASCIIEncoding.ASCII.GetBytes(scriptText));

			if (doc == null) {
				doc.Add("_id", scriptID);
				doc.Add("Bytes", bytesArray);
			}
			else {
				doc["Bytes"] = bytesArray;
			}

			collection.Save(doc);
		}

		public static string GetScriptFromDatabase(string scriptID) {
			string result = null;
			MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", "Action");
			BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));

			if (doc != null) {
				result = ASCIIEncoding.ASCII.GetString((byte[])doc["Bytes"].AsBsonBinaryData);
			}

			return result;
		}


		//added this because if we just initialize a the memory stream then we have very serious (crashing) issue when saving the items.
		//the memory stream causes a "timeout not supported by stream" exception.  It wasn't fun to track down or figure out.
		protected byte[] ScriptByteArray {
			get;
			set;
		}

		public string MemStreamAsString {
			get {
				return System.Text.ASCIIEncoding.ASCII.GetString(ScriptByteArray);
			}
		}

		public virtual ScriptTypes ScriptType {
			get {
				return ScriptTypes.None;
			}
		}

		protected byte[] MemStreamAsByteArray {
			get {
				return ScriptByteArray;
			}
		}

		public virtual void AddVariable(object variable, string variableName) {
			throw new NotImplementedException();
		}

		public virtual void RunScript() {
			throw new NotImplementedException();
		}

		~Script() {
			if (_memStream != null) {
				_memStream.Dispose();
			}
		}
	}

	
}
