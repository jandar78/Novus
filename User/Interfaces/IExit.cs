using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using Triggers;
using Rooms;

namespace Interfaces
{
    public interface IExit
    {
        Dictionary<RoomExits, IRoom> AvailableExits { get; set; }
        Dictionary<RoomExits, IDoor> Doors { get; set; }

        bool HasDoor { get; }
        string Description { get; set; }
        string Direction { get; set; }
        string Name { get; set; }
        string LeadsToRoom { get; set; }
    }

    public interface IDoor
    {
        string Id { get; set; }
        string Examine { get; set; }
        bool Breakable { get; set; }
        bool Openable { get; set; }
        bool Climable { get; set; }
        bool Crawlable { get; set; }
        bool Lockable { get; set; }
        bool IsPeekable { get; }
        string Type { get; set; }
        double Hitpoints { get; set; }
        bool Open { get; set; }
        bool Locked { get; set; }
        string Name { get; set; }
        bool RequiresKey { get; set; }
        string GetDescription { get; }
        bool HasKeyHole { get; set; }
        bool Destroyed { get; set; }
        bool Listener { get; set; }
        BsonArray Phrases { get; set; }
        List<GeneralTrigger> Triggers { get; set; }
        string Description { get; set; }
        string DescriptionDestroyed { get; set; }

        void LoadTriggers();
        List<string> ApplyDamage(double damage);
        IDoor GetDoorFromDB();
        IMongoCollection<Door> GetDoorCollection();
        void UpdateDoorStatus();
        string CheckPhrase(string message);
    }
}
