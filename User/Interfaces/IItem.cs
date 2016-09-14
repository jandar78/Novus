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

    public class ItemEventArgs : EventArgs {
        public ItemEvent ItemEvent { get; private set; }
        public ObjectId ItemID { get; private set; }

        public ItemEventArgs(ItemEvent itemEvent, ObjectId itemID) {
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
        ItemCondition MaxCondition { get; set; }
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
        ObjectId Owner { get; set; } //set to the ID of the player/NPC or room currently holding the object, otherwise null

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
}

