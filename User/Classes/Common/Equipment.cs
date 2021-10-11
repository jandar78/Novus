using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Interfaces;

//this used to be the Inventory/Equipment class and it held both the inventory and equipment containers and all the methods for each.
//I decided that breaking them both up was a better idea and would make the code easier to maintain and debug.
//played with the idea of an interface but the fact that each one contains a different type of container made me toss that decision.

namespace Character
{
    public class Equipment : IEquipment
    {
        public List<ObjectId> EquipmentIds { get; set; }
        public Dictionary<Wearable, IItem> Equipped { get; set; }

        public Equipment()
        {
            Equipped = new Dictionary<Wearable, IItem>();

            if (EquipmentIds != null)
            {
                foreach (var id in EquipmentIds)
                {
                    var item = Items.ItemFactory.CreateItem(id).Result;
                    Equipped.Add(item.WornOn, item);
                }
            }
        }

        //Todo: probably should send a message to the player informing them that they equipped or didn't equip the item
        public bool EquipItem(IItem item, IActor player)
        {
            bool result = false;

            if (item is IWeapon weaponItem && weaponItem.IsWieldable)
            {
                //can't equip weapons, they get wielded
            }
            else
            {
                if (!Equipped.ContainsKey(item.WornOn))
                {
                    Equipped.Add(item.WornOn, item);
                    EquipmentIds.Add(item.Id);
                    if (player.Inventory.inventory.Any(i => i.Id == item.Id))
                    {//in case we are adding it from a load and not moving it from the inventory
                        player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                    }
                    result = true;
                }
                else if (item.WornOn == Wearable.WIELD_LEFT || item.WornOn == Wearable.WIELD_RIGHT)
                { //this item can go in the free hand
                    Wearable freeHand = Wearable.WIELD_LEFT; //we default to right hand for weapons
                    if (Equipped.ContainsKey(freeHand))
                    {
                        freeHand = Wearable.WIELD_RIGHT; //maybe this person is left handed
                    }

                    if (!Equipped.ContainsKey(freeHand))
                    { //ok let's equip this
                        item.WornOn = freeHand;
                        item.Save();
                        Equipped.Add(freeHand, item);
                        if (player.Inventory.inventory.Any(i => i.Id == item.Id))
                        {//in case we are adding it from a load and not moving it from the inventory
                            player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id); //we moved the item over to equipped so we need it out of inventory
                        }

                        result = true;
                    }
                }
            }

            return result;
        }

        public bool UnequipItem(IItem item, IActor player)
        {
            bool result = false;
            if (Equipped.ContainsKey(item.WornOn))
            {
                player.Inventory.AddItemToInventory(item, player); //unequipped stuff goes to inventory
                Equipped.Remove(item.WornOn);
                EquipmentIds.Remove(item.Id);

                //if their main hand is now empty, we will make the other hand the main hand
                if (!string.IsNullOrEmpty(player.MainHand) && string.Equals(item.WornOn.ToString(), player.MainHand, StringComparison.InvariantCultureIgnoreCase))
                {
                    player.MainHand = Wearable.WIELD_RIGHT.ToString();
                    if (item.WornOn == Wearable.WIELD_RIGHT)
                    {
                        player.MainHand = Wearable.WIELD_LEFT.ToString();
                    }
                }
                result = true;
            }
            return result;
        }

        public bool WieldItem(IItem item, IActor player)
        {
            bool wielded = false;

            if (!Equipped.ContainsKey(Wearable.WIELD_RIGHT))
            {
                item.WornOn = Wearable.WIELD_RIGHT;
                wielded = true;
            }
            else if (!Equipped.ContainsKey(Wearable.WIELD_LEFT))
            {
                item.WornOn = Wearable.WIELD_LEFT;
                wielded = true;
            }

            if (wielded)
            {
                Equipped.Add(item.WornOn, item);
                player.Inventory.inventory.RemoveWhere(i => i.Id == item.Id);
                return true;
            }

            return false;
        }

        public async void UpdateEquipmentFromDatabase(IActor player)
        {
            if (player.Id != ObjectId.Empty)
            {
                var items = await MongoUtils.MongoData.RetrieveObjectsAsync<Items.Items>(MongoUtils.MongoData.GetCollection<Items.Items>("World", "Items"), i => i.Owner == player.Id);
                if (EquipmentIds != null)
                {
                    foreach (var dbItem in items)
                    {
                        //did they have this equipped previously?
                        var tempId = EquipmentIds.Where(e => e == dbItem.Id).SingleOrDefault();
                        if (tempId != null)
                        {
                            //let's equip it then
                            if (dbItem.WornOn != Wearable.NONE)
                            {
                                Equipped[dbItem.WornOn] = dbItem;
                            }
                        }
                    }
                }
            }
        }

        public Dictionary<Wearable, IItem> GetEquipment(IActor player)
        {
            UpdateEquipmentFromDatabase(player);
            return Equipped;
        }

        public void Wield(IItem item, IActor player)
        {
            WieldItem(item, player);
        }

        public List<IItem> GetWieldedWeapons()
        {
            List<IItem> result = new List<IItem>();
            if (Equipped.ContainsKey(Wearable.WIELD_RIGHT))
            {
                result.Add((IItem)Items.ItemFactory.CreateItem(Equipped[Wearable.WIELD_RIGHT].Id));
            }
            if (Equipped.ContainsKey(Wearable.WIELD_LEFT))
            {
                result.Add((IItem)Items.ItemFactory.CreateItem(Equipped[Wearable.WIELD_LEFT].Id));
            }

            return result;
        }

        public Wearable GetMainHandWeapon(IActor player)
        {
            if (player.MainHand != null)
            {
                return (Wearable)Enum.Parse(typeof(Wearable), player.MainHand);
            }
            return Wearable.NONE;
        }
    }
}
