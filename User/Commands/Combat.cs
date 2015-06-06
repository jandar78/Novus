using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rooms;
using User;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Extensions;

namespace Commands {
	public partial class CommandParser {

        //Combat needs to follow some rules.
        //Always check if round timer expired before doing anything else  (CheckIfCanAttack())
        //1.Check for valid target (GetTarget())
        //2.Check if can hit
        //  a. Calculate attacker hit chance
        //    i. Take into consideration any penalties or bonuses
        //   ii. Take stance into consideration
        //  b. Calculate defender block/dodge chance
        //    i. Take into consideration any penalties or bonuses
        //   ii. Take stance into consideration
        //3. Hit - dodge = chance to hit (% of damage to apply)
        //  a. if chance to hit <= zero: no damage (we don't calculate an attack?)
        //  b. if chance to hit > zero: modifies damage
        //4. Calculate attack damage
        //  a. Take into consideration any penalties or bonuses
        //5. Calculate defense
        //  a. Take into consideration any penalties or bonuses
        //6. (Attack - Defense) * chance to hit
        //7. Apply damage
        //8. Apply penalties or bonuses based on attack
        //9. Set next round timer


        private static double ApplyStanceModifier(double originalValue){
            double stanceModifier = 1.0;
            //do some math stuff here to reduce 1.0 or increment it based on stance
            return originalValue * stanceModifier;
        }


        private static double GetBonus(User.User player, CharacterEnums.BonusTypes type) {
            return player.Player.GetBonus(type);
        }      

        //this is where attacks dones from scripts will call into
        //this can just set the round timer and apply the damages       
        private static void SpecialAttack(User.User player) {
            if (!CheckIfCanAttack(player.Player.LastCombatTime)) {
                return;
            }
        }

        /// <summary>
        /// Let's us know if enough time has elapsed to perform the next attack.
        /// Eventually this will be determined be a number of factors.
        /// </summary>
        /// <param name="lastCombatTime"></param>
        /// <returns></returns>
        private static bool CheckIfCanAttack(DateTime lastCombatTime) {
            TimeSpan wait = DateTime.Now.ToUniversalTime() - lastCombatTime;
            return (wait.TotalSeconds < 5);
        }

        //Will probably have to be modified a little bit when we introduce spells, but maybe not
        private static void Kill(User.User player, List<string> commands) {

            //Has the round interval time elapsed?
            if (!CheckIfCanAttack(player.Player.LastCombatTime)) {
                return;
            }

            //no target, no fight
            User.User enemy = GetTarget(player, commands);
            if (enemy == null) {
                return;
            }

            //For now when we get attacked we auto target that person
            TargetEachOther(player, enemy);

            //See if we can hit the other player
            double hitPercent = PercentHit(player, enemy);

            //get target block
            //block only lowers damage output, a really succesful block will make damage 0 or may even reflect back damage (maybe)
            //also block should take into consideration what is being used to block, obviosuly a shield is better but with a weapons parrying is much better than 
            //block, as you may lose a combat round or your stance may be affected for the next round causing the defending player to waste a combat round
            //to get back into a good stance (well not a full round but it will affect 
            //double chanceToBlock = GetAndEvaluateExpression("BlockChance", enemy.Player);

            //Get their main hand
            Items.Wearable mainHand = player.Player.Equipment.GetMainHandWeapon(player.Player);

            //Attack with the main hand
            WeaponHandAttack(player, enemy);
            //if they are wielding a weapon in their opposite hand then attack with it.
            if (player.Player.Equipment.GetWieldedWeapons().Count == 2) {
                WeaponHandAttack(player, enemy, true); //off-hand attack
            }

            //last time they attacked in a combat round 
            player.Player.LastCombatTime = DateTime.Now.ToUniversalTime();

            //save as we progress through the fight, no quitting once you get going 
            enemy.Player.Save();
            player.Player.Save();
        }

        private static double PercentHit(User.User player, User.User enemy) {
            //TODO:
            //Calculate the attackers chance to succesfully perform a hit.
            //The defender then needs to calculate block. 
            //So the damage a player can do is proportional to the chance of hitting he has, the higher chance of landing a full blow
            //the higher the damage will be.  The defender also does have a chance to dodge an attack which can cause damage to be zero (full dodge)
            //or be lowered some more (graze).  Dodging should happen rarely based on what level of dodge they have (Master may be only 25% chance to dodge)
            //obviously being a master dodger will require quite a bit of points dropped into dexterity and endurance.
            //Blocking lowers the damage amount, not the chance to hit.
            double chanceToHit = GetAndEvaluateExpression("HitChance", player.Player);
            return chanceToHit;
          
        }

