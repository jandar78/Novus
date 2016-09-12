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
using Interfaces;

//Items now perform an event any time they do an action, triggers can now use the event that gets raised to do whatever they may want to do by subscribing

//TODO: Booby traps that can be placed on things, like doors, exits, items, etc.  It should just be an item that has a booby trap type of trigger that can be
//deactivated, removed, reset, etc.  Should be fun to make, not everything should have a slot for booby trap placements though.

//Item triggers can give the players a tip as to where they can go to start a quest.  This could also mean they can tell the player where they can be given to to complete/start a quest.

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {
        
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
        public BsonArray Trigger { get; set; }
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

        public static List<IItem> GetByName(string name, string owner) {
            var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("World","Items");
            name = name.CamelCaseString();
            //find any items in the location that match what the player typed
            var items = MongoUtils.MongoData.RetrieveObjectsAsync<BsonDocument>(collection, i => i["Name"] == name && i["Owner"] == owner).Result;
            List<IItem> itemList = new List<IItem>();
            foreach (var item in items) {
                itemList.Add(ItemFactory.CreateItem(item["_id"].AsObjectId).Result);
            }

            return itemList;
        }

        public async static void DeChargeLightSources() {
            var collection = MongoUtils.MongoData.GetCollection<BsonDocument>("World", "Items");
            var doc = await MongoUtils.MongoData.RetrieveObjectsAsync<BsonDocument>(collection, i => i["ItemType"] == 5 && i["isLit"] == true);

            foreach (var item in doc) {
                IItem lightSource = await GetByID(item["_id"].AsObjectId.ToString());
                IIluminate light = lightSource as IIluminate;
                light.Drain();
            }
        }

        public async static Task<IItem> GetByID(string id) {
            var collection = MongoUtils.MongoData.GetCollection<Items>("World", "Items");
            var item = await MongoUtils.MongoData.RetrieveObjectAsync<Items>(collection, i => i.Id == ObjectId.Parse(id));
            IItem result = null;
            if (item != null) {
                result = await ItemFactory.CreateItem(item.Id);
            }

            return result;
        }

        public async static Task<IItem> GetByIDFromList(List<string> id) {
            if (id.Count > 0) {
                IItem result = await GetByID(id[0]);
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

        public async void Save() {
           var collection = MongoUtils.MongoData.GetCollection<Items>("World", "Items");
           this.ItemTriggers = null; //we don't want to save this when we deserialize and all triggers are saved in Triggers as BsonDocuments and can't be edited within the engine
           await MongoUtils.MongoData.SaveAsync<Items>(collection, i => i.Id == this.Id, this);
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

        public static IItem GetBestItem(List<IItem> set) {
            IItem bestItem = null;
            if (set.Count() > 0) {
                bestItem = set[0];
                foreach (IItem comparee in set) {
                    if (bestItem == comparee) {
                        continue;
                    }
                    bestItem = CompareItems(bestItem, comparee);
                }
            }
            return bestItem;
        }

        //we are going to base the best item on an accumulation of points based on every stat difference for the item
        private static IItem CompareItems(IItem bestItem, IItem comparee) {
            List<int> scores = new List<int>();
            List<IItem> bothItems = new List<IItem> { bestItem, comparee };
            
            int index = 0;
            foreach (IItem item in bothItems) {
                IClothing clothing = item as IClothing;
                IWeapon weapon = item as IWeapon;
                IContainer container = item as IContainer;

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
}
