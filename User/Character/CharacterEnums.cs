using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharacterEnums {

	public enum CharacterType { PLAYER, NPC, MOB }
	public enum CharacterRace { HUMAN, ORC, DWARF, ELF }

	//uses bitwise operator to make it more readable, output is the same as setting them to 0x1,0x2,0x4,0x8,0x10, etc.
	//doing it this way should allow to players to be several different types of characters at once (Do I want this?)
	[Flags]
	public enum CharacterClass {
		FIGHTER = 1 << 1,
		EXPLORER = 1 << 2,
		ENGINEER = 1 << 3,
		PILOT = 1 << 4,
		WEAPONSMITH = 1 << 5,
	}

	public enum CharacterActionState {
		NONE,
		DEAD,
		UNCONCIOUS,
		SLEEPING,
		FIGHTING,
        ROTTING,
        HIDING,
        SNEAKING
	}

	public enum CharacterStanceState {
        NONE,
		SITTING,
		LAYING_UNCONCIOUS,
        LAYING_DEAD,
        PRONE,
		STANDING,
        DECOMPOSING
	}

	public enum Genders {
		MALE, 
		FEMALE
	}

	public enum SkinType { FLESH, FUR, LEATHER, SCALY, FEATHERS }
	public enum HairColors { WHITE, RED, BLACK, BROWN, BLONDE, BLUE, PURPLE, SILVER, GREY }
	public enum EyeColors { WHITE, RED, BLACK, BROWN, YELLOW, BLUE, PURPLE, HAZEL, GREY }
	public enum SkinColors { TAN, PALE, FLUSHED, BLACK, FAIR, OLIVE, BROWN, GREY }
    public enum BodyBuild { ANOREXIC, SKINNY, MEDIUM, ATHLETIC, HEAVY, OVERWEIGHT, OBESE }
	
	public enum Languages { COMMON, DRAKISH, PALVIAN } //these are temporary just for testing
}

