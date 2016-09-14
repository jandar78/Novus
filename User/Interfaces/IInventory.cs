using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB;

//this used to be the Inventory/Equipment class and it held both the inventory and equipment containers and all the methods for each.
//I decided that breaking them both up was a better idea and would make the code easier to maintain and debug.

namespace Interfaces
{
    public interface IInventory
    {
        ObjectId playerID { get; set; }
        HashSet<IItem> inventory { get; set; }

        IItem RemoveInventoryItem(IItem item, IEquipment equipment);
        void AddItemToInventory(IItem item);
        void UpdateInventoryFromDatabase();
        List<IItem> GetInventoryAsItemList();
        List<string> GetInventoryList();
        List<IItem> GetAllItemsToWear();        
    }
}
