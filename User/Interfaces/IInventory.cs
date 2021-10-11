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
        HashSet<IItem> inventory { get; set; }

        List<ObjectId> InventoryIds { get; set;}

        IItem RemoveInventoryItem(IItem item, IActor player);
        void AddItemToInventory(IItem item, IActor player);
        void UpdateInventoryFromDatabase(IActor player);
        List<IItem> GetInventoryAsItemList(IActor player);
        List<string> GetInventoryList(IActor player);
        List<IItem> GetAllItemsToWear(IActor player);        
    }
}
