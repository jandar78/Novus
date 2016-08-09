using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {

    [Flags]
    public enum RoomTypes
    {
        //when using flags you should always have a none, ALWAYS.
        NONE = 1 << 0, //this is more readable than 0x0, 0x1, 0x2, 0x4, 0x8, etc 
        OUTDOORS = 1 << 1,
        INDOORS = 1 << 2,
        DARK_CAVE = 1 << 3,
        NO_PVP = 1 << 4,
        FOREST = 1 << 5,
        COLLAPSIBLE = 1 << 6, //walls or ceiling can close in, can be triggered on/off
        DEADLY = 1 << 7 //lava, falling basically a room where death is guaranteed
    };

    public enum RoomExits { None, Up, Down, North, East, South, West };

    public enum RoomObjects { Players, Npcs, Items };

    public interface IRoom {
        string Id { get; set; }
        string Title { get; set; }
        string Zone { get; }
        int RoomId { get; }
        string Description { get; set; }
        bool IsDark { get; }
        bool IsLightSourcePresent { get; }
        string Type { get; set; }
        List<IExit> RoomExits { get; }
        List<string> players { get; set; }
        List<string> npcs { get; set; }
        List<string> items { get; set; }

        IExit GetRoomExit(RoomExits direction);
        void GetRoomExits();
        List<string> GetObjectsInRoom(RoomObjects objectType, double percentage = 100);
        List<string> GetObjectsInRoom(string objectType, double percentage = 100);
        RoomTypes GetRoomType();
        void Save();
        void InformPlayersInRoom(IMessage message, List<object> ignoreId);
        void InformPlayersInRoom(IMessage message, List<string> ignoreId);
    }

    public interface IWeather {
        string WeatherMessage { get; set; }
        int WeatherIntensity { get; set; }
    }

    public interface IRoomModifier
    {
        string Name { get; set; }
        double Value { get; set; }

        Dictionary<string, List<string>> ImmuneList { get; set; }

        int TimeInterval { get; set; }
        List<Dictionary<string, string>> Hints { get; set; }
        List<Dictionary<string, string>> Affects { get; set; }
    }

    public class Room : IRoom
    {
        public string Description { get; set; }
        public string Id { get; set; }
        public bool IsDark { get; }
        public bool IsLightSourcePresent { get; }
        public List<IExit> RoomExits { get; }
        public int RoomId { get; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Zone { get; }
        public List<string> players { get; set; }
        public List<string> npcs { get; set; }
        public List<string> items { get; set; }

        public List<string> GetObjectsInRoom(RoomObjects objectType, double percentage) { return new List<string>(); }
        public List<string> GetObjectsInRoom(string objectType, double percentage) { return new List<string>(); }
        public IExit GetRoomExit(RoomExits direction) { return new Exit(); }
        public void GetRoomExits() { }
        public RoomTypes GetRoomType() { return RoomTypes.NONE; }
        public void Save() { }
        public void InformPlayersInRoom(IMessage message, List<object> ignoreId) { }
        public void InformPlayersInRoom(IMessage message, List<string> ignoreId) { }
    }

    public class RoomModifier {
        public string Name { get; set; }
        public double Value { get; set; }
        public Dictionary<string, List<string>> ImmuneList { get; set; }
        public int TimeInterval { get; set; }
        public List<Dictionary<string, string>> Hints { get; set; }
        public List<Dictionary<string, string>> Affects { get; set; }
    }
}
