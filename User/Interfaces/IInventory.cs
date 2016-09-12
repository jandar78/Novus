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
        string playerID { get; set; }
        HashSet<IItem> inventory { get; set; }

        IItem RemoveInventoryItem(IItem item, IEquipment equipment);
        void AddItemToInventory(IItem item);
        void UpdateInventoryFromDatabase();
        List<IItem> GetInventoryAsItemList();
        List<string> GetInventoryList();
        List<IItem> GetAllItemsToWear();        
    }

    public class Inventory : IInventory
    {
        public HashSet<IItem> inventory
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string playerID
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void AddItemToInventory(IItem item)
        {
            throw new NotImplementedException();
        }

        public List<IItem> GetAllItemsToWear()
        {
            throw new NotImplementedException();
        }

        public List<IItem> GetInventoryAsItemList()
        {
            throw new NotImplementedException();
        }

        public List<string> GetInventoryList()
        {
            throw new NotImplementedException();
        }

        public IItem RemoveInventoryItem(IItem item, IEquipment equipment)
        {
            throw new NotImplementedException();
        }

        public void UpdateInventoryFromDatabase()
        {
            throw new NotImplementedException();
        }
    }
}
