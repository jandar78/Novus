using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rooms;

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
        bool IsOutdoors { get; }
        List<Exits> RoomExits { get; set; }
        List<ObjectId> players { get; set; }
        List<ObjectId> npcs { get; set; }
        List<ObjectId> items { get; set; }
        BsonArray Modifiers { get; set; }
        List<Triggers.GeneralTrigger> Triggers { get; set; }
        IExit GetRoomExit(RoomExits direction);
        void GetRoomExits();
        List<ObjectId> GetObjectsInRoom(RoomObjects objectType, double percentage = 100);
        List<ObjectId> GetObjectsInRoom(string objectType, double percentage = 100);
        RoomTypes GetRoomType();
        void Save();
        void InformPlayersInRoom(IMessage message, List<ObjectId> ignoreId);
        void InformPlayersInRoom(IMessage message, List<object> ignoreId);
    }

    public interface IWeather {
        string Weather { get; set; }
        string WeatherMessage { get; set; }
        int WeatherIntensity { get; set; }
    }

    public interface IRoomModifier
    {
        int Id { get; set; }
        string Name { get; set; }
        double Value { get; set; }

        Dictionary<string, List<string>> ImmuneList { get; set; }

        int Timer { get; set; }
        List<Dictionary<string, object>> Hints { get; set; }
        List<Dictionary<string, object>> Affects { get; set; }
    }
}
