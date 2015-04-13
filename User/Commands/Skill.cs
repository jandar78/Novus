using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoUtils;
using Extensions;
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

namespace Commands {   

    public class Skill {
        private Dictionary<string, object> DataSet { get; set; }
        private Stack<object> DataStack { get; set; }

       // public BsonDocument SkillDocument { get; set; }
        public BsonArray CheckPlayersInRoom { get; set; }
        
        public SkillLevel skillLevel { get; set; }
        public SkillTypes skillType { get; set; }
         
        public string MsgTarget { get; set; }
        public string MsgOthers { get; set; }
        public string Message { get; set; }
        public string Calculation { get; set; }
        public bool MustHavePlayersInRoom { get; set; }
        public List<string> UserCommand { get; set; }

        public BsonArray StateSelfNotAllowed { get; set; } 
        public BsonArray StateSelfNeedsToBe { get; set; }
        public BsonArray StanceSelfNotAllowed { get; set; }
        public BsonArray StanceSelfNeedsToBe { get; set; }
        public BsonArray StateOtherNotAllowed { get; set; } 
        public BsonArray StanceOtherNotAllowed { get; set; }
        public BsonArray StateOtherNeedsToBe { get; set; }
        public BsonArray StanceOtherNeedsToBe { get; set; }

        public User.User Target { get; set; }
        public User.User Player { get; set; }

        //this may end up being a dictionary if player can have more than one action state applied
        //there could be states if skill outcome is succesful or not that affects the player and others around him
        //ex. if player cartwheels and fails stance would be lying down and not standing. (what if different stances based on how close to success it was?)
        //may make these a dictionary in the future we'll see
        public CharacterEnums.CharacterActionState StateIfSuccessSelf { get; set; }
        public CharacterEnums.CharacterStanceState StanceIfSuccessSelf { get; set; }

        public CharacterEnums.CharacterActionState StateIfSuccessOthers { get; set; }
        public CharacterEnums.CharacterStanceState StanceIfSuccessOthers { get; set; }
        
        public double CheckToPass { get; set; }
        public Script script;

        public Skill() { }

        public void FillSkill(User.User user, List<string> commands) {
            //get the Skill from the DB
            DataSet = new Dictionary<string, object>();
            DataStack = new Stack<object>();

            UserCommand = commands;
            
            script = new Script(commands[1].CamelCaseWord(), "Action");
            
            UserCommand.RemoveAt(0);
            script.LuaScript["UserCommand"] = UserCommand;
            
            script.LuaScript.RegisterMarkedMethodsOf(this);

            Player = user;
            script.LuaScript["player"] = Player.Player;
            
            //if they have a target or they passed one in let's add it to the script variables as well
            if (Player.Player.CurrentTarget != null || commands.Count > 3){
                if (Player.Player.CurrentTarget != null && commands.Count <= 3) { //didn't pass a target because they have one
                    Target = MySockets.Server.GetAUser(Player.Player.CurrentTarget);
                }
                else { //they passed in a target
                    Target = CommandParser.FindTargetByName(commands[2], user.Player.Location);
                }

                if (Target != null) {
                    script.LuaScript["target"] = Target.Player;
                }
            }
        }
        

        /// <summary>
        /// For this method to work correctly the Attribute names **MUST** be separated by a space at the start and end.
        /// (Dexterity+Cunning) will not work it needs to be ( Dexterity + Cunning ) any other math symbols and numbers do not require
        /// spaces.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private string ReplaceStringWithNumber(Character.Iactor player) {
            //would like to make this a bit more generic so if new attributes are inserted we don't have to change this method
            //I think easiest way is to have the expression be separated by spaces, but just so it works with anything let's get rid of
            //any mathematical signs and then we should just have the name of the attributes we want.
            string temp = Calculation;
            string[] operators = new string[] { "+", "-", "/", "*", "(", ")", "[", "]", "{", "}", "^", "SQRT", "POW", "." };
            foreach (string operand in operators) {
             temp = temp.Replace(operand, " ");
            }
           
            //need to get rid of repeats and empty spaces
            string[] attributeList = temp.Split(' ');

            temp = Calculation;

            foreach (string attributeName in attributeList) {
                if (!string.IsNullOrEmpty(attributeName)) {
                    if (player.GetAttributes().ContainsKey(attributeName)) {
                       temp = temp.Replace(attributeName, player.GetAttributeValue("attributeName").ToString());
                    }
                    else if (player.GetSubAttributes().ContainsKey(attributeName)) {
                        temp = temp.Replace(attributeName, player.GetSubAttributes()[attributeName].ToString());
                    }
                    else if (attributeName.Contains("Rank")){
                        temp = temp.Replace(attributeName, player.GetAttributeRank(attributeName.Substring(0, attributeName.Length - 4)).ToString());
                    }
                }
            }

            return temp;                     
        }

    
        #region Lua script parsing methods
        public void ExecuteScript() {
            script.RunScript();

                if (Message != null) {
                    Player.MessageHandler(Message);
                }
                if (Target != null && MsgTarget != null) {
                    Target.MessageHandler(MsgTarget);
                }
                if (MsgOthers != null) {
                    Room.GetRoom(Player.Player.Location).InformPlayersInRoom(MsgOthers, new List<string>(new string[] { Player.UserID }));
                }
        }
        
        //LUA methods need to be public or you'll get "method is nil" exception
        [LuaAccessible]
        public double ParseAndCalculateCheck(Character.Iactor player, string calculation) {
            Calculation = calculation;
            Expression expression = new Expression(ReplaceStringWithNumber(player));
            double result = 0;
            try {
                result = (double)expression.Evaluate();
            }
            catch (Exception) {}
            
            return CheckToPass = result;
        }

        [LuaAccessible]
        public void SetSkillStates(object o, bool others = false) {
            if (o != null) {
                Character.Iactor actor = (Character.Iactor)o;

                if (!others) {
                    if (StanceIfSuccessSelf != CharacterEnums.CharacterStanceState.None) {
                        actor.SetStanceState(StanceIfSuccessSelf);
                    }

                    actor.SetActionState(StateIfSuccessSelf);
                }
                else {
                    actor.SetStanceState(StanceIfSuccessOthers);
                    actor.SetActionState(StateIfSuccessOthers);
                }
            }
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
            if (!string.IsNullOrEmpty(message)) {
                if (string.Equals(who, "Player", StringComparison.CurrentCultureIgnoreCase)) {
                    Message = message;
                }
                else if (string.Equals(who, "Target", StringComparison.CurrentCultureIgnoreCase)) {
                    MsgTarget = message;
                }
                else if (string.Equals(who, "Room", StringComparison.CurrentCultureIgnoreCase)) {
                    MsgOthers = message;
                }
            }
        }

        [LuaAccessible]
        public object GetPlayer(string type) {
            object o = null;
            if (string.Equals(type, "Character", StringComparison.CurrentCultureIgnoreCase)) {
                o = Player.Player;
            }
            else {
                o = Player;
            }

            return o;
        }

        [LuaAccessible]
        public object GetTarget(string type) {
            object o = null;
            if (string.Equals(type, "Character", StringComparison.CurrentCultureIgnoreCase)) {
                o = Target.Player;
            }
            else {
                o = Target;
            }

            return o;
        }

        #endregion Lua script parsing methods
        
        private enum CheckRoom { NONE, ALL, GREATER_OR_EQUAL, GREATER, LESS, LESS_OR_EQUAL }
    }

}
