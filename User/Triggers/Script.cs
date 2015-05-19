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
		private ScriptMethods scriptMethods;

		public ScriptEngine Engine {
			get {
				if (_engine == null) {
					_engine = new ScriptEngine();
				}
				return _engine;
			}
		}

		public Roslyn.Scripting.Session Session {
			get {
				if (_session == null) {
					
					_session = Engine.CreateSession(scriptMethods, scriptMethods.GetType());
				}
				return _session;
			}
		}

        public override string MemStreamAsString {
            get {
				ASCIIEncoding.ASCII.GetString(MemStreamAsByteArray);
				return @"SendMessage(""52aa9e0231b6bd15cc44e0b2"", ""You just ran a roslyn script! Congratulations on this achievement!"");";
            }
        }

        public RoslynScript() { }
        /// <summary>
        /// Pass in FALSE for registerMethods if you would like to provide the methods to register by calling LuaScript.
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="registerMethods"></param>
        public RoslynScript(string scriptID, string scriptCollection, bool registerMethods = true) {
			scriptMethods = new ScriptMethods();
            MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptByteArray = (byte[])doc["Bytes"].AsBsonBinaryData;
				if (registerMethods) {
					new[]
					{
						 typeof (Type).Assembly,
						 typeof (ICollection).Assembly,
						 typeof (Console).Assembly,
						 typeof (RoslynScript).Assembly,
						 typeof (IEnumerable<>).Assembly,
						 typeof (IQueryable).Assembly,
						 GetType().Assembly
					}.ToList().ForEach(asm => Engine.AddReference(asm));
				
					//Import common namespaces
					new[]
					{
						 "System", "System.Linq",
						 "System.Collections",
						 "System.Collections.Generic"
					 }.ToList().ForEach(ns => Engine.ImportNamespace(ns));
				}
            }
        }

        public RoslynScript(BsonDocument doc, bool registerMethods = true) {
            MemStream = new MemoryStream((byte[])doc["Bytes"].AsBsonBinaryData);
            if (registerMethods) {
            }
        }

        ~RoslynScript(){
            if (_memStream != null) {
                _memStream.Dispose();
            }
			if (_engine != null) {
				_engine = null;
			}
			if (_session != null) {
				_session = null;
			}
        }
        
        public override void RunScript() {
            MemStream = new MemoryStream(ScriptByteArray);
            if (_memStream != null) {
			   Session.Execute(MemStreamAsString);
            }
        }

        public override void AddVariableForScript(object variable, string variableName) {
		
        }

		public void AddNamespace(string nameSpace) {
			_session.ImportNamespace(nameSpace);
		}

		public void AddReference(Assembly assembly) {
			_session.AddReference(assembly);
		}
	}


	//this will be for any scripts that we want builders to create.  It will have limited access to classes.
	public class LuaScript : Script {
        private Lua _lua;
		private ScriptMethods scriptMethods;

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

        public override string MemStreamAsString {
            get {
				return ASCIIEncoding.ASCII.GetString(MemStreamAsByteArray);
            }
        }

        public LuaScript() { }
        /// <summary>
        /// Pass in FALSE for registerMethods if you would like to provide the methods to register by calling LuaScript.
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="registerMethods"></param>
        public LuaScript(string scriptID, string scriptCollection, bool registerMethods = true) {
			scriptMethods = new ScriptMethods();
            MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptByteArray = (byte[])doc["Bytes"].AsBsonBinaryData;
				if (registerMethods) {
					Engine.RegisterMarkedMethodsOf(this);
				}
            }
        }

        public LuaScript(BsonDocument doc, bool registerMethods = true) {
            MemStream = new MemoryStream((byte[])doc["Bytes"].AsBsonBinaryData);
            if (registerMethods) {
				Engine.RegisterMarkedMethodsOf(this);
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
            MemStream = new MemoryStream(ScriptByteArray);
            if (_memStream != null) {
				Engine.DoString(MemStreamAsString);
            }
        }

        public override void AddVariableForScript(object variable, string variableName) {
			Engine[variableName] = variable;
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

		public abstract string MemStreamAsString {
			get;
		}

		protected byte[] MemStreamAsByteArray {
			get {
				return ScriptByteArray;
			}
		}

		public virtual void AddVariableForScript(object variable, string variableName) {
			throw new NotImplementedException();
		}

		public virtual void RunScript() {
			throw new NotImplementedException();
		}
	}

	
}
