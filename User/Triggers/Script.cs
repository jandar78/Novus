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
using Rooms;
using Character;
using Items;
using Extensions;
using Commands;
using System.Collections;

namespace Triggers {
    public class Script {
        private MemoryStream _memStream;
        private Lua _lua;

        private MemoryStream MemStream {
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

        //added this because if we just initialize a the memory stream then we have very serious (crashing) issue when saving the items.
        //the memory stream causes a "timeout not supported by stream" exception.  It wasn't fun to track down or figure out.
        private byte[] ScriptByteArray { get; set; }

        public Lua LuaScript {
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

        public byte[] MemStreamAsByteArray {
            get {
                return ScriptByteArray;
            }
        }

        public string MemStreamAsString {
            get {
              return ASCIIEncoding.ASCII.GetString(MemStreamAsByteArray);
            }
        }

        public Script() { }
        /// <summary>
        /// Pass in FALSE for registerMethods if you would like to provide the methods to register by calling LuaScript.
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="registerMethods"></param>
        public Script(string scriptID, string scriptCollection, bool registerMethods = true) {
            MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", scriptCollection);
            BsonDocument doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptID));
            if (doc != null && doc["Bytes"].AsBsonBinaryData != null) {
                ScriptByteArray = (byte[])doc["Bytes"].AsBsonBinaryData;
                if (registerMethods) {
                    LuaScript.RegisterMarkedMethodsOf(this);
                }
            }
        }

        public Script(BsonDocument doc, bool registerMethods = true) {
            MemStream = new MemoryStream((byte[])doc["Bytes"].AsBsonBinaryData);
            if (registerMethods) {
                LuaScript.RegisterMarkedMethodsOf(this);
            }
        }

        ~Script(){
            if (_memStream != null) {
                _memStream.Dispose();
            }
            if (_lua != null) {
                _lua.Dispose();
            }
        }
        
        public void RunScript() {
            MemStream = new MemoryStream(ScriptByteArray);
            if (_memStream != null) {
               LuaScript.DoString(MemStreamAsString);
            }
        }

        public void AddVariableForScript(object variable, string variableName) {
            LuaScript[variableName] = variable;
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

        //These are copied from SKill.cs will need to work on converting skill.cs to using the Script class so they can share common ones
        //and if needed then classes can register their own additional functions with Lua
        #region Lua methods
        [LuaAccessible]
        public object GetPlayer(string playerID) {
            object o = null;
            o = MySockets.Server.GetAUser(playerID).Player;
            return o;
        }

        [LuaAccessible]
        public Type GetClassType(string className) {
            Type t = null;

            switch (className) {
                case "Utils":
                    t = typeof(Utils);
                    break;
                case "Room":
                    t = typeof(Room);
                    break;
                case "Exits":
                    t = typeof(Exits);
                    break;
                case "Skill":
                    t = typeof(Skill);
                    break;
                case "Character":
                    t = typeof(Character.Character);
                    break;
                case "NPC":
                    t = typeof(Character.NPC);
                    break;
                case "NPCUtils":
                    t = typeof(Character.NPCUtils);
                    break;
                case "Items":
                    t = typeof(Items.Items);
                    break;
                case "User":
                    t = typeof(User.User);
                    break;
                case "Server":
                    t = typeof(MySockets.Server);
                    break;
                case "CommandParser":
                    t = typeof(CommandParser);
                    break;
                default:
                    break;
            }

            return t;
        }

        [LuaAccessible]
        public object GetMethodResult(string className, string methodName, object table) {
            Type t = GetClassType(className);         
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            LuaTable luaTable = (LuaTable)table;
            ParameterInfo[] p = m.GetParameters();
            object[] parameters = new object[luaTable.Values.Count];
            int i = 0;
            foreach (var value in luaTable.Values) {
                parameters[i] = CastObject(value, p[i].ParameterType.FullName);
                i++;
            }


            object result = m.Invoke(null, parameters);

            return result;
        }

        [LuaAccessible]
        public object CastObject(object o, string type) {
            return Convert.ChangeType(o, Type.GetType(type));
        }

        [LuaAccessible]
        public Type GetObjectType(object o) {
            Type t = o.GetType();
            switch (t.Name) {
                case "bool":
                    t = typeof(System.Boolean);
                    break;
                case "int":
                    t = typeof(System.Int32);
                    break;
                case "double":
                    t = typeof(System.Double);
                    break;
                case "string":
                default:
                    t = typeof(System.String);
                    break;
            }
            return t;
        }

        [LuaAccessible]
        public Type GetType(string typeToReturn) {
            Type t = null;
            switch (typeToReturn) {
                case "bool":
                    t = typeof(System.Boolean);
                    break;
                case "int":
                    t = typeof(System.Int32);
                    break;
                case "double":
                    t = typeof(System.Double);
                    break;
                case "string":
                default:
                    t = typeof(System.String);
                    break;
            }
            return t;
        }

        [LuaAccessible]
        public void InvokeMethod(object o, string methodName, object table) {
            Type t = o.GetType();
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

            LuaTable luaTable = table as LuaTable;
            if (luaTable != null) {
                object[] parameters = new object[luaTable.Values.Count];
                int i = 0;
                foreach (var value in luaTable.Values) {
                    var vType = value.GetType();
                    parameters[i] = value;
                    i++;
                }
                if (m.IsStatic) {
                    o = null;
                }

                m.Invoke(o, parameters);
            }
            else {
                if (m.IsStatic) {
                    o = null;
                }

                m.Invoke(o, new object[] { table });
            }
        }

        [LuaAccessible]
        public object GetDictionaryElement(object o, string name) {
            IDictionary dict = (IDictionary)o;
            if (dict.Contains(name.CamelCaseWord())) {
                o = dict[name.CamelCaseWord()];
            }

            return o;
        }

        [LuaAccessible]
        public object GetElement(object[] array, double position, string type) {
            object o = null;
            if ((int)position >= 0 && (int)position < array.Length) {
                o = array[(int)position];
                o = CastObject(o, type);
            }
            return o;
        }

        [LuaAccessible]
        public void SendMessage(string playerID, string message) {
            User.User player = MySockets.Server.GetAUser(playerID);
            player.MessageHandler(message);
        }
       
        [LuaAccessible]
        public object GetProperty(object o, string value, string type, string className = null) {
            if (o == null && className == null) {
                return null;
            }

            Type t = null;

            if (!string.IsNullOrEmpty(className) && string.Equals(className, "Skill", StringComparison.CurrentCultureIgnoreCase)) {
                o = this;
                t = o.GetType();
            }
            else {
                t = o.GetType();
            }

            var p = t.GetProperties();

            o = p.Where(prop => string.Equals(prop.Name, value, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault().GetValue(o, null);

            if (!string.IsNullOrEmpty(type)) {
                o = CastObject(o, type);
            }

            return o;
        }

        [LuaAccessible]
        public object GetField(object o, string name) {
            Type t = o.GetType();
            FieldInfo f = t.GetField(name);

            o = f.GetValue(o);

            return o;
        }

        [LuaAccessible]
        public object GetMember(object o, string name) {
            Type t = o.GetType();

            o = t.GetMember(name)[0];

            return o;
        }

        [LuaAccessible]
        public List<object> Table2List(LuaInterface.LuaTable table) {
            List<object> list = new List<object>();
            foreach (DictionaryEntry e in table) {
                list.Add(e.Value);
            }
            
            return list;
        }

        [LuaAccessible]
        public object[] Table2Array(LuaInterface.LuaTable table) {
            return Table2List(table).ToArray();
        }

        
        #endregion Lua methods

    }
}
