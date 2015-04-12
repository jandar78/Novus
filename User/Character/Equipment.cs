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
//played with the idea of an interface but the fact that each one contains a different type of container  made me toss that decision.

namespace Character {
    public class Equipment {
        public string playerID { get; set; } 

        public Dictionary<Items.Wearable, Items.Iitem> equipped;

        public Equipment() {
            equipped = new Dictionary<Items.Wearable, Items.Iitem>();
        }
        
        public bool EquipItem(Items.Iitem item, Inventory inventory) {
            bool result = false;

            Items.Iweapon weaponItem = item as Items.Iweapon;
            if (weaponItem != null && weaponItem.IsWieldable) {
                //can't equip a wieldable weapon
            }
            else {
                if (!equipped.ContainsKey(item.WornOn)) {
                    equipped.Add(item.WornOn, item);
                    if (inventory.inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                        inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                    }
                    result = true;
                }
                else if (item.WornOn == Items.Wearable.WIELD_LEFT || item.WornOn == Items.Wearable.WIELD_RIGHT) { //this item can go in the free hand
                    Items.Wearable freeHand = Items.Wearable.WIELD_LEFT; //we default to right hand for weapons
                    if (equipped.ContainsKey(freeHand)) freeHand = Items.Wearable.WIELD_RIGHT; //maybe this perosn is left handed
                    if (!equipped.ContainsKey(freeHand)) { //ok let's equip this
                        item.WornOn = freeHand;
                        item.Save();
                        equipped.Add(freeHand, item);
                        if (inventory.inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                            inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                        }
                        result = true;
                    }
                }
            }

            return result;
        }

        public bool UnequipItem(Items.Iitem item, Iactor player) {
            bool result = false;
            if (equipped.ContainsKey(item.WornOn)) {
                player.Inventory.inventory.Add(item); //unequipped stuff goes to inventory
                equipped.Remove(item.WornOn);
                
                //if their main hand is now empty, we will make the other hand the main hand
                if (!string.IsNullOrEmpty(player.MainHand) && string.Equals(item.WornOn.ToString(), player.MainHand, StringComparison.InvariantCultureIgnoreCase)){
                    player.MainHand = Items.Wearable.WIELD_RIGHT.ToString();
                    if (item.WornOn == Items.Wearable.WIELD_RIGHT) {
                        player.MainHand = Items.Wearable.WIELD_LEFT.ToString();
                    }
                }
                result = true;
            }
            return result;
        }

        public bool WieldItem(Items.Iitem item, Inventory inventory) {
            bool wielded = false;

            if (!equipped.ContainsKey(Items.Wearable.WIELD_RIGHT)) {
                item.WornOn = Items.Wearable.WIELD_RIGHT;
                wielded = true;    
            }
            else if (!equipped.ContainsKey(Items.Wearable.WIELD_LEFT)) {
                item.WornOn = Items.Wearable.WIELD_LEFT;
                wielded = true;
            }

            if (wielded) {
                equipped.Add(item.WornOn, item);
                inventory.inventory.RemoveWhere(i => i.Id == item.Id);
                return true;
            }

            return false;
        }


        //TODO: this method may need some work done to it
        public void UpdateEquipmentFromDatabase() {
            if (playerID != null) {
                MongoCollection col = MongoUtils.MongoData.GetCollection("World", "Items");
                var docs = col.FindAs<BsonDocument>(Query.EQ("Owner", playerID));
                foreach (BsonDocument dbItem in docs) {
                    ObjectId itemID = dbItem["_id"].AsObjectId;
                    Items.Iitem temp = equipped.Where(i => i.Value.Id == itemID).SingleOrDefault().Value;
                    if (temp == null) {
                        temp = Items.Items.GetByID(dbItem["_id"].AsObjectId.ToString());
                        equipped[temp.WornOn] = temp;
                    }
                }
            }
        }

        public Dictionary<Items.Wearable, Items.Iitem> GetEquipment() {
            UpdateEquipmentFromDatabase();
            return equipped;
        }

        public void Wield(Items.Iitem item, Inventory inventory) {
            WieldItem(item, inventory);
        }

        public List<Items.Iitem> GetWieldedWeapons() {
            List<Items.Iitem> result = new List<Items.Iitem>();
            if (equipped.ContainsKey(Items.Wearable.WIELD_RIGHT)) {
                result.Add((Items.Iitem)Items.ItemFactory.CreateItem(equipped[Items.Wearable.WIELD_RIGHT].Id));
            }
            if (equipped.ContainsKey(Items.Wearable.WIELD_LEFT)) {
                result.Add((Items.Iitem)Items.ItemFactory.CreateItem(equipped[Items.Wearable.WIELD_LEFT].Id));
            }

            return result;
        }

        public Items.Wearable GetMainHandWeapon(Iactor player) {
            if (player.MainHand != null) {
                return (Items.Wearable)Enum.Parse(typeof(Items.Wearable), player.MainHand);
            }
            return Items.Wearable.NONE;
        }
    }
}
