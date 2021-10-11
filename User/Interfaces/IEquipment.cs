using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;


namespace Interfaces
{
    public interface IEquipment
    {
        List<ObjectId> EquipmentIds { get; set; }

        [BsonIgnore]
        Dictionary<Wearable, IItem> Equipped { get; set; }
        bool EquipItem(IItem item, IActor player);
        bool UnequipItem(IItem item, IActor player);
        bool WieldItem(IItem item, IActor player);
        void UpdateEquipmentFromDatabase(IActor player);
        Dictionary<Wearable, IItem> GetEquipment(IActor player);
        void Wield(IItem item, IActor player);
        List<IItem> GetWieldedWeapons();
        Wearable GetMainHandWeapon(IActor player);
    }
}


