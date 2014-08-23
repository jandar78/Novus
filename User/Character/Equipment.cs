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
        public Iactor player { get; set; } //I don't like the circular dependency this creates but oh well.

        public Dictionary<Items.Wearable, Items.Iitem> equipped;

        public Equipment() {
            equipped = new Dictionary<Items.Wearable, Items.Iitem>();
        }
        
        public bool EquipItem(Items.Iitem item) {
            //Wieldable items cannot be equipped they must be wielded that check needs to go here
            bool result = false;

            Items.Iweapon weaponItem = item as Items.Iweapon;
            if (weaponItem != null && weaponItem.IsWieldable) {
                //can't equip a wieldable weapon
            }
            else {
                if (!equipped.ContainsKey(item.WornOn)) {
                    equipped.Add(item.WornOn, item);
                    if (player.Inventory.inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                        player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
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
                        if (player.Inventory.inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                            player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                        }
                        result = true;
                    }
                }
            }

            return result;
        }

        public bool UnequipItem(Items.Iitem item) {
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
                player.Save();
                result = true;
            }
            return result;
        }

        public bool WieldItem(Items.Iitem item) {
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
                player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id);
                return true;
            }

            return false;
        }

        public void UpdateEquipmentFromDatabase() {
            if (player.ID != null) {
                MongoCollection col = MongoUtils.MongoData.GetCollection("World", "Items");
                var docs = col.FindAs<BsonDocument>(Query.EQ("Owner", player.ID));
                foreach (BsonDocument dbItem in docs) {
                    ObjectId itemID = dbItem["_id"].AsObjectId;
                    Items.Iitem temp = player.Equipment.equipped.Where(i => i.Value.Id == itemID).SingleOrDefault().Value;
                    if (temp == null) {
                        player.Equipment.equipped[temp.WornOn] = Items.Items.GetByID(dbItem["_id"].AsObjectId.ToString());
                    }
                }
            }
        }

        public Dictionary<Items.Wearable, Items.Iitem> GetEquipment() {
            UpdateEquipmentFromDatabase();
            return player.Equipment.equipped;
        }

        public void Wield(Items.Iitem item) {
            player.Equipment.WieldItem(item);
            player.Save();
        }

        public List<Items.Iitem> GetWieldedWeapons() {
            List<Items.Iitem> result = new List<Items.Iitem>();
            if (player.Equipment.equipped.ContainsKey(Items.Wearable.WIELD_RIGHT)) {
                result.Add((Items.Iitem)Items.ItemFactory.CreateItem(player.Equipment.equipped[Items.Wearable.WIELD_RIGHT].Id));
            }
            if (player.Equipment.equipped.ContainsKey(Items.Wearable.WIELD_LEFT)) {
                result.Add((Items.Iitem)Items.ItemFactory.CreateItem(player.Equipment.equipped[Items.Wearable.WIELD_LEFT].Id));
            }

            return result;
        }

        public Items.Wearable GetMainHandWeapon() {
            if (player.MainHand != null) {
                return (Items.Wearable)Enum.Parse(typeof(Items.Wearable), player.MainHand);
            }
            return Items.Wearable.NONE;
        }
    }
}