        private static double GetAndEvaluateExpression(string calculationName, Character.Iactor player) {
            MongoCollection col = MongoUtils.MongoData.GetCollection("Calculations", "Combat");
            IMongoQuery query = Query.EQ("_id", calculationName);
            BsonDocument doc = col.FindOneAs<BsonDocument>(query).AsBsonDocument;
            NCalc.Expression expression = new NCalc.Expression(ReplaceStringWithNumber(player, doc["Expression"].AsString));
            double expressionResult = (double)expression.Evaluate();
            //let's take into consideration some other factors.  Visibility, stance, etc.
            //TODO: add that here.  They only take away from chance to hit. But some of them can be negated.
            //if it's dark but player can see in the dark for example.
            return expressionResult;
        }

        //TODO:
        //This method exists in the Skill class, should probably combine them into a math library or something
        private static string ReplaceStringWithNumber(Character.Iactor player, string expression) {
            //would like to make this a bit more generic so if new attributes are inserted we don't have to change this method
            //I think easiest way is to have the expression be separated by spaces, but just so it works with anything let's get rid of
            //any mathematical signs and then we should just have the name of the attributes we want.
            string temp = expression;
            string[] operators = new string[] { "+", "-", "/", "*", "(", ")", "[", "]", "{", "}", "^", "SQRT", "POW", "MAX" };
            foreach (string operand in operators) {
                temp = temp.Replace(operand, " ");
            }

            //need to get rid of repeats and empty spaces
            string[] attributeList = temp.Split(' ');

            //if you cocked your head to the side at this assignment, read over the code again.
            temp = expression;

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
                    else if (attributeName.Contains("Bonus")){ //this part does not exist in the Skill class method
                        string bonusName = attributeName.Replace("Bonus", "");
                        double replacementValue = player.GetBonus((CharacterEnums.BonusTypes)Enum.Parse(typeof(CharacterEnums.BonusTypes), bonusName));
                        temp = temp.Replace(attributeName, replacementValue.ToString());
                    }
                }
            }

