using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharacterEnums {

	public enum CharacterType { PLAYER, NPC }
	public enum CharacterRace { Human, Orc, Dwarf, Elf }

	//uses bitwise operator to make it more readable, output is the same as setting them to 0x1,0x2,0x4,0x8,0x10, etc.
	//doing it this way should allow to players to be several different types of characters at once (Do I want this?)
	[Flags]
	public enum CharacterClass {
        Fighter = 1 << 1,
		Explorer = 1 << 2,
		Engineer = 1 << 3,
		Pilot = 1 << 4,
		Weaponsmith = 1 << 5
	}

	public enum CharacterActionState {None, Dead, Unconcious, Sleeping,	Fighting, Rotting, Hiding, Sneaking	}

    //TODO: Add combat stances, there is a Github issue for this for more detail.
	public enum CharacterStanceState {None, Sitting, Laying_unconcious, Laying_dead, Prone, Standing, Decomposing }

    public enum CombatStances { Neutral, Offensive, Defensive, Disrupted }

	public enum Genders {Male, Female}

	public enum SkinType { Flesh, Fur, Leather, Scaly, Feathers }
	public enum HairColors { White, Red, Black, Brown, Blonde, Blue, Purple, Silver, Grey }
	public enum EyeColors { White, Red, Black, Brown, Yellow, Blue, Purple, Hazel, Grey }
	public enum SkinColors { Tan, Pale, Flushed, Black, Fair, Olive, Brown, Grey }
    public enum BodyBuild { Anorexic, Skinny, Medium, Athletic, Heavy, Overweight, Obese }
	
	public enum Languages { Common, Drakish, Palvian } //these are temporary just for testing

    public enum BonusTypes { Dodge, Attack, CritDamage, CritChance, HitChance, Defense, LightArmor, MediumArmor, HeavyArmor, Weapon, Dexterity, 
        Strength, Endurance, Intelligence, Hitpoint, Charisma, Agility, Cunning, Leadership, Wisdom, Toughness };
}

