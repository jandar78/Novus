using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IEquipment
    {
        ObjectId playerID { get; set; }
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
}