            return temp;
        }


        private static void WeaponHandAttack(User.User player, User.User enemy, bool offHand = false) {            
            //Calculate the total damage           
            double defense = 0.0d;
            double attack = 0.0d;
            double damage = CalculateDamage(player, enemy, offHand, out defense, out attack); 
            Room room = Room.GetRoom(player.Player.Location);
            
            SendRoundOutcomeMessage(player, enemy, room, damage, defense, attack);

            UpdatePlayerState(player, enemy);
            
        }

        /// <summary>
        /// This method sets a player state to unconcious or dead based 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        private static void UpdatePlayerState(User.User player, User.User enemy) {
            if (enemy.Player.IsUnconcious()) SendDeadOrUnconciousMessage(player, enemy);
            if (enemy.Player.IsDead()) {
                SendDeadOrUnconciousMessage(player, enemy, true);

                Character.NPC npc = enemy.Player as Character.NPC;
                if (npc != null) {
                    npc.CalculateXP();
                    npc.Fsm.ChangeState(AI.Rot.GetState(), npc);
                    enemy.Player = npc;
                    enemy.Player.Save();
                }
            }
        }


        /// <summary>
        /// Informs all parties of the outcome of the combat round.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        /// <param name="room"></param>
        /// <param name="damage"></param>
        /// <param name="defense"></param>
        /// <param name="attack"></param>
        private static void SendRoundOutcomeMessage(User.User player, User.User enemy, Room room, double damage, double defense, double attack) {
            //TODO: Get the message based weapon type/special weapon (blunt, blade, axe, pole, etc.)
            //Get the weapon type and append it to the "Hit" or "Miss" type when getting the message 
            //ex: HitSword, HitClub, MissAxe, MissUnarmed could even get really specific HitRustyShortSword, MissLegendaryDaggerOfBlindness
            //Make a method to figure out the type by having a lookup table in the DB that points to a weapon type string
            if (damage < 0) {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "Hit", MessageType.Self), player, enemy, damage, defense, attack));
                enemy.MessageHandler(ParseMessage(GetMessage("Combat", "Hit", MessageType.Target), player, enemy, damage, defense, attack));
                string roomMessage = ParseMessage(GetMessage("Combat", "Hit", MessageType.Room), player, enemy, damage, defense, attack);

                room.InformPlayersInRoom(roomMessage, new List<string>(new string[] { player.UserID, enemy.UserID }));
                enemy.Player.ApplyEffectOnAttribute("Hitpoints", damage);

                Character.NPC npc = enemy.Player as Character.NPC;
                if (npc != null) {
                    npc.IncreaseXPReward(player.UserID, (damage * -1.0));
                }
            }
            else {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "Miss", MessageType.Self), player, enemy, damage, defense, attack));
                enemy.MessageHandler(ParseMessage(GetMessage("Combat", "Miss", MessageType.Target), player, enemy, damage, defense, attack));
                string roomMessage = ParseMessage(GetMessage("Combat", "Miss", MessageType.Room), player, enemy, damage, defense, attack);
                room.InformPlayersInRoom(roomMessage, new List<string>(new string[] { player.UserID, enemy.UserID }));
            }
        }

        private static double CalculateDamage(User.User player, User.User enemy, bool offHand, out double defense, out double attack) {
            defense = 0.0d;
            attack = 0.0d;
            attack = Math.Round(CalculateAttack(player, enemy.Player.Level, offHand), 2, MidpointRounding.AwayFromZero);
            defense = Math.Round(CalculateDefense(enemy), 2, MidpointRounding.AwayFromZero);
           
            return Math.Round((attack + defense), 2, MidpointRounding.AwayFromZero);
        }

        private static User.User GetTarget(User.User player, List<string> commands) {
            User.User enemy = FindTarget(player, commands);

            if (!SendMessageIfTargetUnavailable(player, enemy)) {
                return null;
            }       

            return enemy;
        }

        public static User.User FindTargetByName(string name, int location) {
            User.User enemy = null;
            foreach (User.User foe in MySockets.Server.GetAUserByFirstName(name)) {
                if (foe.Player.Location == location) {
                    enemy = foe;
                    break;
                }
            }

            if (enemy == null) {
                //ok it's not a player lets look through the NPC list
                foreach (Character.NPC npc in Character.NPCUtils.GetAnNPCByName(name, location)) {
                    User.User foe = new User.User(true);
                    foe.UserID = npc.ID;
                    foe.Player = npc;
                    enemy = foe;
                    break;
                }
            }

            return enemy;
        }

        /// <summary>
        /// Finds a possible target that matches by name or ID, for either player or NPC.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        private static User.User FindTarget(User.User player, List<string> commands) {
            User.User enemy = null;

            if (commands.Count > 2 && !string.Equals(commands[2], "target", StringComparison.InvariantCultureIgnoreCase)) {
                enemy = FindTargetByName(commands[2], player.Player.Location);
            }
            //couldn't find the target by name, now let's see if our current Target is around
            if (enemy == null) {
                enemy = MySockets.Server.GetAUser(player.Player.CurrentTarget);
            }

            //didn't find a player character so let's look for an npc
            if (enemy == null) {
                Character.Iactor npc = null;
                string[] position = commands[0].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                if (position.Count() > 1) {
                    //ok so the player specified a specific NPC in the room list to examine and not just the first to match
                    int pos;
                    int.TryParse(position[position.Count() - 1], out pos);
                    if (pos != 0) {
                        npc = Character.NPCUtils.GetAnNPCByID(GetObjectInPosition(pos, commands[2], player.Player.Location));
                    }
                }
                else {
                    npc = Character.NPCUtils.GetAnNPCByID(player.Player.CurrentTarget);
                }

                if (npc != null) {
                    User.User temp = new User.User(true);
                    temp.UserID = npc.ID;
                    temp.Player = npc;
                    enemy = temp;
                }
            }

            return enemy;
        }

        /// <summary>
        /// Send message out if target not found, not in same location or already dead.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        /// <returns></returns>
        private static bool SendMessageIfTargetUnavailable(User.User player, User.User enemy) {
            bool targetFound = true;

               if (enemy == null) { //target not found so no longer in combat
                //TODO: pull target not found message from DB
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetNotFound", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.LastTarget = player.Player.CurrentTarget; //set the current target to the last target the player had
                player.Player.CurrentTarget = null;

                if (player.Player.IsNPC) {
                    player.Player.Save(); //need to update the npc status in the DB
                }
                targetFound = false;
            }

            //can't fight when you are not in the same room
            if (targetFound && enemy.Player.Location != player.Player.Location) {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetNotInLocation", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.UpdateTarget(null);
                targetFound = false;
            }

            //before we get to fighting let's make sure this target isn't already dead
            if (targetFound && enemy.Player.IsDead()) {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetFoundDead", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.UpdateTarget(null);
                targetFound = false;
            }

            return targetFound;
        }

        /// <summary>
        /// Sets player being attacked to target the attacker and places both players into combat mode.
        /// Does not override any current targets the player being attacked may have.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        private static void TargetEachOther(User.User player, User.User enemy) {
            if (player.Player.InCombat == false || enemy.Player.InCombat == false) {
                player.Player.InCombat = true;
                enemy.Player.InCombat = true;
            }

            if (player.Player.CurrentTarget == null) {
                player.Player.UpdateTarget(enemy.UserID);
            }

            //the enemy may be being attacked by someone else if they have a target assigned already we don't want to override it
            //we'll let the AI portion handle which charcater that is attacking it should be it's target.  If the enemy is a player, they will
            //select which target they want to attack
            if (enemy.Player.CurrentTarget == null) {
                enemy.Player.UpdateTarget(player.UserID);
            }
        }

		#region Combat calculations
        
		private static double CalculateAttack(User.User player, int targetLevel, bool offHand = false){
            double result = 0.0;
            result = 100 - ((targetLevel - player.Player.Level + player.Player.GetAttributeRank("Strength") - 1) * 10);
            result *= 0.10;
            double weaponResult = WeaponDamage(player);
            result += weaponResult;
            double attributeResult = player.Player.GetAttributeValue("Strength") * GetAttributeRankModifier(player.Player.GetAttributeRank("Strength"));
            result += attributeResult / 10;
            result *= WeaponSkill(player);
	
			return result * -1;
		}

        private static double WeaponDamage(User.User player, bool offhand = false) {
            double result = 0.0d;
            List<Items.Iitem> weapons = player.Player.Equipment.GetWieldedWeapons();
            if (weapons.Count > 0) {
                Items.Iweapon weapon;
                if (!offhand) {
                    weapon = (Items.Iweapon)weapons.Where(w => w.WornOn.ToString().CamelCaseWord() == player.Player.MainHand.CamelCaseWord()).SingleOrDefault();
                }
                else {
                    weapon = (Items.Iweapon)weapons.Where(w => w.WornOn.ToString().CamelCaseWord() != player.Player.MainHand.CamelCaseWord()).SingleOrDefault();
                }

                result = RandomNumber.GetRandomNumber().NextNumber((int)weapon.CurrentMinDamage, (int)weapon.CurrentMaxDamage + 1);
            }
            else { 
                result = 0.0d; 
            }

            return result;
        }

        private static double GetAttributeRankModifier(int rank) {
            double result = 0.1d;
            if (rank > 2 && rank <= 5) result = 0.15;
            else if (rank > 5 && rank < 8) result = 0.2;
            else if (rank > 8) result = 0.25;

            return result;
        }

        private static double WeaponSkill(User.User player) {
            //TODO: This is the logic for the weapon skill portion just need to get the weapon skill tree set up
            //if (Math.Pow(WeaponSkill, 2)) > 0.5) return 0.5d;
            //else return Math.Pow(WeaponSkill, 2);
            return 0.25d;
        }

		private static double CalculateDefense(User.User enemy) {
			double result = 0.0;
            if (!enemy.Player.CheckUnconscious) {
                double gearResult = 0.0d; //should be GetGearHit + BonusHit;
                double hit = 0.0d; //this will ultimately be the max of Dexterity or Agility bonus
                hit = (hit + gearResult) / 100;
                hit += 0.4;
                double modifier = 5 * (enemy.Player.GetAttributeValue("Dexterity") - enemy.Player.GetAttributeValue("Strength"));
                result = (hit + modifier) / 100;
			}
			else { //if player is unconcious he's going to die in one hit, maybe make it more than -100?
				result = -100;
			}
			return result;
		}

		private static double CalculateDefense(Door door) {
            //TODO:some doors may have some resistance to damage add that here
			double result = 0.0;
			return result;
		}
		#endregion Combat calculations

		#region Combat State Changes
		/// <summary>
		/// Informs everyone of the state change the player just went through as a result of combat, either killed or knocked out
		/// </summary>
		/// <param name="player"></param>
		/// <param name="enemy"></param>
        /// 
        private static void SendDeadOrUnconciousMessage(User.User player, User.User enemy, bool dead = false) {
            player.Player.ClearTarget();
            string status = dead == true ? "Killed" : "KnockedUnconcious";
            player.MessageHandler(ParseMessage(GetMessage("Combat", status, MessageType.Self), player, enemy));
            player.MessageHandler(ParseMessage(GetMessage("Combat", status, MessageType.Target), player, enemy));
            Room.GetRoom(player.Player.Location).InformPlayersInRoom(ParseMessage(GetMessage("Combat", status, MessageType.Room), player, enemy), new List<string>(new string[] { player.UserID, enemy.UserID }));
			if (dead) {
				SetKiller(enemy, player);
			}
			SetCombatTimer(player, enemy);
        }

		private static void SetKiller(User.User enemy, User.User player) {
			enemy.Player.KillerID = player.UserID;
			enemy.Player.TimeOfDeath = DateTime.UtcNow;
		}
        		
		//set player timer to minvalue
		private static void SetCombatTimer(User.User player, User.User enemy) {
			player.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
			enemy.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
		}
		#endregion

		#region Finishing moves
		//finisher moves
        //these will actually be skill moves
		private static void Cleave(User.User player, List<string> commands) {//this will need a check in the future to be used with only bladed weapons
			User.User enemy = null;
            if (commands.Count > 2) {

                foreach (User.User foe in MySockets.Server.GetAUserByFirstName(commands[2])) {
                    if (foe.Player.Location == player.Player.Location) {
                        enemy = foe;
                    }
                }
            }
            else {//did not specify a name let's kill the first player we find unconcious in our same location
                enemy = MySockets.Server.GetCurrentUserList().Where(u => u.Player.Location == player.Player.Location && String.Compare(u.Player.ActionState.ToString(), "unconcious", true) == 0).SingleOrDefault();
            }

            if (enemy == null) {
                //ok it's not a player lets look through the NPC list
                foreach (Character.NPC npc in Character.NPCUtils.GetAnNPCByName(commands[2], player.Player.Location)) {
                    if (npc.ActionState == CharacterEnums.CharacterActionState.Unconcious) {
                        User.User foe = new User.User(true);
                        foe.UserID = npc.ID;
                        foe.Player = npc;
                        enemy = foe;
                        break;
                    }
                }
            }

			if (enemy == null) {
                player.MessageHandler("You can't kill what you can't see!");
				return;
			}
            Room room = Room.GetRoom(player.Player.Location);

			if (String.Compare(enemy.Player.ActionState.ToString(),"unconcious", true) == 0) {
				if (commands.Count > 3 && commands[3].ToLower() == "slowly") {  //a slow death for your opponent, bask in it.
                    player.MessageHandler(String.Format("You slowly drive your blade through {0}'s chest and twist it a few times as {1} lays on the ground unconcious.", enemy.Player.FirstName, enemy.Player.Gender == "Male" ? "he" : "she"));
                    enemy.MessageHandler(String.Format("{0} slowly drives {1} blade through your chest and twists it a few times as you lay on the ground unconcious.", player.Player.FirstName, player.Player.Gender == "Male" ? "his" : "her"));
					string roomMessage = String.Format("{0} slowly drives {1} blade through {2}'s chest and twists it a few times as {3} lay on the ground unconcious.", player.Player.FirstName, player.Player.Gender == "Male" ? "his" : "her", enemy.Player.FirstName, enemy.Player.Gender == "Male" ? "he" : "she");
					room.InformPlayersInRoom(roomMessage, new List<string>(new string[] {player.UserID, enemy.UserID})); 
; 
				}
				else {
                    player.MessageHandler(String.Format("You cleave {0} as {1} lays on the ground unconcious.", enemy.Player.FirstName, enemy.Player.Gender == "Male" ? "he" : "she"));
                    enemy.MessageHandler(String.Format("{0} cleaved you as you lay on the ground unconcious.", player.Player.FirstName));
					string roomMessage = String.Format("{0} cleaved {1} as {2} lay on the ground unconcious.", player.Player.FirstName, enemy.Player.FirstName, enemy.Player.Gender == "Male" ? "he" : "she");
					room.InformPlayersInRoom(roomMessage, new List<string>(new string[] {player.UserID, enemy.UserID})); 
				}
                enemy.Player.SetAttributeValue("Hitpoints", -100);
				//SetDead(player, enemy);
                Character.NPC npc = enemy.Player as Character.NPC;
                if (npc != null) {
                    if (npc.IsDead()) {
                        npc.Fsm.ChangeState(AI.Rot.GetState(), npc);
                    }
                }
			}
			else {
                player.MessageHandler(String.Format("You can't cleave {0}, {1} not unconcious.", enemy.Player.FirstName, enemy.Player.Gender == "Male" ? "he's" : "she's"));
			}
		}
		#endregion

        #region Combat Messages
        //replaces the placeholder with the actual value, so edits can all be made on the DB, if you add anymore place holders this is where you would do the replace
        public static string ParseMessage(string message, User.User attacker, User.User target, double damage = 0, double defense = 0, double attack = 0) {

            message = message.Replace("{attacker}", attacker.Player.FirstName)
                             .Replace("{damage}", (damage * -1).ToString().FontColor(Utils.FontForeColor.RED))
                             .Replace("{attack}", attack.ToString())
                             .Replace("{defense}", defense.ToString());
            
            if (target != null){
                message = message.Replace("{target}", target.Player.FirstName)
                                 .Replace("{him-her}", target.Player.Gender == "Male" ? "him" : "her")
                                 .Replace("{he-she}", target.Player.Gender == "Male" ? "he" : "she");
            }

            return message;
        }

        public enum MessageType { Self, Target, Room };

        public static string GetMessage(string collection, string type, MessageType to) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("Messages");
            MongoCollection table = db.GetCollection(collection.CamelCaseWord());
            IMongoQuery query = Query.EQ("_id", type.CamelCaseWord());

            var result = table.FindOneAs<BsonDocument>(query).AsBsonDocument;

            //this allows the message to have multiple different versions and we'll just pick one at random to give it more variance
            //and feel more immersive.
            BsonArray msg = result["Messages"][0][(int)to].AsBsonArray;

            int choice = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, msg.Count);
 
            BsonDocument message = msg[choice].AsBsonDocument;

            return message[0].AsString;

        }
        #endregion Combat Messages

        #region Break things

		private static void Break(User.User player, List<string> commands) {
			string objectName = "";

			for (int i = commands.Count -1; i > 0; i--){ //this should keep the words in the order we want
				objectName += commands[i];
			}
            bool brokeIt = false;
		    //let's see if it a door we want to break down first
            if (BreakDoor(player, commands)) {
                brokeIt = true;
            }
            else if (BreakObject(player, objectName)) {
                brokeIt = true;
            }

            if (!brokeIt) {
                //TODO: add this to the message collection in the DB
                player.MessageHandler(GetMessage("Messages", "NothingToBreak", MessageType.Self));             
            }
		}

		private static bool BreakDoor(User.User player, List<string> commands){
			Door door = FindDoor(player.Player.Location, commands);
			if (door == null) {
				return false;
			}

			if (door.Destroyed) {
                player.MessageHandler(GetMessage("Messages", "AlreadyBroken", MessageType.Self));
				return true;
			}

		    double attack = CalculateAttack(player, 0);
			double defense = CalculateDefense(door);
			double damage = attack - defense;
			List<string> message = door.ApplyDamage(damage);
			door.UpdateDoorStatus();
            player.MessageHandler(message[0].FontColor(Utils.FontForeColor.RED));
			Rooms.Room.GetRoom(player.Player.Location).InformPlayersInRoom(String.Format(message[1], player.Player.FirstName), new List<string>(new string[] {player.UserID}));
			return true;
		}

		private static bool BreakObject(User.User player, string objectName) {
            //TODO: finish this method now that we have objects in the game that can be destroyed
            //find item in the DB, check if destructible.
            //Calculate player attack and determine damage to item
            //return message.
			return false;
		}
		#endregion

      
	}
} 
