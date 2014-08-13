using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using NCalc;
using System.Reflection;
using Rooms;
using Character;
using Items;
using System.Xml;
using System.Collections;
using LuaInterface;
using Triggers;

namespace WorldBuilder {
    public partial class Form1 : Form {
        private bool ScriptError { get; set; }

        //these two are so the lua registered methods can work
        private Dictionary<string, object> DataSet { get; set; }
        private Stack<object> DataStack { get; set; }

        private void saveScript_Click(object sender, EventArgs e) {
            if (!IsEmpty(scriptIdValue.Text) && !IsEmpty(scriptValue.Text) && !ScriptError) {
                byte[] scriptBytes = System.Text.Encoding.ASCII.GetBytes(scriptValue.Text);

                BsonBinaryData scriptArray = new BsonBinaryData(scriptBytes);
                
                BsonDocument doc = new BsonDocument();
                doc.Add("_id", scriptIdValue.Text);
                doc.Add(new BsonElement("Bytes", scriptArray.AsBsonValue));
                    
                

                MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", (string)scriptTypesValue.SelectedItem);
                collection.Save(doc);
                
                scriptValidatedValue.Visible = false;
            }
            else if (ScriptError) {
                DisplayErrorBox("Script file contains errors.  Test script before saving.");
            }
        }

        private void loadScript_Click(object sender, EventArgs e) {
            if (!IsEmpty(scriptIdValue.Text) && !IsEmpty(scriptTypesValue.Text)) {
                MongoCollection collection = MongoUtils.MongoData.GetCollection("Scripts", (string)scriptTypesValue.SelectedItem);
                BsonDocument scriptDocument = collection.FindOneAs<BsonDocument>(Query.EQ("_id", scriptIdValue.Text));

                byte[] scriptBytes = (byte[])scriptDocument["Bytes"].AsBsonBinaryData;

                
                scriptValue.Text = System.Text.Encoding.ASCII.GetString(scriptBytes);
                
                scriptValidatedValue.Visible = false;
            }

        }

        private void scriptFilterRefresh_Click(object sender, EventArgs e) {
            if (ConnectedToDB) {
                if (!IsEmpty((string)scriptFilterTypeValue.SelectedItem)) {
                    scriptDatabaseValues.Items.Clear();

                    MongoCursor<BsonDocument> result = null;
                    if (IsEmpty(scriptFilterValue.Text)) {
                        result = MongoUtils.MongoData.GetCollection("Scripts", (string)scriptFilterTypeValue.SelectedItem).FindAllAs<BsonDocument>();
                    }
                    else {
                        result = MongoUtils.MongoData.GetCollection("Scripts", (string)scriptFilterTypeValue.SelectedItem).FindAs<BsonDocument>(Query.EQ("_id", scriptFilterValue.Text));
                    }

                    foreach (BsonDocument doc in result) {
                        this.scriptDatabaseValues.Items.Add(doc["_id"].AsString);
                    }
                }
                else {
                    DisplayErrorBox("You must select a filter first");
                }
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e) {
            scriptIdValue.Text = (string)scriptDatabaseValues.Items[scriptDatabaseValues.SelectedIndex];
            scriptTypesValue.SelectedItem = scriptFilterTypeValue.SelectedItem;

            loadScript_Click(null, null);
            
        }

        private void testScript_Click(object sender, EventArgs e) {
            Lua lua = new Lua();
            lua.RegisterMarkedMethodsOf(this);
            //add some variables so we can pass tests
            lua["item"] = Items.ItemFactory.CreateItem(ObjectId.Parse("5383dc8531b6bd11c4095993"));

            if (byPassTestValue.Visible) {
                ScriptError = !byPassTestValue.Checked;
            }
            else {
                try {
                    lua.DoString(scriptValue.Text);
                    ScriptError = false;
                    scriptValidatedValue.Visible = true;
                }
                catch (LuaException lex) {
                    MessageBox.Show(lex.Message, "Lua Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ScriptError = true;
                    scriptValidatedValue.Visible = false;
                }
            }
        }

        #region Lua Methods
        //LUA methods need to be public or you'll get "method is nil" exception
        //these are just stub methods for th emost part solely to let th eLua interface run through the lua script and find any compiler errors
        [LuaAccessible]
        public double ParseAndCalculateCheck(Character.Iactor player, string calculation) {
            return 1.0d;
        }

        [LuaAccessible]
        public void SetSkillStates(object o, bool others = false) {
        }

        [LuaAccessible]
        public double ParseAndCalculateCheckOther(object player) {
           return 0.0d;
        }

        [LuaAccessible]
        public void SendMessage(string playerID, string message) {
            
        }

        [LuaAccessible]
        public object ColorFont(string message, double color) {
           return message;
        }

        [LuaAccessible]
        public void SetVariable(string name, object o) {
            if (!DataSet.ContainsKey(name)) {
                DataSet.Add(name, o);
            }
            else {
                DataSet[name] = o;
            }
        }

        [LuaAccessible]
        public object GetProperty(object o, string value, string type, string className = null) {
            return null;
        }

        [LuaAccessible]
        public object GetVariable(string name) {
            object o = null;
            if (DataSet.ContainsKey(name)) {
                o = DataSet[name];
            }

            return o;
        }

        [LuaAccessible]
        public void AssignMessage(string who, string message) {
            
        }

        [LuaAccessible]
        public object GetMethodResult(string className, string methodName, object table) {
            Type t = GetClassType(className);
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            LuaTable luaTable = (LuaTable)table;
            ParameterInfo[] p = m.GetParameters();
            object[] parameters = new object[luaTable.Values.Count];
            int i = 0;
            foreach (var value in luaTable.Values) {
                parameters[i] = CastObject(value, p[i].ParameterType.FullName);
                i++;
            }
            object result = null; 
            try {
                result = m.Invoke(null, parameters);
            }
            catch (Exception){
                result = null;
            }

            return result;
        }

        [LuaAccessible]
        public object CastObject(object o, string type) {
            return Convert.ChangeType(o, Type.GetType(type));
        }

        [LuaAccessible]
        public Type GetClassType(string className) {
            Type t = null;

            switch (className) {
                case "Utils":
                    t = typeof(Extensions.Utils);
                    break;
                case "Room":
                    t = typeof(Room);
                    break;
                case "Exits":
                    t = typeof(Exits);
                    break;
                case "Skill":
                    t = typeof(Commands.Skill);
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
                    t = typeof(Commands.CommandParser);
                    break;
                default:
                    break;
            }

            return t;
        }

        [LuaAccessible]
        public object GetPlayer(string type) {
            User.User user = new User.User();
            Character.Iactor character = CharacterFactory.Factory.CreateCharacter(CharacterEnums.CharacterType.PLAYER);
            user.Player = character;
            object o = null;
            if (string.Equals(type, "Character", StringComparison.CurrentCultureIgnoreCase)) {
                o = character;
            }
            else {
                o = user;
            }

            return o;
        }

        [LuaAccessible]
        public object GetTarget(string type) {
            User.User user = new User.User();
            Character.Iactor character = CharacterFactory.Factory.CreateCharacter(CharacterEnums.CharacterType.PLAYER);
            user.Player = character;
            object o = null;
            if (string.Equals(type, "Character", StringComparison.CurrentCultureIgnoreCase)) {
                o = character;
            }
            else {
                o = user;
            }

            return o;
        }
     
    }
}
        #endregion Lua Methods