using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Sockets;

namespace Commands {
	public partial class CommandParser {

        //this may be deprecated in the future and instead rely on executing the commands by calling C# scripts in the DB
        
        private delegate void CommandDelegate(IUser player, List<string> command); //we just call this guy from now on

        private static Dictionary<string, CommandDelegate> MovementCommands;
        private static Dictionary<string, CommandDelegate> VisualCommands;
        private static Dictionary<string, CommandDelegate> PlayerCommands;
        private static Dictionary<string, CommandDelegate> CombatCommands;
        private static Dictionary<string, CommandDelegate> GeneralCommands;
        private static Dictionary<string, CommandDelegate> ItemCommands;

        private static List<Dictionary<string, CommandDelegate>> CommandsList;


		//this is where all the commands will go just follow whats already here
		static public void LoadUpCommandDictionary() {

			MovementCommands = new Dictionary<string, CommandDelegate>();
			CombatCommands = new Dictionary<string, CommandDelegate>();
			PlayerCommands = new Dictionary<string, CommandDelegate>();
			VisualCommands = new Dictionary<string, CommandDelegate>();
            GeneralCommands = new Dictionary<string, CommandDelegate>();
            ItemCommands = new Dictionary<string, CommandDelegate>();

            //Item commands for interacting with them
            ItemCommands.Add("GRAB", new CommandDelegate(Get));
            ItemCommands.Add("PICKUP", new CommandDelegate(Get));
            ItemCommands.Add("DROP", new CommandDelegate(Drop));
            ItemCommands.Add("UNEQUIP", new CommandDelegate(Unequip));
            ItemCommands.Add("EQUIP", new CommandDelegate(Equip));
            ItemCommands.Add("WIELD", new CommandDelegate(Wield));
            ItemCommands.Add("UNWIELD", new CommandDelegate(Unequip));
            ItemCommands.Add("EAT", new CommandDelegate(Eat));
            ItemCommands.Add("DRINK", new CommandDelegate(Drink));
            ItemCommands.Add("PUT", new CommandDelegate(Put));
            ItemCommands.Add("GET", new CommandDelegate(Get));
            ItemCommands.Add("IGNITE", new CommandDelegate(Activate));
            ItemCommands.Add("TURNON", new CommandDelegate(Activate));
            ItemCommands.Add("ACTIVATE", new CommandDelegate(Activate));
            ItemCommands.Add("SWITCHON", new CommandDelegate(Activate));
            ItemCommands.Add("EXTINGUISH", new CommandDelegate(DeActivate));
            ItemCommands.Add("TURNOFF", new CommandDelegate(DeActivate));
            ItemCommands.Add("DEACTIVATE", new CommandDelegate(DeActivate));
            ItemCommands.Add("SWITCHOFF", new CommandDelegate(DeActivate));
			ItemCommands.Add("LOOT", new CommandDelegate(Loot));
			ItemCommands.Add("GIVE", new CommandDelegate(Give));
            ItemCommands.Add("WEAR", new CommandDelegate(Equip));
            ItemCommands.Add("REMOVE", new CommandDelegate(Unequip));


            //General commands (biggest of the list)
            GeneralCommands.Add("BUG", new CommandDelegate(ReportBug));
            GeneralCommands.Add("HIDE", new CommandDelegate(PerformSkill));
            GeneralCommands.Add("UNCONCEAL", new CommandDelegate(PerformSkill));
            GeneralCommands.Add("SPOT", new CommandDelegate(PerformSkill));
            GeneralCommands.Add("SNEAK", new CommandDelegate(PerformSkill));
            GeneralCommands.Add("PEEK", new CommandDelegate(PerformSkill));
			GeneralCommands.Add("GROUP", new CommandDelegate(Group));

			//Movement Commands
			MovementCommands.Add("NORTH", new CommandDelegate(Move));
			MovementCommands.Add("N", new CommandDelegate(Move));
			MovementCommands.Add("SOUTH", new CommandDelegate(Move));
			MovementCommands.Add("S", new CommandDelegate(Move));
			MovementCommands.Add("EAST", new CommandDelegate(Move));
			MovementCommands.Add("E", new CommandDelegate(Move));
			MovementCommands.Add("WEST", new CommandDelegate(Move));
			MovementCommands.Add("W", new CommandDelegate(Move));
			MovementCommands.Add("UP", new CommandDelegate(Move));
			MovementCommands.Add("U", new CommandDelegate(Move));
			MovementCommands.Add("DOWN", new CommandDelegate(Move));
			MovementCommands.Add("D", new CommandDelegate(Move));
			MovementCommands.Add("OPEN", new CommandDelegate(Open));
			MovementCommands.Add("CLOSE", new CommandDelegate(Close));
			MovementCommands.Add("LOCK", new CommandDelegate(Lock));
			MovementCommands.Add("UNLOCK", new CommandDelegate(Unlock));
			MovementCommands.Add("SIT", new CommandDelegate(Sit));
			MovementCommands.Add("STAND", new CommandDelegate(Stand));
			MovementCommands.Add("LAY", new CommandDelegate(Prone));
			MovementCommands.Add("PRONE", new CommandDelegate(Prone));

			//Visual commands
			VisualCommands.Add("LOOK", new CommandDelegate(Look));
			VisualCommands.Add("DATE", new CommandDelegate(DisplayDate));
			VisualCommands.Add("TIME", new CommandDelegate(DisplayTime));
            VisualCommands.Add("EXAMINE", new CommandDelegate(Examine));

			//Player information commands
			PlayerCommands.Add("STATS", new CommandDelegate(DisplayStats));
			PlayerCommands.Add("SAY", new CommandDelegate(Say));
			PlayerCommands.Add("SAYTO", new CommandDelegate(SayTo));
			PlayerCommands.Add("WHISPER", new CommandDelegate(Whisper));
			PlayerCommands.Add("TELL", new CommandDelegate(Tell));
			PlayerCommands.Add("EMOTE", new CommandDelegate(Emote));
			PlayerCommands.Add("WHO", new CommandDelegate(Who));
			PlayerCommands.Add("HELP", new CommandDelegate(Help));
            PlayerCommands.Add("LEVEL", new CommandDelegate(LevelUp));
            PlayerCommands.Add("INVENTORY", new CommandDelegate(Inventory));
            PlayerCommands.Add("EQUIPMENT", new CommandDelegate(Equipment));

			//Combat Commands
			CombatCommands.Add("ATTACK", new CommandDelegate(Kill));
			CombatCommands.Add("KILL", new CommandDelegate(Kill));
			CombatCommands.Add("CLEAVE", new CommandDelegate(Cleave));
			CombatCommands.Add("DESTROY", new CommandDelegate(Break));
			CombatCommands.Add("BREAK", new CommandDelegate(Break));
            			
			//we don't want to add the combat dictionary to the list because we are already checking it before any of these
			CommandsList = new List<Dictionary<string, CommandDelegate>>();
			CommandsList.Add(VisualCommands);
			CommandsList.Add(PlayerCommands);
			CommandsList.Add(MovementCommands);
            CommandsList.Add(GeneralCommands);
            CommandsList.Add(ItemCommands);
		}
	}
}
