using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LuaInterface;
using System.Reflection;
using Rooms;
using Character;
using Items;
using Extensions;
using Commands;
using System.Collections;
using NCalc;

namespace Triggers {
	public class ScriptMethods {
		public Dictionary<string, object> DataSet {
			get;
			set;
		}
		public Stack<object> DataStack {
			get;
			set;
		}


		public ScriptMethods() {
			DataSet = new Dictionary<string, object>();
			DataStack = new Stack<object>();
		}
		//These are copied from SKill.cs will need to work on converting skill.cs to using the Script class so they can share common ones
		//and if needed then classes can register their own additional functions with Lua

		[LuaAccessible]
		public static object GetPlayer(string playerID) {
			object o = null;
			o = MySockets.Server.GetAUser(playerID).Player;
			return o;
		}

		public User.User GetUser(string playerID) {
			return MySockets.Server.GetAUser(playerID);
		}

		[LuaAccessible]
		public static Type GetClassType(string className) {
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
		public static object GetMethodResult(string className, string methodName, object table) {
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

		//rosyln version
		public object GetMethodResult(string className, string methodName, object[] parameters) {
			Type t = GetClassType(className);
			MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);	
			object result = m.Invoke(null, parameters);

			return result;
		}

		[LuaAccessible]
		public static object CastObject(object o, string type) {
			return Convert.ChangeType(o, Type.GetType(type));
		}

		[LuaAccessible]
		public static Type GetObjectType(object o) {
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
		public static Type GetType(string typeToReturn) {
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
		public static void InvokeMethod(object o, string methodName, object table) {
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

		//roslyn method
		public void InvokeMethod(object o, string methodName, object[] parameters) {
			Type t = o.GetType();
			MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

			if (m.IsStatic) {
				o = null;
			}

			m.Invoke(o, parameters);
		}
		


		[LuaAccessible]
		public static object GetDictionaryElement(object o, string name) {
			IDictionary dict = (IDictionary)o;
			if (dict.Contains(name.CamelCaseWord())) {
				o = dict[name.CamelCaseWord()];
			}

			return o;
		}

		[LuaAccessible]
		public static object GetElement(object[] array, double position, string type) {
			object o = null;
			if ((int)position >= 0 && (int)position < array.Length) {
				o = array[(int)position];
				o = CastObject(o, type);
			}
			return o;
		}

		[LuaAccessible]
		public static void SendMessage(string playerID, string message) {
			User.User player = MySockets.Server.GetAUser(playerID);
			player.MessageHandler(message);
		}

		[LuaAccessible]
		public static object GetProperty(object o, string value, string type, string className = null) {
			if (o == null && className == null) {
				return null;
			}

			Type t = null;

			t = o.GetType();
			

			var p = t.GetProperties();

			o = p.Where(prop => string.Equals(prop.Name, value, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault().GetValue(o, null);

			if (!string.IsNullOrEmpty(type)) {
				o = CastObject(o, type);
			}

			return o;
		}

		[LuaAccessible]
		public static object GetField(object o, string name) {
			Type t = o.GetType();
			FieldInfo f = t.GetField(name);

			o = f.GetValue(o);

			return o;
		}

		[LuaAccessible]
		public static object GetMember(object o, string name) {
			Type t = o.GetType();

			o = t.GetMember(name)[0];

			return o;
		}

		[LuaAccessible]
		public static List<object> Table2List(LuaInterface.LuaTable table) {
			List<object> list = new List<object>();
			foreach (DictionaryEntry e in table) {
				list.Add(e.Value);
			}

			return list;
		}

		[LuaAccessible]
		public static object[] Table2Array(LuaInterface.LuaTable table) {
			return Table2List(table).ToArray();
		}

		[LuaAccessible]
		public double ParseAndCalculateCheck(Character.Iactor player, string calculation) {
			Expression expression = new Expression(ReplaceStringWithNumber(player, calculation));
			double result = 0;
			try {
				result = (double)expression.Evaluate();
			}
			catch (Exception) {
			}

			return result;
		}

		[LuaAccessible]
		public object ColorFont(string message, double color) {
			Utils.FontForeColor fontColor = (Utils.FontForeColor)color;
			return message.FontColor(fontColor);
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

		/// <summary>
		/// For this method to work correctly the Attribute names **MUST** be separated by a space at the start and end.
		/// (Dexterity+Cunning) will not work it needs to be ( Dexterity + Cunning ) any other math symbols and numbers do not require
		/// spaces.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		private string ReplaceStringWithNumber(Character.Iactor player, string calculation) {
			//would like to make this a bit more generic so if new attributes are inserted we don't have to change this method
			//I think easiest way is to have the expression be separated by spaces, but just so it works with anything let's get rid of
			//any mathematical signs and then we should just have the name of the attributes we want.
			string temp = calculation;
			string[] operators = new string[] { "+", "-", "/", "*", "(", ")", "[", "]", "{", "}", "^", "SQRT", "POW", "." };
			foreach (string operand in operators) {
				temp = temp.Replace(operand, " ");
			}

			//need to get rid of repeats and empty spaces
			string[] attributeList = temp.Split(' ');

			temp = calculation;

			foreach (string attributeName in attributeList) {
				if (!string.IsNullOrEmpty(attributeName)) {
					if (player.GetAttributes().ContainsKey(attributeName)) {
						temp = temp.Replace(attributeName, player.GetAttributeValue("attributeName").ToString());
					}
					else if (player.GetSubAttributes().ContainsKey(attributeName)) {
						temp = temp.Replace(attributeName, player.GetSubAttributes()[attributeName].ToString());
					}
					else if (attributeName.Contains("Rank")) {
						temp = temp.Replace(attributeName, player.GetAttributeRank(attributeName.Substring(0, attributeName.Length - 4)).ToString());
					}
				}
			}

			return temp;
		}
	}
}
