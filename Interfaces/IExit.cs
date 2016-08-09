using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Interfaces
{
    public interface IExit
    {
        Dictionary<RoomExits, IRoom> availableExits { get; set; }
        Dictionary<RoomExits, IDoor> doors { get; set; }

        bool HasDoor { get; }
        string Description { get; set; }
        string Direction { get; set; }
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
        BsonArray Triggers { get; set; }
        string Description { get; set; }
        string DescriptionDestroyed { get; set; }

        void LoadTriggers();
        List<string> ApplyDamage(double damage);
        BsonDocument GetDoorFromDB();
        MongoCollection GetDoorCollection();
        void UpdateDoorStatus();
        string CheckPhrase(string message);
    }

    public class Exit : IExit
    {
        public Dictionary<RoomExits, IRoom> availableExits { get; set; }
        public string Description { get; set; }
        public string Direction { get; set; }
        public Dictionary<RoomExits, IDoor> doors { get; set; }
        public bool HasDoor { get; }

        public void Exits() { }
    }

    public class Door : IDoor
    {
        public string Id { get; set; }
        public string Examine { get; set; }
        public bool Breakable { get; set; }
        public bool Openable { get; set; }
        public bool Climable { get; set; }
        public bool Crawlable { get; set; }
        public bool Lockable { get; set; }
        public bool IsPeekable { get; }
        public string Type { get; set; }
        public double Hitpoints { get; set; }
        public bool Open { get; set; }
        public bool Locked { get; set; }
        public string Name { get; set; }
        public bool RequiresKey { get; set; }
        public string GetDescription { get; }
        public bool HasKeyHole { get; set; }
        public bool Destroyed { get; set; }
        public bool Listener { get; set; }
        public BsonArray Phrases { get; set; }
        public BsonArray Triggers { get; set; }
        public string Description { get; set; }
        public string DescriptionDestroyed { get; set; }
        public List<ITrigger> _exitTriggers { get; set; }
        
        public IDoor GetDoor(string doorID, string doorID2 = "") { return new Door(); }
        public void LoadTriggers() { }
        public List<string> ApplyDamage(double damage) { return new List<string>(); }
        public BsonDocument GetDoorFromDB() { return new BsonDocument(); }
        public MongoCollection GetDoorCollection() { return null; }
        public void UpdateDoorStatus() { }
        public string CheckPhrase(string message) { return string.Empty; }
    }
}
