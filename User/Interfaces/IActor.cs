using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Interfaces
{
    public enum CharacterType { PLAYER, NPC }
    public enum CharacterRace { Human, Orc, Dwarf, Elf }

    //uses bitwise operator to make it more readable, output is the same as setting them to 0x1,0x2,0x4,0x8,0x10, etc.
    //doing it this way should allow to players to be several different types of characters at once (Do I want this?)
    [Flags]
    public enum CharacterClass
    {
        Fighter = 1 << 1,
        Explorer = 1 << 2,
        Engineer = 1 << 3,
        Pilot = 1 << 4,
        Weaponsmith = 1 << 5
    }

    public enum CharacterActionState { None, Dead, Unconcious, Sleeping, Fighting, Rotting, Hiding, Sneaking }

    public enum CharacterStanceState { None, Sitting, Laying_unconcious, Laying_dead, Prone, Standing, Decomposing }

    public enum CombatStances { Neutral, Offensive, Defensive, Disrupted }

    public enum Genders { Male, Female, Apache_Helicopter, None }

    public enum SkinType { Flesh, Fur, Leather, Scaly, Feathers }
    public enum HairColors { White, Red, Black, Brown, Blonde, Blue, Purple, Silver, Grey }
    public enum EyeColors { White, Red, Black, Brown, Yellow, Blue, Purple, Hazel, Grey }
    public enum SkinColors { Tan, Pale, Flushed, Black, Fair, Olive, Brown, Grey }
    public enum BodyBuild { Anorexic, Skinny, Medium, Athletic, Heavy, Overweight, Obese }

    public enum Languages { Common, Drakish, Palvian } //these are temporary just for testing

    public enum BonusTypes
    {
        Dodge, Attack, CritDamage, CritChance, HitChance, Defense, LightArmor, MediumArmor, HeavyArmor, Weapon, Dexterity,
        Strength, Endurance, Intelligence, Hitpoint, Charisma, Agility, Cunning, Leadership, Wisdom, Toughness
    };

    public interface IActor
    {
        #region  Properties
        IInventory Inventory { get; set; }
        IEquipment Equipment { get; set; }
        #region General
        ObjectId UserID { get; set; }
        string Password { get; set; }
        long Experience { get; set; }
        long NextLevelExperience { get; set; }
        bool Leveled { get; set; }
        bool IsLevelUp { get; }
        ObjectId Id { get; set; }
        string Location { get; set; }
        string LastLocation { get; set; }
        bool IsNPC { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        string FullHonors { get; }
        string Title { get; set; }
        string Description { get; set; }
        Genders Gender { get; set; }
        string GenderPossesive { get; }
        BodyBuild Build { get; }
        int Age { get; set; }
        double Weight { get; set; }
        double Height { get; set; }
        CharacterClass Class { get; set; }
        CharacterRace Race { get; }
        EyeColors EyeColor { get; set; }
        SkinColors SkinColor { get; set; }
        SkinType SkinType { get; set; }
        HairColors HairColor { get; set; }
        #endregion General

        #region Stances
        string Action { get; }
        string Stance { get; }
        CharacterStanceState StanceState { get; set; }
        CharacterActionState ActionState { get; set; }
        #endregion Stances

        #region Leveling
        int PointsToSpend { get; set; }
        int Level { get; set; }
        double LevelModifier { get; set; }
        #endregion Leveling

        #region Combat
        bool CheckUnconscious { get; }
        bool CheckDead { get; }
        double DeathLimit { get; }
        ObjectId CurrentTarget { get; set; }
        ObjectId LastTarget { get; set; }
        bool InCombat { get; set; }
        DateTime LastCombatTime { get; set; }
        DateTime TimeOfDeath { get; set; }
        string MainHand { get; set; }
        ObjectId KillerID { get; set; }
        #endregion Combat

        #endregion Properties

        #region Public Methods
        void Save();
        void Load(ObjectId id);

        void SetActionState(CharacterActionState state);
        void SetStanceState(CharacterStanceState state);
        void SetActionStateDouble(double state);
        void SetStanceStateDouble(double state);

        void RewardXP(ObjectId id, long amount);
        void IncreasePoints();

        #region Combat Methods
        bool IsUnconcious();
        bool IsDead();
        void ClearTarget();
        void UpdateTarget(ObjectId targetID);

        double GetBonus(BonusTypes type);
        void AddBonus(BonusTypes type, string name, double amount, int time = 0);
        void RemoveBonus(BonusTypes type, string name, double bonus);
        void CleanupBonuses();
        //void Wield(Items.Iitem item);
        #endregion Combat Methods

        #region Attribute Wrappers
        void ApplyRegen(string attribute);
        void ApplyEffectOnAttribute(string name, double value);
        double GetAttributeMax(string attribute);
        double GetAttributeValue(string attribute);
        int GetAttributeRank(string attribute);
        void SetAttributeValue(string name, double value);
        void SetMaxAttributeValue(string name, double value);
        void SeAttributeRegenRate(string name, double value);

        List<Character.Attribute> GetAttributes();
        Dictionary<string, double> GetSubAttributes();

        void CalculateSubAttributes();
        #endregion Attributes Wrappers

        #region Language Stuff
        void AddLanguage(Languages language);
        bool KnowsLanguage(Languages language);
        #endregion Language Stuff

        #endregion Public Methods

        bool Loot(IUser looter, List<string> commands, bool bypassCheck = false);
        bool CanLoot(ObjectId looterId);
    }


    //just the empty object for mapping purposes
    //public class Character : IActor
    //{
    //    public string Action { get; }      

    //    public CharacterActionState ActionState { get; }
        
    //    public int Age { get; set; }
        
    //    public string Build { get; }
        
    //    public bool CheckDead { get; }
    //    public bool CheckUnconscious { get; }
        
    //    public string Class { get; }
        
    //    public string CurrentTarget { get; set; }
        
    //    public double DeathLimit { get; }
        
    //    public string Description { get; set; }
        
    //    public IEquipment Equipment { get; set; }
        
    //    public long Experience { get; set; }
        
    //    public string EyeColor { get; }
        
    //    public string FirstName { get; set; }
        
    //    public string FullHonors { get; }
        
    //    public string FullName { get; }
        
    //    public string Gender { get; }
        
    //    public string GenderPossesive { get; }
        
    //    public string HairColor { get; }
        
    //    public double Height { get; set; }
        
    //    public string ID { get; set; }
    //    public bool InCombat { get; set; }
    //    public IInventory Inventory { get; set; }
        
    //    public bool IsLevelUp { get; }
        
    //    public bool IsNPC { get; set; }
        
    //    public string KillerID { get; set; }
        
    //    public DateTime LastCombatTime { get; set; }
    //    public string LastLocation { get; set; }
        
    //    public string LastName { get; set; }
        
    //    public string LastTarget { get; set; }
    //    public int Level { get; set; }
        
    //    public bool Leveled { get; set; }
        
    //    public double LevelModifier { get; set; }
        
    //    public string Location { get; set; }
        
    //    public string MainHand { get; set; }
        
    //    public long NextLevelExperience { get; set; }
        
    //    public string UserID { get; set; }
    //    public string Password { get; set; }
    //    public int PointsToSpend { get; set; }
    //    public string Race { get; }
    //    public string SkinColor { get; }
    //    public string SkinType { get; }
    //    public string Stance { get; }
        
    //    public CharacterStanceState StanceState { get; set; }
    //    public DateTime TimeOfDeath
    //    {
    //        get; set;
    //    }

    //    public string Title
    //    {
    //        get; set;
    //    }

    //    public double Weight
    //    {
    //        get; set;
    //    }

    //    public void AddBonus(BonusTypes type, string name, double amount, int time = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void AddLanguage(Languages language)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void ApplyEffectOnAttribute(string name, double value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void ApplyRegen(string attribute)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void CalculateSubAttributes()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool CanLoot(string looterID)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void CleanupBonuses()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void ClearTarget()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public double GetAttributeMax(string attribute)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetAttributeRank(string attribute)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Dictionary<string, IAttributes> GetAttributes()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public double GetAttributeValue(string attribute)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public double GetBonus(BonusTypes type)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Dictionary<string, double> GetSubAttributes()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void IncreasePoints()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool IsDead()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool IsUnconcious()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool KnowsLanguage(Languages language)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Load(string id)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Loot(IActor looter, List<string> commands, bool bypassCheck = false)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void RemoveBonus(BonusTypes type, string name, double bonus)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void RewardXP(string id, long amount)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Save()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SeAttributeRegenRate(string name, double value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetActionState(CharacterActionState state)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetActionStateDouble(double state)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetAttributeValue(string name, double value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetMaxAttributeValue(string name, double value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetStanceState(CharacterStanceState state)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void SetStanceStateDouble(double state)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void UpdateTarget(string targetID)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}


