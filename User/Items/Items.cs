using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Triggers;

//Items now perform an event any time they do an action, triggers can now use the event that gets raised to do whatever they may want to do by subscribing

//TODO: Booby traps that can be placed on things, like doors, exits, items, etc.  It should just be an item that has a booby trap type of trigger that can be
//deactivated, removed, reset, etc.  Should be fun to make, not everything should have a slot for booby trap placements though.

//Item triggers can give the players a tip as to where they can go to start a quest.  This could also mean they can tell the player where they can be given to to complete/start a quest.

namespace Items {
    public sealed partial class Items : Iitem, Iweapon, Iedible, Icontainer, Iiluminate, Iclothing, Ikey {
        
        #region Properties
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public ItemCondition CurrentCondition { get; set; }
        public ItemCondition MaxCondition { get; set; }
        public bool IsWearable { get; set; }
        public int MinimumLevel { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ItemsType, int> ItemType { get; set; }
        public Wearable WornOn { get; set; }
        public string Location { get; set; }
        public bool IsMovable { get; set; }
        public BsonArray Triggers { get; set; }
        public string Owner { get; set; }

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


        private List<ITrigger> _itemTriggers;

        public List<ITrigger> ItemTriggers {
            get {
                return _itemTriggers;
            }
            set {
                _itemTriggers = value;
            }
        }
        

        //public List<ITrigger> SpeechTriggers {
        //    get {
        //        return _speechTriggers;
        //    }
        //    set {
        //        _speechTriggers = value;
        //    }
        //}
        //private List<ITrigger> _speechTriggers;
        #endregion Properties

        #region Public Static Methods
        public static string ParseItemName(List<string> commands) {
            commands.RemoveAt(0);
            commands.RemoveAt(0);

            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                commands.RemoveAt(commands.Count - 1);
            }

            StringBuilder itemName = new StringBuilder();
            foreach (string word in commands) {
                if (string.Equals(word, "in", StringComparison.InvariantCultureIgnoreCase) || string.Equals(word, "from", StringComparison.InvariantCultureIgnoreCase)) {
                    break; // we've reached the end of the item name if we encounter either of these
                }
                itemName.Append(word + " ");
            }
            return itemName.ToString().Trim().CamelCaseString();
        }

        public static List<Iitem> GetByName(string name, string owner) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("Items");
            name = name.CamelCaseString();
            //find any items in the location that match what the player typed
            var doc = collection.FindAs<BsonDocument>(Query.And(Query.Matches("Name", name), Query.EQ("Owner", owner)));
            
            List<Iitem> result = new List<Iitem>();

            if (doc != null) {
                foreach (BsonDocument itemDoc in doc) {
                    result.Add(ItemFactory.CreateItem(itemDoc["_id"].AsObjectId));
                }             
            }
            return result;
        }

        public static void DeChargeLightSources() {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("Items");
            var doc = collection.FindAs<BsonDocument>(Query.And(Query.EQ("ItemType", 5), Query.EQ("isLit", true)));

            foreach (BsonDocument item in doc) {
                Iitem lightSource = GetByID(item["_id"].AsObjectId.ToString());
                Iiluminate light = lightSource as Iiluminate;
                light.Drain();
            }
        }

        public static Iitem GetByID(string id) {
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection collection = db.GetCollection("Items");
            var doc = collection.FindOneAs<BsonDocument>(Query.EQ("_id", ObjectId.Parse(id)));
            Iitem result = null;
            if (doc != null) {
                result = ItemFactory.CreateItem(doc["_id"].AsObjectId);
            }
            return result;
        }

        public static Iitem GetByIDFromList(List<string> id) {
            if (id.Count > 0) {
                Iitem result = GetByID(id[0]);
                return result;
            }

            return null;
        }
        #endregion Public Static Methods

        #region Public Methods

    

        public void DeteriorateCondition() {
            //TODO: condition should affect item stats as well
            int newCondition = ((int)CurrentCondition) - 1;
            if (newCondition >= 1) { //otherwise it can't deteriorate any more and it's broken anyways
                CurrentCondition = (ItemCondition)newCondition;

            }
            Save();
            OnDeteriorated(new ItemEventArgs(ItemEvent.DETERIORATE, this.Id));
        }

        public void ImproveCondition() {
            //TODO: condition should affect item stats as well
            int newCondition = ((int)CurrentCondition) + 1;
            if (newCondition <= (int)ItemCondition.EXCELLENT) { //can't go higher than Excellent
                CurrentCondition = (ItemCondition)newCondition;
            }
            Save();
            OnImproved(new ItemEventArgs(ItemEvent.IMPROVE, this.Id));
        }

        public void Save() {
           MongoCollection collection = MongoUtils.MongoData.GetCollection("World", "Items");
           this.ItemTriggers = null; //we don't want to save this when we deserialize and all triggers are saved in Triggers as BsonDocuments and can't be edited within the engine
           collection.Save<Items>(this);

        }
        #endregion Public Methods

