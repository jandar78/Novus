using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IEquipment
    {
        string playerID { get; set; }
        Dictionary<Wearable, IItem> equipped { get; set; }
        bool EquipItem(IItem item, IInventory inventory);
        bool UnequipItem(IItem item, IActor player);
        bool WieldItem(IItem item, IInventory inventory);
        void UpdateEquipmentFromDatabase(Dictionary<Wearable, IItem> equipped);
        Dictionary<Wearable, IItem> GetEquipment();
        void Wield(IItem item, IInventory inventory);
        List<IItem> GetWieldedWeapons();
        Wearable GetMainHandWeapon(IActor player);
    }

    public class Equipment : IEquipment
    {
        public Dictionary<Wearable, IItem> equipped { get; set; }
        
        public string playerID { get; set; }
        
        public bool EquipItem(IItem item, IInventory inventory)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Wearable, IItem> GetEquipment()
        {
            throw new NotImplementedException();
        }

        public Wearable GetMainHandWeapon(IActor player)
        {
            throw new NotImplementedException();
        }

        public List<IItem> GetWieldedWeapons()
        {
            throw new NotImplementedException();
        }

        public bool UnequipItem(IItem item, IActor player)
        {
            throw new NotImplementedException();
        }

        public void UpdateEquipmentFromDatabase(Dictionary<Wearable, IItem> equipped)
        {
            throw new NotImplementedException();
        }

        public void Wield(IItem item, IInventory inventory)
        {
            throw new NotImplementedException();
        }

        public bool WieldItem(IItem item, IInventory inventory)
        {
            throw new NotImplementedException();
        }
    }
}


