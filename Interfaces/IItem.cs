using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Interfaces {
    public enum Wearable { NONE, HEAD, LEFT_EAR, RIGHT_EAR, NECK, BACK, CHEST, SHOULDERS, WAIST, FEET, HANDS, WIELD, WIELD_RIGHT, WIELD_LEFT } //this is enough for now add more later
    public enum ItemCondition { NONE, DESTROYED_BEYOND_REPAIR, DESTROYED, DAMAGED, VERY_WORN, WORN, GOOD, VERY_GOOD, EXCELLENT } //a few item conditions
    public enum ItemsType { WEAPON, CLOTHING, EDIBLE, DRINKABLE, CONTAINER, ILUMINATION, KEY } //a couple of item types
    public enum EdibleType { FOOD, BEVERAGE }
    public enum ItemEvent { OPEN, CLOSE, WEAR, LOOK_IN, STORE, RETRIEVE, DRAIN, RECHARGE, IGNITE, EXTINGUISH, EXAMINE, DETERIORATE, IMPROVE, CONSUME, WIELD }

    public class ItemEventArgs : EventArgs
    {
        public ItemEvent ItemEvent { get; private set; }
        public ObjectId ItemID { get; private set; }

        public ItemEventArgs(ItemEvent itemEvent, ObjectId itemID)
        {
            ItemEvent = itemEvent;
            ItemID = itemID;
        }
    }

    public interface IItem {
        List<ITrigger> ItemTriggers { get; set; }
      //  List<ITrigger> SpeechTriggers { get; set; }
        ObjectId Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        double Weight { get; set; }
        ItemCondition CurrentCondition { get; set; }
        bool IsWearable { get; set; } 
        int MinimumLevel { get; set; }
        Wearable WornOn { get; set; }
        string Location { get; set; }
        Dictionary<ItemsType, int> ItemType { get; set; }
        void DeteriorateCondition();
        void ImproveCondition();
        void Save();
        string Examine();
        bool IsMovable { get; set; }
        string Owner { get; set; } //set to the ID of the player/NPC or room currently holding the object, otherwise null

        event EventHandler<ItemEventArgs> Deteriorated;
        event EventHandler<ItemEventArgs> Improved;
        event EventHandler<ItemEventArgs> ContainerOpened;
        event EventHandler<ItemEventArgs> ContainerClosed;
        event EventHandler<ItemEventArgs> Examined;
        event EventHandler<ItemEventArgs> ItemWorn;
        event EventHandler<ItemEventArgs> LookedIn;
        event EventHandler<ItemEventArgs> Stored;
        event EventHandler<ItemEventArgs> Retrieved;
        event EventHandler<ItemEventArgs> Consumed;
        event EventHandler<ItemEventArgs> Ignited;
        event EventHandler<ItemEventArgs> Extinguished;
        event EventHandler<ItemEventArgs> Drained;
        event EventHandler<ItemEventArgs> Recharged;
        event EventHandler<ItemEventArgs> Wielded;
        void OnDeteriorated(ItemEventArgs e);
        void OnImproved(ItemEventArgs e);
        void OnOpened(ItemEventArgs e);
        void OnClosed(ItemEventArgs e);
        void OnExamined(ItemEventArgs e);
        void OnWorn(ItemEventArgs e);
        void OnLookedIn(ItemEventArgs e);
        void OnStored(ItemEventArgs e);
        void OnRetrieved(ItemEventArgs e);
        void OnConsumed(ItemEventArgs e);
        void OnIgnited(ItemEventArgs e);
        void OnExtinguished(ItemEventArgs e);
        void OnDrained(ItemEventArgs e);
        void OnRecharged(ItemEventArgs e);
        void OnWielded(ItemEventArgs e);
   }

    public class Item : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey
    {
        public List<ITrigger> ItemTriggers { get; set; }
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public ItemCondition CurrentCondition { get; set; }
        public bool IsWearable { get; set; }
        public int MinimumLevel { get; set; }
        public Wearable WornOn { get; set; }
        public string Location { get; set; }
        public Dictionary<ItemsType, int> ItemType { get; set; }
        public void DeteriorateCondition() { }
        public void ImproveCondition() { }
        public void Save() { }
        public string Examine() { return String.Empty; }
        public bool IsMovable { get; set; }
        public string Owner { get; set; } //set to the ID of the player/NPC or room currently holding the object, otherwise null
        public void GetAttributesAffected(BsonArray attributesToAffect) { }
        public Dictionary<string, double> Consume() { return new Dictionary<string, double>(); }

        public double MinDamage { get; set; }
        public double MaxDamage { get; set; }
        public double AttackSpeed { get; set; }
        public bool IsWieldable { get; set; }
        public double CurrentMinDamage { get; set; }
        public double CurrentMaxDamage { get; set; }

        public List<string> Contents { get; set; }
        public List<string> GetContents() { return new List<string>(); }
        public IItem RetrieveItem(string id) { return new Item(); }
        public bool StoreItem(string id) { return true; }
        public double WeightLimit { get; set; }
        public double CurrentWeight { get; set; }
        public double ReduceCarryWeightBy { get; set; }
        public bool IsOpenable { get; set; }
        public bool Opened { get; set; }
        public string Open() { return string.Empty; }
        public string Close() { return string.Empty; }
        public void Wear() { }
        public string LookIn() { return string.Empty; }
        public bool isLit { get; set; }
        public bool isLightable { get; set; }
        public bool isChargeable { get; set; }
        public FuelSource fuelSource { get; set; }
        public LightType lightType { get; set; }
        public double maxCharge { get; set; }
        public double currentCharge { get; set; }
        public double chargeLowWarning { get; set; }
        public double chargeDecayRate { get; set; } //per sec
        public void Drain() { }
        public IMessage Ignite() { return new Message(); } 
        public IMessage Extinguish() { return new Message(); }
        public void ReCharge(double chargeAmount) { }
        public string ExamineCharge() { return string.Empty; }
        public Wearable EquippedOn { get; set; }
        public decimal MaxDefense { get; set; }
        public decimal CurrentDefense { get; set; }
        public Dictionary<string, double> TargetDefenseEffects { get; set; }
        public Dictionary<string, double> PlayerDefenseEffects { get; set; }
        public Dictionary<string, double> WearEffects { get; set; }
        public string DoorID { get; set; }
        public bool SkeletonKey { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> WieldEffects { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> TargetAttackEffects { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, double> PlayerAttackEffects { get; set; }

        public Dictionary<String, double> Wield() { return new Dictionary<string, double>(); }

        public event EventHandler<ItemEventArgs> Deteriorated;
        public event EventHandler<ItemEventArgs> Improved;
        public event EventHandler<ItemEventArgs> ContainerOpened;
        public event EventHandler<ItemEventArgs> ContainerClosed;
        public event EventHandler<ItemEventArgs> Examined;
        public event EventHandler<ItemEventArgs> ItemWorn;
        public event EventHandler<ItemEventArgs> LookedIn;
        public event EventHandler<ItemEventArgs> Stored;
        public event EventHandler<ItemEventArgs> Retrieved;
        public event EventHandler<ItemEventArgs> Consumed;
        public event EventHandler<ItemEventArgs> Ignited;
        public event EventHandler<ItemEventArgs> Extinguished;
        public event EventHandler<ItemEventArgs> Drained;
        public event EventHandler<ItemEventArgs> Recharged;
        public event EventHandler<ItemEventArgs> Wielded;
        public void OnDeteriorated(ItemEventArgs e) { }
        public void OnImproved(ItemEventArgs e) { }
        public void OnOpened(ItemEventArgs e) { }
        public void OnClosed(ItemEventArgs e) { }
        public void OnExamined(ItemEventArgs e) { }
        public void OnWorn(ItemEventArgs e) { }
        public void OnLookedIn(ItemEventArgs e) { }
        public void OnStored(ItemEventArgs e) { }
        public void OnRetrieved(ItemEventArgs e) { }
        public void OnConsumed(ItemEventArgs e) { }
        public void OnIgnited(ItemEventArgs e) { }
        public void OnExtinguished(ItemEventArgs e) { }
        public void OnDrained(ItemEventArgs e) { }
        public void OnRecharged(ItemEventArgs e) { }
        public void OnWielded(ItemEventArgs e) { }
    }
}