        #region Events
        public void OnDeteriorated(ItemEventArgs e) {
            if (Deteriorated != null) {
                Deteriorated(this, e);
            }
        }

        public void OnImproved(ItemEventArgs e) {
            if (Improved != null) {
                Improved(this, e);
            }
        }

        public void OnOpened(ItemEventArgs e) {
            if (ContainerOpened != null) {
                ContainerOpened(this, e);
            }
        }

        public void OnClosed(ItemEventArgs e) {
            if (ContainerClosed != null) {
                ContainerClosed(this, e);
            }
        }

        public void OnExamined(ItemEventArgs e) {
            if (Examined != null) {
                Examined(this, e);
            }
        }

        public void OnWorn(ItemEventArgs e) {
            if (ItemWorn != null) {
                ItemWorn(this, e);
            }
        }

        public void OnLookedIn(ItemEventArgs e) {
            if (LookedIn != null) {
                LookedIn(this, e);
            }
        }

        public void OnStored(ItemEventArgs e) {
            if (Stored != null) {
                Stored(this, e);
            }
        }

        public void OnRetrieved(ItemEventArgs e) {
            if (Retrieved != null) {
                Retrieved(this, e);
            }
        }

        public void OnConsumed(ItemEventArgs e) {
            if (Consumed != null) {
                Consumed(this, e);
            }
        }

        public void OnIgnited(ItemEventArgs e) {
            if (Ignited != null) {
                Ignited(this, e);
            }
        }

        public void OnExtinguished(ItemEventArgs e) {
            if (Extinguished != null) {
                Extinguished(this, e);
            }
        }

        public void OnDrained(ItemEventArgs e) {
            if (Drained != null) {
                Drained(this, e);
            }
        }

        public void OnRecharged(ItemEventArgs e) {
            if (Recharged != null) {
                Recharged(this, e);
            }
        }

        public void OnWielded(ItemEventArgs e) {
            if (Wielded != null) {
                Wielded(this, e);
            }
        }
        #endregion Events

        #region Constructor
        public Items() { }
        
        #endregion Constructor

        public string Examine() {
            OnExamined(new ItemEventArgs(ItemEvent.EXAMINE, this.Id));
            return Description;
        }

        public static Iitem GetBestItem(List<Iitem> set) {
            Iitem bestItem = null;
            if (set.Count() > 0) {
                bestItem = set[0];
                foreach (Iitem comparee in set) {
                    if (bestItem == comparee) {
                        continue;
                    }
                    bestItem = CompareItems(bestItem, comparee);
                }
            }
            return bestItem;
        }

        //we are going to base the best item on an accumulation of points based on every stat difference for the item
        private static Iitem CompareItems(Iitem bestItem, Iitem comparee) {
            List<int> scores = new List<int>();
            List<Iitem> bothItems = new List<Iitem> { bestItem, comparee };
            
            int index = 0;
            foreach (Iitem item in bothItems) {
                Iclothing clothing = item as Iclothing;
                Iweapon weapon = item as Iweapon;
                Icontainer container = item as Icontainer;

                if (clothing != null) {
                    scores[index] += (int)clothing.CurrentDefense;
                }
                
                if (weapon != null) {
                    scores[index] += (int)(weapon.CurrentMaxDamage - weapon.CurrentMinDamage);
                    scores[index] += (int)weapon.AttackSpeed;
                    //TODO: will need to figure out wear effects and player/target attack effects
                }

                if (container != null) {
                    scores[index] += (int)container.WeightLimit / 100;
                    scores[index] += (int)container.ReduceCarryWeightBy * 100;
                }

                index++;
            }

            if (scores[1] > scores[0]) {
                bestItem = bothItems[1];
            }

            return bestItem;
        }

    }

    public class ItemEventArgs : EventArgs {
        public ItemEvent ItemEvent { get; private set; }
        public ObjectId ItemID { get; private set; }
        
        public ItemEventArgs(ItemEvent itemEvent, ObjectId itemID) {
            ItemEvent = itemEvent;
            ItemID = itemID;
        }
    }

    #region Public Enumerations
    public enum Wearable { NONE, HEAD, LEFT_EAR, RIGHT_EAR, NECK, BACK, CHEST, SHOULDERS, WAIST, FEET, HANDS, WIELD, WIELD_RIGHT, WIELD_LEFT} //this is enough for now add more later
    public enum ItemCondition {NONE, DESTROYED_BEYOND_REPAIR, DESTROYED, DAMAGED, VERY_WORN, WORN, GOOD, VERY_GOOD, EXCELLENT } //a few item conditions
    public enum ItemsType { WEAPON, CLOTHING, EDIBLE, DRINKABLE, CONTAINER, ILUMINATION, KEY } //a couple of item types
    public enum EdibleType { FOOD, BEVERAGE }
    public enum ItemEvent { OPEN, CLOSE, WEAR, LOOK_IN, STORE, RETRIEVE, DRAIN, RECHARGE, IGNITE, EXTINGUISH, EXAMINE, DETERIORATE, IMPROVE, CONSUME, WIELD }
    #endregion Public Enumerations
}
