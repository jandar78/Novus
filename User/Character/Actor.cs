using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterEnums;
using CharacterFactory;
using Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Character {
    //TODO: Convert this to an interface rather than an abstract class, same with Character and Actor
    public interface Iactor {
        #region  Properties
        Inventory Inventory { get;}
        Equipment Equipment { get;}
        #region General
        string Password { get; set; }
        long Experience { get; set; }
        long NextLevelExperience { get; set; }
        bool Leveled { get; set; }
        bool IsLevelUp { get; }
        string ID { get; set; }
        int Location { get; set; }
        int LastLocation { get; set; }
        bool IsNPC { get; set; }     
        string FirstName { get; set; }
        string LastName { get; set; }
        string Description { get; set; }
        string Gender { get; }
        string GenderPossesive { get; }
        string Build { get; }
        int Age { get; set; }
        double Weight { get; set; }
        double Height{ get; set; }
        string Class { get; }
        string Race { get; }
        string EyeColor { get; }
        string SkinColor { get; }
        string SkinType { get; }
        string HairColor { get; }
        #endregion General

        #region Stances
        string Action { get; }
        string Stance { get; }
        CharacterStanceState StanceState { get; }
        CharacterActionState ActionState { get; }
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
        string CurrentTarget { get; set; }
        string LastTarget { get; set; }
        bool InCombat { get; set; }
        DateTime LastCombatTime { get; set; }
        string MainHand { get; set; }
        #endregion Combat

        #endregion Properties

        #region Public Methods
        void Save();
        void Load(string id);

        void SetActionState(CharacterActionState state);
        void SetStanceState(CharacterStanceState state);
        void SetActionStateDouble(double state);
        void SetStanceStateDouble(double state);

        void RewardXP(string id, long amount);
        void IncreasePoints();

        #region Combat Methods
        bool IsUnconcious();
        bool IsDead();
        void ClearTarget();
        void UpdateTarget(string targetID);
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

        Dictionary<string, Attribute> GetAttributes();
        Dictionary<string, double> GetSubAttributes();

        void CalculateSubAttributes();
        #endregion Attributes Wrappers

        #region Language Stuff
        void AddLanguage(CharacterEnums.Languages language);
        bool KnowsLanguage(CharacterEnums.Languages language);
        #endregion Language Stuff

        #region Inventory/Equipment Wrappers
        //void AddItemToInventory(Items.Iitem item);
        //void RemoveItemFromInventory(Items.Iitem item);

        //void EquipItem(Items.Iitem item);
        //void UnequipItem(Items.Iitem item);

        //List<Items.Iitem> GetInventoryAsItemList();
        //List<string> GetInventoryList();
        //Dictionary<Items.Wearable, Items.Iitem> GetEquipment();
        //List<Items.Iitem> GetWieldedWeapons();
        //List<Items.Iitem> GetAllItemsToWear();
        #endregion Inventory/Equipment Wrappers

        #endregion Public Methods

       // Items.Wearable GetMainHandWeapon();

        void Loot(User.User looter, List<string> commands);
    }
}

