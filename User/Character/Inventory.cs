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

//this used to be the Inventory/Equipment class and it held both the inventory and equipment containers and all the methods for each.
//I decided that breaking them both up was a better idea and would make the code easier to maintain and debug.

namespace Character {
    public class Inventory {
        public string playerID { get; set; }

        public HashSet<Items.Iitem> inventory;

        public Inventory() {
            inventory = new HashSet<Items.Iitem>();
        }

        public Items.Iitem RemoveInventoryItem(Items.Iitem item, Equipment equipment) {
            Items.Iitem result = null;

            result = inventory.Where(i => i.Id == item.Id).SingleOrDefault();
            inventory.RemoveWhere(i => i.Id == item.Id);

            if (result == null) { //so if it wasn't in the inventory we need to check what is equipped
                foreach (KeyValuePair<Items.Wearable, Items.Iitem> slot in equipment.equipped) {
                    if (slot.Value == item) {
                        result = (Items.Iitem)slot.Value;
                        equipment.equipped.Remove(slot.Key);
                        break;
                    }
                }
            }
            return result;
        }

		public void AddItemToInventory(Items.Iitem item) {
            item.Owner = playerID;
            item.Save();
            inventory.Add(item);
        }

        public void UpdateInventoryFromDatabase() {
            MongoCollection col = MongoUtils.MongoData.GetCollection("World", "Items");
            var docs = col.FindAs<BsonDocument>(Query.EQ("Owner", playerID));
            foreach (BsonDocument dbItem in docs) {
                ObjectId itemID = dbItem["_id"].AsObjectId;
                Items.Iitem temp = inventory.Where(i => i.Id == itemID).SingleOrDefault();
                if (temp == null) {
                    inventory.Add(Items.Items.GetByID(dbItem["_id"].AsObjectId.ToString()));
                }
            }
        }

        public List<Items.Iitem> GetInventoryAsItemList() {
            UpdateInventoryFromDatabase();
            return inventory.ToList();
        }

        public List<string> GetInventoryList() {
            UpdateInventoryFromDatabase();
            List<string> result = new List<string>();
            Dictionary<string, int> itemGroups = new Dictionary<string, int>();

            foreach (Items.Iitem item in GetInventoryAsItemList()) {
                if (item != null) {
                    Items.Icontainer containerItem = item as Items.Icontainer;
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

        public List<Items.Iitem> GetAllItemsToWear() {
            List<Items.Iitem> result = new List<Items.Iitem>();
            List<List<Items.Iitem>> inventorySet = new List<List<Items.Iitem>>();

            var inventoryItems = GetInventoryAsItemList();
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.HEAD).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.CHEST).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.FEET).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.HANDS).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.NECK).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.SHOULDERS).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.WAIST).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.WIELD_LEFT).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.WIELD_RIGHT).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.LEFT_EAR).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.RIGHT_EAR).ToList());
            inventorySet.Add(inventoryItems.Where(i => i.WornOn == Items.Wearable.BACK).ToList());

            //yay we have our long list of inventory items now to go through and compare them individually to find the ones with the best stats
            foreach (List<Items.Iitem> set in inventorySet) {
                result.Add(Items.Items.GetBestItem(set));
            }

            return result;
        }
    }
}
