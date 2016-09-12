using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {
    public interface INpc {
        void Update();
        void CalculateXP();
        void IncreaseXPReward(string id, double damage);
		void DecreaseXPReward(double amount);
        void ParseMessage(IMessage message);
        IFsm Fsm { get; set; }
        DateTime NextAiAction { get; set; }
        string AiState { get; set; }
        string PreviousState { get; set; }
        string GlobalState { get; set; }
        long XP { get; set; }

    }

    //public class NPC : IActor, INpc
    //{
    //    #region private things
    //    private Dictionary<string, double> damageTracker;
    //    private IInventory _inventory;
    //    private IEquipment _equipment;
    //    private List<IQuest> _quests;
    //    #endregion private things

    //    #region Public Members
    //    public IInventory Inventory { get; set; }
        
    //    public IEquipment Equipment { get; set; }
        
    //    public List<IQuest> Quests { get; set; }
        
    //    public Queue<string> Messages;
    //    public List<ITrigger> Triggers;
    //    #endregion Public Members

    //    #region Protected Members
    //    protected Dictionary<string, IAttributes> Attributes;
    //    protected Dictionary<string, double> SubAttributes;
    //    protected HashSet<Languages> KnownLanguages; //this will hold all the languages the player can understand
    //    protected double _levelModifier;
    //    private IStatBonuses Bonuses;

    //    #region Stances
    //    protected CharacterStanceState _stanceState;
    //    protected CharacterActionState _actionState;
    //    #endregion Stances

    //    #region Misc
    //    protected int _level;
    //    protected CharacterClass _class;
    //    protected Languages _primaryLanguage;
    //    protected Tuple<int, DateTime> _koCount; //this will only ever be zero on initialize until first knockout
    //    protected int _points;
    //    #endregion Misc

    //    #region Bodily descriptions
    //    protected Genders _gender;
    //    protected EyeColors _eyeColor;
    //    protected HairColors _hairColor;
    //    protected SkinColors _skinColor;
    //    protected SkinType _skinType;
    //    protected BodyBuild _build;
    //    protected CharacterRace _race;
    //    #endregion Bodily descriptions
    //    #endregion Protected Members

    //    #region Private members
    //    private int points = 0;
    //    #endregion Private members

    //    #region  Properties
    //    public string Password { get; set; }
    //    public long Experience { get; set; }
    //    public long NextLevelExperience { get; set; }
    //    public bool Leveled { get; set; }
    //    public bool IsLevelUp { get; set; }
    //    public string ID { get; set; }
    //    public string Location { get; set; }
    //    public string LastLocation { get; set; }
    //    public bool IsNPC { get; set; }
    //    public int MobTypeID { get; set; }
    //    public IFsm Fsm { get; set; }
    //    public string Title { get; set; }
    //    public string FullName { get; set; }
    //    public string FullHonors { get; set; }
    //    public DateTime NextAiAction { get; set; }
    //    public void Update() { }
    //    private bool IsMob { get; set; }
    //    public long XP { get; set; }
    //    #endregion Properties

    //    #region Descriptive
    //    public string Gender { get; set; }
    //    public string MainHand { get; set; }
    //    public string GenderPossesive { get; set; }
    //    public string Build { get; set; }
    //    public int Age { get; set; }
    //    public double Weight { get; set; }
    //    public double Height { get; set; }
    //    public string FirstName { get; set; }
    //    public string LastName { get; set; }
    //    public string Class { get; set; }
    //    public string Race { get; set; }
    //    public string Description { get; set; }
    //    public string EyeColor { get; set; }
    //    public string SkinColor { get; set; }
    //    public string SkinType { get; set; }
    //    public string HairColor { get; set; }
    //    #endregion Descriptive

    //    #region Stances
    //    public string Action { get; set; }
    //    public string Stance { get; set; }
    //    public CharacterStanceState StanceState { get; set; }
    //    public CharacterActionState ActionState { get; set; }
    //    #endregion Stances

    //    #region Leveling
    //    public int PointsToSpend { get; set; }
    //    public int Level { get; set; }
    //    public double LevelModifier { get; set; }
        
    //    #endregion Leveling

    //    #region Combat
    //    public string KillerID { get; set; }
    //    public DateTime TimeOfDeath { get; set; }
    //    public bool CheckUnconscious { get; }
    //    public bool CheckDead { get; }
    //    public double DeathLimit { get; }
    //    public string CurrentTarget { get; set; }
    //    public string LastTarget { get; set; }
    //    public bool InCombat { get; set; }
    //    public DateTime LastCombatTime { get; set; }
    //    #endregion Combat

    //    public NPC() { }

    //    public NPC(CharacterRace race, CharacterClass characterClass, Genders gender, Languages language, SkinColors skinColor, SkinType skinType, HairColors hairColor, EyeColors eyeColor, BodyBuild build)
    //    { }


    //    public void Save() { }

    //    public void Load(string id) { }
        
    //    public void CalculateXP() { }

    //    public void IncreaseXPReward(string id, double damage) { }

    //    public void DecreaseXPReward(double amount) { }

    //    public void ParseMessage(IMessage message) { }

    //    public void SetActionState(CharacterActionState state) { }

    //    public void SetStanceState(CharacterStanceState state) { }

    //    public void SetActionStateDouble(double state) { }

    //    public void SetStanceStateDouble(double state) { }

    //    public void IncreasePoints() { }

    //    public bool IsUnconcious() { return false; }

    //    public bool IsDead() { return false; }

    //    public void ClearTarget() { }

    //    public void UpdateTarget(string targetID) { }

    //    public void ApplyRegen(string attribute) { }

    //    public void ApplyEffectOnAttribute(string name, double value) { }

    //    public double GetAttributeMax(string attribute) { return 0.0d; }

    //    public double GetAttributeValue(string attribute) { return 0.0d; }

    //    public int GetAttributeRank(string attribute) { return 0; }

    //    public string AiState { get; set; }

    //    public string PreviousState { get; set; }

    //    public string GlobalState { get; set; }

    //    public void SetAttributeValue(string name, double value) { }

    //    public void SetMaxAttributeValue(string name, double value) { }

    //    public void SeAttributeRegenRate(string name, double value) { }
        
    //    public Dictionary<string, IAttributes> GetAttributes() { return new Dictionary<string, IAttributes>(); }
        
    //    public Dictionary<string, double> GetSubAttributes() { return new Dictionary<string, double>(); }

    //    public void CalculateSubAttributes() { }

    //    public void AddLanguage(Languages language) { }

    //    public bool KnowsLanguage(Languages language) { return true; }

    //    public bool Loot(IUser looter, List<string> commands, bool byPassCheck = false) { return false; }
        
    //    public bool CanLoot(string looterID) { return false; }

    //    public void RewardXP(string id, long amount) { }

    //    public void AddBonus(BonusTypes type, string name, double amount, int time = 0) { }

    //    public void RemoveBonus(BonusTypes type, string name, double bonus) { }

    //    public void CleanupBonuses() { }
        
    //    public double GetBonus(BonusTypes type) { return 0.0d; }
    //}
}
