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
using Interfaces;

namespace Commands {   

    public class Skill {
        public BsonArray CheckPlayersInRoom { get; set; }
        
        public SkillLevel skillLevel { get; set; }
        public SkillTypes skillType { get; set; }
         
        public List<string> UserCommand { get; set; }

        public IUser Target { get; set; }
        public IUser Player { get; set; }

        //this may end up being a dictionary if player can have more than one action state applied
        //there could be states if skill outcome is succesful or not that affects the player and others around him
        //ex. if player cartwheels and fails stance would be lying down and not standing. (what if different stances based on how close to success it was?)
        //may make these a dictionary in the future we'll see
        public CharacterActionState StateIfSuccessSelf { get; set; }
        public CharacterStanceState StanceIfSuccessSelf { get; set; }

        public CharacterActionState StateIfSuccessOthers { get; set; }
        public CharacterStanceState StanceIfSuccessOthers { get; set; }
        
        public IScript script;

        public Skill() {}

        public void FillSkill(IUser user, List<string> commands) {
            UserCommand = commands;
            
			script = ScriptFactory.GetScript(commands[1].CamelCaseWord(), "Action");
            
            UserCommand.RemoveAt(0);
			Player = user;
			
			if (script.ScriptType == ScriptTypes.Lua) {
				script.AddVariable(UserCommand, "UserCommand");
				script.AddVariable(Player.Player, "player");
			}
			else {
				script.AddVariable(UserCommand, "UserCommand");
				script.AddVariable(Player.Player.ID, "playerID");
			}
            
            //if they have a target or they passed one in let's add it to the script variables as well
            if (Player.Player.CurrentTarget != null || commands.Count > 3){
                if (Player.Player.CurrentTarget != null && commands.Count <= 3) { //didn't pass a target because they have one
                    Target = MySockets.Server.GetAUser(Player.Player.CurrentTarget);
                }
                else { //they passed in a target
                    Target = CommandParser.FindTargetByName(commands[2], user.Player.Location);
                }

                if (Target != null) {
					if (script.ScriptType == ScriptTypes.Lua) {
						script.AddVariable(Target.Player, "target");
					}
					else {
						script.AddVariable(Target.Player.ID, "targetID");
					}
                }
            }
        }
        
        public void ExecuteScript() {
            script.RunScript();
        }       
    }

}
