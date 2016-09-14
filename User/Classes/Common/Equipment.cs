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
//played with the idea of an interface but the fact that each one contains a different type of container  made me toss that decision.

namespace Character {
    public class Equipment : IEquipment{
        public ObjectId playerID { get; set; } 

        public Dictionary<Wearable, IItem> equipped { get; set; }

        public Equipment() {
            equipped = new Dictionary<Wearable, IItem>();
        }
        
        public bool EquipItem(IItem item, IInventory inventory) {
            bool result = false;

            IWeapon weaponItem = item as IWeapon;
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
                else if (item.WornOn == Wearable.WIELD_LEFT || item.WornOn == Wearable.WIELD_RIGHT) { //this item can go in the free hand
                    Wearable freeHand = Wearable.WIELD_LEFT; //we default to right hand for weapons
                    if (equipped.ContainsKey(freeHand)) freeHand = Wearable.WIELD_RIGHT; //maybe this perosn is left handed
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

        public bool UnequipItem(IItem item, IActor player) {
            bool result = false;
            if (equipped.ContainsKey(item.WornOn)) {
                player.Inventory.inventory.Add(item); //unequipped stuff goes to inventory
                equipped.Remove(item.WornOn);
                
                //if their main hand is now empty, we will make the other hand the main hand
                if (!string.IsNullOrEmpty(player.MainHand) && string.Equals(item.WornOn.ToString(), player.MainHand, StringComparison.InvariantCultureIgnoreCase)){
                    player.MainHand = Wearable.WIELD_RIGHT.ToString();
                    if (item.WornOn == Wearable.WIELD_RIGHT) {
                        player.MainHand = Wearable.WIELD_LEFT.ToString();
                    }
                }
                result = true;
            }
            return result;
        }

        public bool WieldItem(IItem item, IInventory inventory) {
            bool wielded = false;

            if (!equipped.ContainsKey(Wearable.WIELD_RIGHT)) {
                item.WornOn = Wearable.WIELD_RIGHT;
                wielded = true;    
            }
            else if (!equipped.ContainsKey(Wearable.WIELD_LEFT)) {
                item.WornOn = Wearable.WIELD_LEFT;
                wielded = true;
            }

            if (wielded) {
                equipped.Add(item.WornOn, item);
                inventory.inventory.RemoveWhere(i => i.Id == item.Id);
                return true;
            }

            return false;
        }

        public void UpdateEquipmentFromDatabase(Dictionary<Wearable, IItem> equipped) {
            if (playerID != null) {
                var items = MongoUtils.MongoData.RetrieveObjectsAsync<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), i => i.Owner == playerID).Result;
                foreach (var dbItem in items) {
                    //do they have this item equipped?
                    IItem temp = equipped.Where(i => i.Value.Id == dbItem.Id).SingleOrDefault().Value;
                    if (temp == null) {
                    //let's equip it then 
                    //temp = Items.Items.GetByID(dbItem.Id.ToString());
                    equipped[temp.WornOn] = dbItem;
                    }
                }
            }
        }

        public Dictionary<Wearable, IItem> GetEquipment() {
            UpdateEquipmentFromDatabase(equipped);
            return equipped;
        }

        public void Wield(IItem item, IInventory inventory) {
            WieldItem(item, inventory);
        }

        public List<IItem> GetWieldedWeapons() {
            List<IItem> result = new List<IItem>();
            if (equipped.ContainsKey(Wearable.WIELD_RIGHT)) {
                result.Add((IItem)Items.ItemFactory.CreateItem(equipped[Wearable.WIELD_RIGHT].Id));
            }
            if (equipped.ContainsKey(Wearable.WIELD_LEFT)) {
                result.Add((IItem)Items.ItemFactory.CreateItem(equipped[Wearable.WIELD_LEFT].Id));
            }

            return result;
        }

        public Wearable GetMainHandWeapon(IActor player) {
            if (player.MainHand != null) {
                return (Wearable)Enum.Parse(typeof(Wearable), player.MainHand);
            }
            return Wearable.NONE;
        }
    }
}
