using System;
using System.Collections.Generic;
using Character;
using MongoDB.Bson;


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
        Inventory Inventory { get; }
        Equipment Equipment { get; }
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
        void Hydrate(BsonDocument document);
        void Hydrate(ObjectId id);
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
}


