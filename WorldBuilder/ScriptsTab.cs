using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;
using System.Collections;
using Triggers;
using Roslyn.Scripting.CSharp;
using Interfaces;
using Sockets;
using Rooms;
using LuaInterface;

namespace WorldBuilder {
    public partial class Form1 : Form {
        private bool ScriptError { get; set; }

        //these two are so the lua registered methods can work
        private Dictionary<string, object> DataSet { get; set; }
        private Stack<object> DataStack { get; set; }

        private async void saveScript_Click(object sender, EventArgs e) {
            if (!IsEmpty(scriptIdValue.Text) && !IsEmpty(scriptValue.Text) && !ScriptError) {
                byte[] scriptBytes = System.Text.Encoding.ASCII.GetBytes(scriptValue.Text);

                BsonBinaryData scriptArray = new BsonBinaryData(scriptBytes);

                Triggers.Script newScript = new LuaScript() {
                    ID = scriptIdValue.Text,
                    ScriptByteArray = scriptBytes,
                    ScriptType = (ScriptTypes)Enum.Parse(typeof(ScriptTypes), scriptTypeValue.SelectedItem.ToString())
                };

                var collection = MongoUtils.MongoData.GetCollection<Triggers.Script>("Scripts", (string)scriptTypesValue.SelectedItem);
                await collection.ReplaceOneAsync<Triggers.Script>(s => s.ID == scriptIdValue.Text, newScript, new UpdateOptions { IsUpsert = true });
                
                scriptValidatedValue.Visible = false;
            }
            else if (ScriptError) {
                DisplayErrorBox("Script file contains errors.  Test script before saving.");
            }
        }

        private void loadScript_Click(object sender, EventArgs e) {
            if (!IsEmpty(scriptIdValue.Text) && !IsEmpty(scriptTypesValue.Text)) {
                var collection = MongoUtils.MongoData.GetCollection<Triggers.Script>("Scripts", (string)scriptTypesValue.SelectedItem);
                var script = MongoUtils.MongoData.RetrieveObject<Triggers.Script>(collection, s => s.ID == scriptIdValue.Text);
                
                byte[] scriptBytes = (byte[])script.ScriptByteArray;
				scriptTypeValue.Text = script.ScriptType.ToString();
				scriptValue.Text = script.MemStreamAsString;
                
                scriptValidatedValue.Visible = false;
            }
        }

        private void scriptFilterRefresh_Click(object sender, EventArgs e) {
            if (ConnectedToDB) {
                if (!IsEmpty((string)scriptFilterTypeValue.SelectedItem)) {
                    scriptDatabaseValues.Items.Clear();

                    IEnumerable<Triggers.Script> result = new List<Triggers.Script>();
                    var collection = MongoUtils.MongoData.GetCollection<Triggers.Script>("Scripts", (string)scriptFilterTypeValue.SelectedItem);
                    if (IsEmpty(scriptFilterValue.Text)) {
                        result = MongoUtils.MongoData.RetrieveObjects<Triggers.Script>(collection, s => s.ID != string.Empty);
                    }
                    else {
                        result = new List<Triggers.Script>() { MongoUtils.MongoData.RetrieveObject<Triggers.Script>(collection, s => s.ID == scriptFilterValue.Text) };
                    }

                    foreach (var script in result) {
                        this.scriptDatabaseValues.Items.Add(script.ID);
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
			scriptValidatedValue.Visible = false;
			if (scriptTypeValue.Text == "Lua") {
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
					}
				}
			}
			else {
				if (byPassTestValue.Visible) {
					ScriptError = !byPassTestValue.Checked;
				}
				ScriptEngine engine = new ScriptEngine();
				ScriptMethods host = new ScriptMethods();
				host.DataSet.Add("player", Character.NPCUtils.CreateNPC(1));
				host.DataSet.Add("npc", Character.NPCUtils.CreateNPC(1));
				Roslyn.Scripting.Session session = engine.CreateSession(host, host.GetType());
				new[]
					   {
						 typeof (Type).Assembly,
						 typeof (ICollection).Assembly,
						 typeof (Console).Assembly,
						 typeof (RoslynScript).Assembly,
						 typeof (IEnumerable<>).Assembly,
						 typeof (IQueryable).Assembly,
						 typeof (ScriptMethods).Assembly,
						 typeof(IActor).Assembly,
						 typeof(Character.Character).Assembly,
						 typeof(NPC).Assembly,
						 typeof(Room).Assembly,
						 typeof(Commands.CommandParser).Assembly,
						 typeof(Interfaces.Message).Assembly,
						 GetType().Assembly
					}.ToList().ForEach(asm => session.AddReference(asm));

				//Import common namespaces
				new[]
						{
						 "System", "System.Linq", "System.Object", "System.Collections", "System.Collections.Generic",
						 "System.Text", "System.Threading.Tasks", "System.IO",
						 "Character", "Rooms", "Items", "Commands", "ClientHandling", "Triggers"
					 }.ToList().ForEach(ns => session.ImportNamespace(ns));
				try {
					var result = session.CompileSubmission<object>(scriptValue.Text);
					ScriptError = false;
					scriptValidatedValue.Visible = true;
				}
				catch (Exception ex) {
					MessageBox.Show("Errors found in script:\n " + ex.Message, "Script Errors", MessageBoxButtons.OK);
				}
			}
		}

        #region Lua Methods
        //LUA methods need to be public or you'll get "method is nil" exception
        //these are just stub methods for th emost part solely to let th eLua interface run through the lua script and find any compiler errors
		//this will not find logic errors in the script.  All scripts should be tested before in game before being moved to live.
        [LuaAccessible]
        public double ParseAndCalculateCheck(IActor player, string calculation) {
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
                    t = typeof(NPC);
                    break;
                case "NPCUtils":
                    t = typeof(Character.NPCUtils);
                    break;
                case "Items":
                    t = typeof(Items.Items);
                    break;
                case "User":
                    t = typeof(User);
                    break;
                case "Server":
                    t = typeof(Server);
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
            IUser user = new User();
            IActor character = Factories.Factory.CreateCharacter(CharacterType.PLAYER);
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
            IUser user = new User();
            IActor character = Factories.Factory.CreateCharacter(CharacterType.PLAYER);
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