using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Character {
    public class Equipment {
        public Dictionary<Items.Wearable, Items.Iitem> equipped;
        public HashSet<Items.Iitem> inventory;

        public Equipment() {
            equipped = new Dictionary<Items.Wearable, Items.Iitem>();
            inventory = new HashSet<Items.Iitem>();
        }

        public void AddInventoryItem(Items.Iitem item) {
            inventory.Add(item);
        }

        public Items.Iitem RemoveInventoryItem(Items.Iitem item) {
            Items.Iitem result = null;
          
            result = inventory.Where(i => i.Id == item.Id).SingleOrDefault();
            inventory.RemoveWhere(i => i.Id == item.Id);

            if (result == null) { //so if it wasn't in the inventory we need to check what is equipped
                foreach (KeyValuePair<Items.Wearable, Items.Iitem> slot in equipped) {
                    if (slot.Value == item) {
                        result = (Items.Iitem)slot.Value; 
                        equipped.Remove(slot.Key);
                        break;
                    }
                }
            }

            return result;
        }

        public bool EquipItem(Items.Iitem item) {
            //Wieldable items cannot be equipped they must be wielded that check needs to go here
            bool result = false;
            if (!equipped.ContainsKey(item.WornOn)) {
                equipped.Add(item.WornOn, item);
                if (inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                    inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
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
                    if (inventory.Any(i => i.Id == item.Id)) {//in case we are adding it from a load and not moving it from the inventory
                        inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                    }
                    result = true;
                }
            }

            return result;
        }

        public bool UnequipItem(Items.Iitem item, out string resultHand, string mainHand = null) {
            bool result = false;
            resultHand = mainHand;
            if (equipped.ContainsKey(item.WornOn)) {
                inventory.Add(item); //unequipped stuff goes to inventory
                equipped.Remove(item.WornOn);

                if (!string.IsNullOrEmpty(mainHand) && string.Equals(item.WornOn.ToString(), mainHand, StringComparison.InvariantCultureIgnoreCase) == true){
                    resultHand = null;
                }
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
                equipped.Add(Items.Wearable.WIELD_RIGHT, item);
                inventory.RemoveWhere(i => i.Id == item.Id);
                return true;
            }

            return false;
        }
    }
}
