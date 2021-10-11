using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Extensions;
using Sockets;
using Interfaces;
using Rooms;

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


        private static double GetBonus(IUser player, BonusTypes type) {
            return player.Player.GetBonus(type);
        }      

        //this is where attacks dones from scripts will call into
        //this can just set the round timer and apply the damages       
        private static void SpecialAttack(IUser player) {
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
        private static void Kill(IUser player, List<string> commands) {

            //Has the round interval time elapsed?
            if (!CheckIfCanAttack(player.Player.LastCombatTime)) {
                return;
            }

            //no target, no fight
            IUser enemy = GetTarget(player, commands);
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
            Wearable mainHand = player.Player.Equipment.GetMainHandWeapon(player.Player);

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

        private static double PercentHit(IUser player, IUser enemy) {
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

        private static double GetAndEvaluateExpression(string calculationName, IActor player) {
            var col = MongoUtils.MongoData.GetCollection<BsonDocument>("Calculations", "Combat");
            var doc = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(col, c => c["_id"] == calculationName).Result;
            NCalc.Expression expression = new NCalc.Expression(ReplaceStringWithNumber(player, doc["Expression"].AsString));
            double expressionResult = (double)expression.Evaluate();
            //let's take into consideration some other factors.  Visibility, stance, etc.
            //TODO: add that here.  They only take away from chance to hit. But some of them can be negated.
            //if it's dark but player can see in the dark for example.
            return expressionResult;
        }

        //TODO:
        //This method exists in the Skill class, should probably combine them into a math library or something
        private static string ReplaceStringWithNumber(IActor player, string expression) {
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
                    if (player.GetAttributes().Any(a => a.Name == attributeName.CamelCaseWord())) {
                        temp = temp.Replace(attributeName, player.GetAttributeValue(attributeName).ToString());
                    }
                    else if (player.GetSubAttributes().ContainsKey(attributeName)) {
                        temp = temp.Replace(attributeName, player.GetSubAttributes()[attributeName].ToString());
                    }
                    else if (attributeName.Contains("Rank")) {
                        temp = temp.Replace(attributeName, player.GetAttributeRank(attributeName.Substring(0, attributeName.Length - 4)).ToString());
                    }
                    else if (attributeName.Contains("Bonus")){ //this part does not exist in the Skill class method
                        string bonusName = attributeName.Replace("Bonus", "");
                        double replacementValue = player.GetBonus((BonusTypes)Enum.Parse(typeof(BonusTypes), bonusName));
                        temp = temp.Replace(attributeName, replacementValue.ToString());
                    }
                }
            }

            return temp;
        }


        private static void WeaponHandAttack(IUser player, IUser enemy, bool offHand = false) {            
            //Calculate the total damage           
            double defense = 0.0d;
            double attack = 0.0d;
            double damage = CalculateDamage(player, enemy, offHand, out defense, out attack); 
            IRoom room = Room.GetRoom(player.Player.Location);
            
            SendRoundOutcomeMessage(player, enemy, room, damage, defense, attack);

            UpdatePlayerState(player, enemy);
            
        }

        /// <summary>
        /// This method sets a player state to unconcious or dead based 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        private static void UpdatePlayerState(IUser player, IUser enemy) {
            if (enemy.Player.IsUnconcious()) SendDeadOrUnconciousMessage(player, enemy);
            if (enemy.Player.IsDead()) {
                SendDeadOrUnconciousMessage(player, enemy, true);

                INPC npc = enemy.Player as INPC;
                if (npc != null) {
                    npc.CalculateXP();
                    npc.Fsm.ChangeState(AI.Rot.GetState(), (IActor)npc);
                    enemy.Player = npc as IActor;
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
        private static void SendRoundOutcomeMessage(IUser player, IUser enemy, IRoom room, double damage, double defense, double attack) {
			//TODO: Get the message based weapon type/special weapon (blunt, blade, axe, pole, etc.)
			//Get the weapon type and append it to the "Hit" or "Miss" type when getting the message 
			//ex: HitSword, HitClub, MissAxe, MissUnarmed could even get really specific HitRustyShortSword, MissLegendaryDaggerOfBlindness
			//Make a method to figure out the type by having a lookup table in the DB that points to a weapon type string
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
			message.TargetID = enemy.UserID.ToString();
			message.TargetType = enemy.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (damage < 0) {
                message.Self = ParseMessage(GetMessage("Combat", "Hit", MessageType.Self), player, enemy, damage, defense, attack);
                message.Target = ParseMessage(GetMessage("Combat", "Hit", MessageType.Target), player, enemy, damage, defense, attack);
                message.Room = ParseMessage(GetMessage("Combat", "Hit", MessageType.Room), player, enemy, damage, defense, attack);

				enemy.Player.ApplyEffectOnAttribute("Hitpoints", damage);

				INPC npc = enemy.Player as INPC;
				if (npc != null) {
					npc.IncreaseXPReward(player.UserID, (damage * -1.0));
				}
            }
            else {
				message.Self = ParseMessage(GetMessage("Combat", "Miss", MessageType.Self), player, enemy, damage, defense, attack);
                message.Target =  ParseMessage(GetMessage("Combat", "Miss", MessageType.Target), player, enemy, damage, defense, attack);
                message.Room = ParseMessage(GetMessage("Combat", "Miss", MessageType.Room), player, enemy, damage, defense, attack);
            }

			if (player.Player.IsNPC) {
				player.MessageHandler(message);
			}
			else {
				player.MessageHandler(message.Self);
			}

			if (enemy.Player.IsNPC) {
				enemy.MessageHandler(message);
			}
			else {
				enemy.MessageHandler(message.Target);
			}
			
			room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID, enemy.UserID });
		}

        private static double CalculateDamage(IUser player, IUser enemy, bool offHand, out double defense, out double attack) {
            defense = 0.0d;
            attack = 0.0d;
            attack = Math.Round(CalculateAttack(player, enemy.Player.Level, offHand), 2, MidpointRounding.AwayFromZero);
            defense = Math.Round(CalculateDefense(enemy), 2, MidpointRounding.AwayFromZero);
           
            return Math.Round((attack + defense), 2, MidpointRounding.AwayFromZero);
        }

        private static IUser GetTarget(IUser player, List<string> commands) {
            IUser enemy = FindTarget(player, commands);

            if (!SendMessageIfTargetUnavailable(player, enemy)) {
                return null;
            }       

            return enemy;
        }

        public static IUser FindTargetByName(string name, string location) {
            IUser enemy = null;
            foreach (IUser foe in Server.GetAUserByFirstName(name)) {
                if (foe.Player.Location == location) {
                    enemy = foe;
                    break;
                }
            }

            if (enemy == null) {
                //ok it's not a player lets look through the NPC list
                foreach (INPC npc in Character.NPCUtils.GetAnNPCByName(name, location)) {
                    IUser foe = new User(true);
                    foe.UserID = ((NPC)npc).Id;
                    foe.Player = npc as IActor;
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
        private static IUser FindTarget(IUser player, List<string> commands) {
            IUser enemy = null;

            if (commands.Count > 2 && !string.Equals(commands[2], "target", StringComparison.InvariantCultureIgnoreCase)) {
                enemy = FindTargetByName(commands[2], player.Player.Location);
            }
            //couldn't find the target by name, now let's see if our current Target is around
            if (enemy == null) {
                enemy = Server.GetAUser(player.Player.CurrentTarget);
            }

            //didn't find a player character so let's look for an npc
            if (enemy == null) {
                IActor npc = null;
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
                    IUser temp = new User(true);
                    temp.UserID = npc.Id;
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
        private static bool SendMessageIfTargetUnavailable(IUser player, IUser enemy) {
            bool targetFound = true;

               if (enemy == null) { //target not found so no longer in combat
                //TODO: pull target not found message from DB
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetNotFound", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.LastTarget = player.Player.CurrentTarget; //set the current target to the last target the player had
                player.Player.CurrentTarget = ObjectId.Empty;

                if (player.Player.IsNPC) {
                    player.Player.Save(); //need to update the npc status in the DB
                }
                targetFound = false;
            }

            //can't fight when you are not in the same room
            if (targetFound && enemy.Player.Location != player.Player.Location) {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetNotInLocation", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.UpdateTarget(ObjectId.Empty);
                targetFound = false;
            }

            //before we get to fighting let's make sure this target isn't already dead
            if (targetFound && enemy.Player.IsDead()) {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "TargetFoundDead", MessageType.Self), player, enemy));
                player.Player.InCombat = false;
                player.Player.UpdateTarget(ObjectId.Empty);
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
        private static void TargetEachOther(IUser player, IUser enemy) {
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
        
		private static double CalculateAttack(IUser player, int targetLevel, bool offHand = false){
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

        private static double WeaponDamage(IUser player, bool offhand = false) {
            double result = 0.0d;
            List<IItem> weapons = player.Player.Equipment.GetWieldedWeapons();
            if (weapons.Count > 0) {
                IWeapon weapon;
                if (!offhand) {
                    weapon = (IWeapon)weapons.Where(w => w.WornOn.ToString().CamelCaseWord() == player.Player.MainHand.CamelCaseWord()).SingleOrDefault();
                }
                else {
                    weapon = (IWeapon)weapons.Where(w => w.WornOn.ToString().CamelCaseWord() != player.Player.MainHand.CamelCaseWord()).SingleOrDefault();
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

        private static double WeaponSkill(IUser player) {
            //TODO: This is the logic for the weapon skill portion just need to get the weapon skill tree set up
            //if (Math.Pow(WeaponSkill, 2)) > 0.5) return 0.5d;
            //else return Math.Pow(WeaponSkill, 2);
            return 0.25d;
        }

		private static double CalculateDefense(IUser enemy) {
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

		private static double CalculateDefense(IDoor door) {
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
        private static void SendDeadOrUnconciousMessage(IUser player, IUser enemy, bool dead = false) {
            player.Player.ClearTarget();
            string status = dead == true ? "Killed" : "KnockedUnconcious";
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;
			message.TargetID = enemy.UserID.ToString();
			message.TargetType = enemy.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			message.Self = ParseMessage(GetMessage("Combat", status, MessageType.Self), player, enemy);
            message.Target = ParseMessage(GetMessage("Combat", status, MessageType.Target), player, enemy);
			message.Room = ParseMessage(GetMessage("Combat", status, MessageType.Room), player, enemy);
            Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>() { player.UserID, enemy.UserID });
			if (dead) {
				SetKiller(enemy, player);
			}
			SetCombatTimer(player, enemy);
        }

		private static void SetKiller(IUser enemy, IUser player) {
			enemy.Player.KillerID = player.UserID;
			enemy.Player.TimeOfDeath = DateTime.UtcNow;
		}
        		
		//set player timer to minvalue
		private static void SetCombatTimer(IUser player, IUser enemy) {
			player.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
			enemy.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
		}
		#endregion

		#region Finishing moves
		//finisher moves
        //these will actually be skill moves
		private static void Cleave(IUser player, List<string> commands) {//this will need a check in the future to be used with only bladed weapons
			IUser enemy = null;
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

            if (commands.Count > 2) {

                foreach (IUser foe in Server.GetAUserByFirstName(commands[2])) {
                    if (foe.Player.Location == player.Player.Location) {
                        enemy = foe;
                    }
                }
            }
            else {//did not specify a name let's kill the first player we find unconcious in our same location
                enemy = Server.GetCurrentUserList().Where(u => u.Player.Location == player.Player.Location && String.Compare(u.Player.ActionState.ToString(), "unconcious", true) == 0).SingleOrDefault();
            }

            if (enemy == null) {
                //ok it's not a player lets look through the NPC list
                foreach (IActor npc in Character.NPCUtils.GetAnNPCByName(commands[2], player.Player.Location)) {
                    if (npc.ActionState == CharacterActionState.Unconcious) {
                        IUser foe = new User(true);
                        foe.UserID = npc.Id;
                        foe.Player = npc;
                        enemy = foe;
                        break;
                    }
                }
            }

			if (enemy == null) {
				message.Self = "You can't kill what you can't see!";
				
			}
			else {
				IRoom room = Room.GetRoom(player.Player.Location);
				message.TargetID = enemy.UserID.ToString();
				message.TargetType = enemy.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

				if (String.Compare(enemy.Player.ActionState.ToString(), "unconcious", true) == 0) {
					if (commands.Count > 3 && commands[3].ToLower() == "slowly") {  //a slow death for your opponent, bask in it.
						message.Self = String.Format("You slowly drive your blade through {0}'s chest and twist it a few times as {1} lays on the ground unconcious.", enemy.Player.FirstName, enemy.Player.GenderPossesive.ToLower());
						message.Target = String.Format("{0} slowly drives {1} blade through your chest and twists it a few times as you lay on the ground unconcious.", player.Player.FirstName, player.Player.Gender == Genders.Male ? "his" : "her");
						message.Room = String.Format("{0} slowly drives {1} blade through {2}'s chest and twists it a few times as {3} lay on the ground unconcious.", player.Player.FirstName, player.Player.Gender == Genders.Male ? "his" : "her", enemy.Player.FirstName, enemy.Player.GenderPossesive.ToLower());
					}
					else {
						message.Self = String.Format("You cleave {0} as {1} lays on the ground unconcious.", enemy.Player.FirstName, enemy.Player.Gender == Genders.Male ? "he" : "she");
						message.Target = String.Format("{0} cleaved you as you lay on the ground unconcious.", player.Player.FirstName);
						message.Room = String.Format("{0} cleaved {1} as {2} lay on the ground unconcious.", player.Player.FirstName, enemy.Player.FirstName, enemy.Player.Gender == Genders.Male ? "he" : "she");
					}
					enemy.Player.SetAttributeValue("Hitpoints", -100);
					//SetDead(player, enemy);
					NPC npc = enemy.Player as NPC;
					if (npc != null) {
						if (npc.IsDead()) {
							npc.Fsm.ChangeState(AI.Rot.GetState(), npc);
						}
					}
				}
				else {
					message.Self = String.Format("You can't cleave {0}, {1} not unconcious.", enemy.Player.FirstName, enemy.Player.GenderPossesive.ToLower() + "'s");
				}
				if (player.Player.IsNPC) {
					player.MessageHandler(message);
				}
				else {
					player.MessageHandler(message.Self);
				}
				if (enemy.Player.IsNPC) {
					enemy.MessageHandler(message);
				}
				else {
					enemy.MessageHandler(message.Target);
				}

				room.InformPlayersInRoom(message, new List<ObjectId>() { player.UserID, enemy.UserID });
			}
		}
		#endregion

        #region Combat Messages
        //replaces the placeholder with the actual value, so edits can all be made on the DB, if you add anymore place holders this is where you would do the replace
        public static string ParseMessage(string message, IUser attacker, IUser target, double damage = 0, double defense = 0, double attack = 0) {

            message = message.Replace("{attacker}", attacker.Player.FirstName)
                             .Replace("{damage}", (damage * -1).ToString().FontColor(Utils.FontForeColor.RED))
                             .Replace("{attack}", attack.ToString())
                             .Replace("{defense}", defense.ToString());
            
            if (target != null){
                message = message.Replace("{target}", target.Player.FirstName)
                                 .Replace("{him-her}", target.Player.Gender == Genders.Male ? "him" : "her")
                                 .Replace("{he-she}", target.Player.GenderPossesive.ToLower());
            }

            return message;
        }

        public enum MessageType { Self, Target, Room };

        public static string GetMessage(string collection, string type, MessageType to) {
            var messages = MongoUtils.MongoData.GetCollection<BsonDocument>("Messages", collection.CamelCaseWord());
            var result = MongoUtils.MongoData.RetrieveObjectAsync<BsonDocument>(messages, m => m["_id"] == type.CamelCaseWord()).Result;

            //this allows the message to have multiple different versions and we'll just pick one at random to give it more variance
            //and feel more immersive.
            if (result != null)
            {
                BsonArray msg = result["Messages"][0][(int)to].AsBsonArray;
                int choice = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, msg.Count);
                BsonDocument message = msg[choice].AsBsonDocument;

                return message[0].AsString;
            }

            return string.Empty;

        }
        #endregion Combat Messages

        #region Break things

		private static void Break(IUser player, List<string> commands) {
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

		private static bool BreakDoor(IUser player, List<string> commands){
			IDoor door = FindDoor(player.Player.Location, commands);
			IMessage message = new Message();
			message.InstigatorID = player.UserID.ToString();
			message.InstigatorType = player.Player.IsNPC == false ? ObjectType.Player : ObjectType.Npc;

			if (door == null) {
				return false;
			}

			if (door.Destroyed) {
				message.Self = GetMessage("Messages", "AlreadyBroken", MessageType.Self);

			}
			else {
				double attack = CalculateAttack(player, 0);
				double defense = CalculateDefense(door);
				double damage = attack - defense;
				List<string> messages = door.ApplyDamage(damage);
				door.UpdateDoorStatus();
				message.Self = messages[0].FontColor(Utils.FontForeColor.RED);
				message.Room = String.Format(messages[1], player.Player.FirstName);
                Room.GetRoom(player.Player.Location).InformPlayersInRoom(message, new List<ObjectId>() { player.UserID });
			}
			return true;
		}

		private static bool BreakObject(IUser player, string objectName) {
            //TODO: finish this method now that we have objects in the game that can be destroyed
            //find item in the DB, check if destructible.
            //Calculate player attack and determine damage to item
            //return message.
			return false;
		}
		#endregion

      
	}
} 
