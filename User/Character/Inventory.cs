using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoUtils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB;
using Interfaces;

//this used to be the Inventory/Equipment class and it held both the inventory and equipment containers and all the methods for each.
//I decided that breaking them both up was a better idea and would make the code easier to maintain and debug.

namespace Character {
    public class Inventory : IInventory{
        public string playerID { get; set; }

        public HashSet<IItem> inventory { get; set; }

        public Inventory() {
            inventory = new HashSet<IItem>();
        }

        public IItem RemoveInventoryItem(IItem item, IEquipment equipment) {
            IItem result = null;

            result = inventory.Where(i => i.Id == item.Id).SingleOrDefault();
            inventory.RemoveWhere(i => i.Id == item.Id);

            if (result == null) { //so if it wasn't in the inventory we need to check what is equipped
                foreach (KeyValuePair<Wearable, IItem> slot in equipment.equipped) {
                    if (slot.Value == item) {
                        result = (IItem)slot.Value;
                        equipment.equipped.Remove(slot.Key);
                        break;
                    }
                }
            }
            return result;
        }

		public void AddItemToInventory(IItem item) {
            item.Owner = playerID;
            item.Save();
            inventory.Add(item);
        }

        public void UpdateInventoryFromDatabase() {
            MongoCollection col = MongoUtils.MongoData.GetCollection("World", "Items");
            var docs = col.FindAs<BsonDocument>(Query.EQ("Owner", playerID));
            foreach (BsonDocument dbItem in docs) {
                ObjectId itemID = dbItem["_id"].AsObjectId;
                IItem temp = inventory.Where(i => i.Id == itemID).SingleOrDefault();
                if (temp == null) {
                    inventory.Add(Items.Items.GetByID(dbItem["_id"].AsObjectId.ToString()));
                }
            }
        }

        public List<IItem> GetInventoryAsItemList() {
            UpdateInventoryFromDatabase();
            return inventory.ToList();
        }

        public List<string> GetInventoryList() {
            UpdateInventoryFromDatabase();
            List<string> result = new List<string>();
            Dictionary<string, int> itemGroups = new Dictionary<string, int>();

            foreach (IItem item in GetInventoryAsItemList()) {
                if (item != null) {
                    IContainer containerItem = item as IContainer;
                    if (containerItem != null) {
                        if (!itemGroups.ContainsKey(item.Name + "$" + (containerItem.Opened ? "[Opened]" : "[Closed]"))) {
                            itemGroups.Add(item.Name + "$" + (containerItem.Opened ? "[Opened]" : "[Closed]"), 1);
                        }
                        else {
                            itemGroups[item.Name + "$" + (containerItem.Opened ? "[Opened]" : "[Closed]")] += 1;
                        }
                    }
                    else {
                        if (!itemGroups.ContainsKey(item.Name + "$" + item.CurrentCondition)) {
                            itemGroups.Add(item.Name + "$" + item.CurrentCondition, 1);
                        }
                        else {
                            itemGroups[item.Name + "$" + item.CurrentCondition] += 1;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, int> pair in itemGroups) {
                string[] temp = pair.Key.Split('$');
                if (!string.Equals(temp[1], "NONE", StringComparison.InvariantCultureIgnoreCase)) {
                    if (temp[1].Contains("[Opened]") || temp[1].Contains("[Closed]")) {
                        result.Add(temp[0] + " " + temp[1] + (pair.Value > 1 ? (" [x" + pair.Value + "]") : ""));
                    }
                    else {
                        result.Add(temp[0] + " (" + temp[1].Replace("_", " ").ToLower() + " condition)" + (pair.Value > 1 ? ("[x" + pair.Value + "]") : ""));
                    }
                }
            }

            return result;
        }

        public List<IItem> GetAllItemsToWear() {
            List<IItem> result = new List<IItem>();
            List<List<IItem>> inventorySet = new List<List<IItem>>();

            var inventoryItems = GetInventoryAsItemList();
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.HEAD).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.CHEST).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.FEET).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.HANDS).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.NECK).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.SHOULDERS).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.WAIST).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.WIELD_LEFT).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.WIELD_RIGHT).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.LEFT_EAR).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.RIGHT_EAR).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Wearable.BACK).ToList());

            //yay we have our long list of inventory items now to go through and compare them individually to find the ones with the best stats
            foreach (List<IItem> set in inventorySet) {
                result.Add(Items.Items.GetBestItem(set));
            }

            return result;
        }
    }
}
