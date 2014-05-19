using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;




namespace Items {
   public interface Iitem {
        ObjectId Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        double Weight { get; set; }
        ItemCondition CurrentCondition { get; set; }
        bool IsWearable { get; set; } 
        int MinimumLevel { get; set; }
        Wearable WornOn { get; set; }
        int Location { get; set; }
        Dictionary<ItemsType, int> ItemType { get; set; }
        void DeteriorateCondition();
        void ImproveCondition();
        void Save();
        string Examine();
        bool IsMovable { get; set; }
        string Owner { get; set; } //set to the ID of the player/NPC or room currently holding the object, otherwise null
        string Trigger { get; set; } //items can accept commands!
        //List<string> ItemType { get; set; }
    }
}
