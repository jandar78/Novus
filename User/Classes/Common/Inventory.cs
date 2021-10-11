using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Interfaces;

//this used to be the Inventory/Equipment class and it held both the inventory and equipment containers and all the methods for each.
//I decided that breaking them both up was a better idea and would make the code easier to maintain and debug.

namespace Character {
    public class Inventory : IInventory
    {
        public HashSet<IItem> inventory { get; set; }

        public List<ObjectId> InventoryIds { get; set; }
        public Inventory() {
            inventory = new HashSet<IItem>();

            if (InventoryIds != null)
            {
                foreach (var id in InventoryIds)
                {
                    var item = Items.ItemFactory.CreateItem(id).Result;
                    inventory.Add(item);
                }
            }
        }

        public IItem RemoveInventoryItem(IItem item, IActor player) {
            IItem result = null;

            result = inventory.Where(i => i.Id == item.Id).SingleOrDefault();
            inventory.RemoveWhere(i => i.Id == item.Id);
            InventoryIds.Remove(item.Id);

            
            //Note: If it's equipped it's got to be unequipped first, let's not auto unequip things
            //if (result == null) { //so if it wasn't in the inventory we need to check what is equipped
            //    foreach (KeyValuePair<Wearable, IItem> slot in player.Equipment.Equipped) {
            //        if (slot.Value == item) {
            //            result = (IItem)slot.Value;
            //            player.Equipment.Equipped.Remove(slot.Key);
            //            break;
            //        }
            //    }
            //}
            return result;
        }

		public void AddItemToInventory(IItem item, IActor player) {
            item.Owner = player.Id;
            
            inventory.Add(item);
            if (!InventoryIds.Contains(item.Id))
            {
                InventoryIds.Add(item.Id);
            }

            item.Save();
            player.Save();
        }

        public async void UpdateInventoryFromDatabase(IActor player) {
            var col = MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items");
            var dbItems = await MongoUtils.MongoData.RetrieveObjectsAsync<Items.Items>(col, i => i.Owner == player.Id);
            if (InventoryIds != null)
            {
                dbItems = dbItems.Where(i => !InventoryIds.Contains(i.Id)); //make sure we don't know about this item
            }

            foreach (var item in dbItems) {
                IItem temp = inventory.Where(i => i.Id == item.Id).SingleOrDefault();
                if (temp == null) {
                    inventory.Add(item);
                }
            }
        }

        public List<IItem> GetInventoryAsItemList(IActor player) {
            UpdateInventoryFromDatabase(player);
            return inventory.ToList();
        }

        public List<string> GetInventoryList(IActor player) {
            UpdateInventoryFromDatabase(player);
            List<string> result = new List<string>();
            Dictionary<string, int> itemGroups = new Dictionary<string, int>();

            foreach (IItem item in GetInventoryAsItemList(player)) {
                if (item != null) {
                    if (item.ItemType.ContainsKey(ItemsType.CONTAINER) && item is IContainer containerItem) {
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

            foreach (var pair in itemGroups) {
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

        public List<IItem> GetAllItemsToWear(IActor player) {
            List<IItem> result = new List<IItem>();
            List<List<IItem>> inventorySet = new List<List<IItem>>();

            var inventoryItems = GetInventoryAsItemList(player);
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
