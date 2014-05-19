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

        public static User.User FindTargetByName(string name, int location){
            User.User enemy = null;
                    foreach (User.User foe in MySockets.Server.GetAUserByName(name)) {
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
            
        //I've come to the realization that this kill method actually needs to be a loop for the available attacks that call the kill method
        //with either main hand or off-hand.

		//this method is way too long and needs to be broken down a little bit into a few method calls but yeah it does a lot of shit
		private static void Kill(User.User player, List<string> commands) {

            TimeSpan wait = DateTime.Now.ToUniversalTime() - player.Player.LastCombatTime;
            if (wait.TotalSeconds < 5) {
                return; //no combat this round, this will eventually depend on their dexterity stat.
            }
            
            User.User enemy = GetTarget(player, commands);
            if (enemy == null) return;

            TargetEachOther(player, enemy);

            Items.Wearable mainHand = player.Player.GetMainHandWeapon();

            WeaponHandAttack(player, enemy); //pass in also which hand they're attacking with? Yes. Main hand attack
            if (player.Player.GetWieldedWeapons().Count == 2) {
                //if they are wielding a weapon in their opposite hand then attack with it.
                WeaponHandAttack(player, enemy, true); //off-hand attack
            }
            
            //combat timestamp, this time will increase/decrease depending on the dexterity so that the next round happens sooner or later
            player.Player.LastCombatTime = DateTime.Now.ToUniversalTime();
            
            //save as we progress through the fight
            enemy.Player.Save();
            player.Player.Save();  
		}

        private static void Assassinate(User.User player, List<string> commands) {
            if (player.Player.ActionState == CharacterEnums.CharacterActionState.SNEAKING || player.Player.ActionState == CharacterEnums.CharacterActionState.HIDING) {
                player.MessageHandler("If the dev had only finished this, you would have so killed whoever your target was.");
            }
            else {
                player.MessageHandler("You can only assassinate someone when you are hidden and have not been detected.");
            }
        }

        private static void WeaponHandAttack(User.User player, User.User enemy, bool offHand = false) {
            //Finally the good stuff
            //This here is half a round of combat when we parse the other players commands they will be the attackers
            //yes, it pays off to be the first one to strike.
            //Off-hand attacks.  If skillfull enough you can do off-hand attacks (maybe a perk/feat or weapon skill level)
            //if off-hand capable loop through this thing twice
            double attack = Math.Round(CalculateAttack(player, enemy.Player.Level, offHand), 2, MidpointRounding.AwayFromZero);
            double defense = Math.Round(CalculateDefense(enemy), 2, MidpointRounding.AwayFromZero);
            double damage = Math.Round((attack + defense), 2, MidpointRounding.AwayFromZero);
            Room room = Room.GetRoom(player.Player.Location);
            //TODO: Get the message based weapon type/special weapon (blunt, blade, axe, pole, etc.)
            if (damage < 0) { 
                player.MessageHandler(ParseMessage(GetMessage("Combat", "Hit", MessageType.SELF), player, enemy, damage, defense, attack));
                enemy.MessageHandler(ParseMessage(GetMessage("Combat", "Hit", MessageType.TARGET), player, enemy, damage, defense, attack));
                string roomMessage = ParseMessage(GetMessage("Combat", "Hit", MessageType.OTHERS), player, enemy, damage, defense, attack);

                room.InformPlayersInRoom(roomMessage, new List<string>(new string[] { player.UserID, enemy.UserID }));
                enemy.Player.ApplyEffectOnAttribute("Hitpoints", damage);

                Character.NPC npc = enemy.Player as Character.NPC;
                if (npc != null) {
                    npc.IncreaseXPReward(player.UserID, (damage * -1.0));
                }
            }
            else {
                player.MessageHandler(ParseMessage(GetMessage("Combat", "Miss", MessageType.SELF), player, enemy, damage, defense, attack));
                enemy.MessageHandler(ParseMessage(GetMessage("Combat", "Miss", MessageType.TARGET), player, enemy, damage, defense, attack));
                string roomMessage = ParseMessage(GetMessage("Combat", "Miss", MessageType.OTHERS), player, enemy, damage, defense, attack);
                room.InformPlayersInRoom(roomMessage, new List<string>(new string[] { player.UserID, enemy.UserID }));
            }

            if (enemy.Player.IsUnconcious()) SetUnconcious(player, enemy);
            if (enemy.Player.IsDead()) {
                SetDead(player, enemy);

                Character.NPC npc = enemy.Player as Character.NPC;
                if (npc != null) {
                    npc.CalculateXP();
                    npc.Fsm.ChangeState(AI.Rot.GetState(), npc);
                    enemy.Player = npc;
                    enemy.Player.Save();
                }
            }
        }

        private static User.User GetTarget(User.User player, List<string> commands) {
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

            if (enemy == null) { //target not found so no longer in combat
                //TODO: pull target not found message from DB
                player.MessageHandler("You can't kill what you can't see!");
                player.Player.InCombat = false;
                player.Player.LastTarget = player.Player.CurrentTarget; //set the current target to the last target the player had
                player.Player.CurrentTarget = null;

                if (player.Player.IsNPC) {
                    player.Player.Save(); //need to update the npc status in the DB
                }
                return null;
            }

            //can't fight when you are not in the same room
            if (enemy.Player.Location != player.Player.Location) {
                player.MessageHandler("Your target is no longer here for you to attack.");
                player.Player.InCombat = false;
                player.Player.UpdateTarget(null);
            }

            //before we get to fighting let's make sure this target isn't already dead
            if (enemy.Player.IsDead()) {
                player.MessageHandler(enemy.Player.GenderPossesive + " is already dead");
                player.Player.InCombat = false;
                player.Player.UpdateTarget(null);
                return null;
            }

            return enemy;
        }

        private static void TargetEachOther(User.User player, User.User enemy) {
            //set both players to fighting status if not already fighting
            //both players should be in combat regardless if the target will attack the attacker.
            //I may remove the auto-combat and make it maybe a bit twitch based combat.  A player could then choose
            //to go several rounds wihtout hitting the other player or just parrying them constantly.
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
            List<Items.Iitem> weapons = player.Player.GetWieldedWeapons();
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
            //TODO: This is th elogic for the weaponskill portion just need to get the weaponskill tree set up
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
		//state changes for combat
        //I think this is now getting handled by the actor abstract class now
		private static void SetUnconcious(User.User player, User.User enemy) {
            player.Player.ClearTarget();
            player.MessageHandler(String.Format("You knocked {0} unconcious.", enemy.Player.FirstName));
            enemy.MessageHandler(String.Format("{0} knocked you unconcious.", player.Player.FirstName));
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(String.Format("{0} knocked {1} unconcious.", player.Player.FirstName, enemy.Player.FirstName), new List<string>(new string[] { player.UserID, enemy.UserID }));
			SetCombatTimer(player, enemy);
		}

		private static void SetDead(User.User player, User.User enemy) {
            player.Player.ClearTarget();
            player.MessageHandler(String.Format("You have killed {0}!", enemy.Player.FirstName));
            enemy.MessageHandler(String.Format("{0} has killed you!", player.Player.FirstName));
			Room.GetRoom(player.Player.Location).InformPlayersInRoom(String.Format("{0} killed {1}.", player.Player.FirstName, enemy.Player.FirstName), new List<string>(new string[] { player.UserID, enemy.UserID }));
            SetCombatTimer(player, enemy);
		}

		//set player timer to minvalue
		private static void SetCombatTimer(User.User player, User.User enemy) {
			player.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
			enemy.Player.LastCombatTime = DateTime.MinValue.ToUniversalTime();
		}
		#endregion

		#region Finishing moves
		//finisher moves
		private static void Cleave(User.User player, List<string> commands) {//this will need a check in the future to be used with only bladed weapons
			User.User enemy = null;
            if (commands.Count > 2) {

                foreach (User.User foe in MySockets.Server.GetAUserByName(commands[2])) {
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
                    if (npc.ActionState == CharacterEnums.CharacterActionState.UNCONCIOUS) {
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
				SetDead(player, enemy);
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
                                 .Replace("{him-her}", target.Player.Gender == "Male" ? "him" : "her");
            }

            return message;
        }

        public enum MessageType { SELF, TARGET, OTHERS };

        public static string GetMessage(string collection, string type, MessageType to) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("Messages");
            MongoCollection table = db.GetCollection(collection.CamelCaseWord());
            IMongoQuery query = Query.EQ("_id", type.CamelCaseWord());

            var result = table.FindOneAs<BsonDocument>(query).AsBsonDocument;

            BsonArray msg = result["Messages"][0][(int)to].AsBsonArray;

            int choice = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, msg.Count);
 
            BsonDocument message = msg[choice].AsBsonDocument;

            return message[0].AsString;

        }
        #endregion Combat Messages

        #region Break shit
        private static void Destroy(User.User player, List<string> commands) {
            Break(player, commands);
		}

		private static void Break(User.User player, List<string> commands) {
			string objectName = "";

			for (int i = commands.Count -1; i > 0; i--){ //this should keep the words in the order we want
				objectName += commands[i];
			}

		    //let's see if it a door we want to break down first
			if (BreakDoor(player, commands)) return;
			if	(BreakObject(player, objectName)) return;

            player.MessageHandler("You look all around but can't find that to break.");
			
		}

		private static bool BreakDoor(User.User player, List<string> commands){
			Door door = FindDoor(player.Player.Location, commands);
			if (door == null) {
				return false;
			}

			if (door.Destroyed) {
                player.MessageHandler("It is already destroyed, why bother?");
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
			return false;
		}
		#endregion
	}
} 
