using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Extensions;
using System.Text.RegularExpressions;

namespace WorldBuilder {
    public partial class Form1 : Form {
        private List<Rooms.Exits> exitsInRoom;
        private List<Rooms.RoomModifier> modifiersInRoom;

        private void roomRefresh_Click(object sender, EventArgs e) {
            this.roomsListValue.Items.Clear();
            
            if (ConnectedToDB) {
                MongoCursor<BsonDocument> result = null;
                if (string.IsNullOrEmpty(filterValue.Text)) {
                    result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAllAs<BsonDocument>();
                }
                else {
                    if (filterTypeValue.Text == "_id") {
                        result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, ObjectId.Parse(filterValue.Text)));
                    }
                    else {
                        result = MongoUtils.MongoData.GetCollection("World", "Rooms").FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, filterValue.Text));
                    }
                }

                foreach (BsonDocument doc in result) {
                    this.roomsListValue.Items.Add(doc["Title"].AsString + " (" + doc["_id"].AsInt32 + ")" );
                }
            }
        }

        private void roomLoad_Click(object sender, EventArgs e) {
            if (!IsEmpty(roomIdValue.Text)) {
                if (ConnectedToDB) {
                    Rooms.Room room = Rooms.Room.GetRoom(roomIdValue.Text);
                    FillRoomControls(room);
                }
            }
        }

        private void FillRoomControls(Rooms.Room room) {
            roomIdValue.Text = room.Id.ToString();
            roomTitleValue.Text = room.Title;
            roomDescriptionValue.Text = room.Description;
            room.GetRoomExits();
            FillExits(room.RoomExits);
            FillModifiers(Rooms.Room.GetModifiers(room.Id));
        }

        private void FillModifiers(List<Rooms.RoomModifier> list) {
            modifiersInRoom = new List<Rooms.RoomModifier>();
            foreach (var modifier in list) {
                roomModifierValue.Items.Add(modifier.Name);
            }
        }

        private void FillExits(List<Rooms.Exits> exits) {
            roomExitsValue.Items.Clear();
            exitsInRoom = new List<Rooms.Exits>();
            foreach (Rooms.Exits exit in exits) {
                roomExitsValue.Items.Add(exit.Direction);
                exitsInRoom.Add(exit);
            }
        }

        private void roomsListValue_DoubleClick(object sender, EventArgs e) {
            Rooms.Room room = LoadRoomInformation();
            displayInGameValue.Text = ClientHandling.MessageBuffer.Format(DisplayAsSeenInGame(room));
        }

        private string DisplayAsSeenInGame(Rooms.Room room) {
            return Look(room);
        }

        private void roomModifierValue_DoubleClick(object sender, EventArgs e) {
            Rooms.RoomModifier modifier = modifiersInRoom.Where(mod => mod.Name == roomModifierValue.SelectedValue.ToString()).SingleOrDefault();

        }

        private Rooms.Room LoadRoomInformation() {
            var roomIdToParse = roomsListValue.SelectedItem.ToString();
            roomIdToParse = roomIdToParse.Substring(roomIdToParse.IndexOf('(') + 1).Replace(")", "");
            Rooms.Room room = Rooms.Room.GetRoom(roomIdValue.Text);
            FillRoomControls(room);
            return room;
        }

        private string Look(Rooms.Room room) {
            StringBuilder sb = new StringBuilder();

                //let's build the description the player will see
                room.GetRoomExits();
                List<Rooms.Exits> exitList = room.RoomExits;

                sb.AppendLine(("- " + room.Title + " -\t\t\t").ToUpper());
                //TODO: add a "Descriptive" flag, that we will use to determine if we need to display the room description.
                sb.AppendLine(room.Description);
                sb.Append(HintCheck(room.Id));

                foreach (Rooms.Exits exit in exitList) {
                    sb.AppendLine(GetExitDescription(exit, room));
                }
                        
            return sb.ToString();

        }

        private string GetExitDescription(Rooms.Exits exit, Rooms.Room room) {
            //there's a lot of sentence  
            string[] vowel = new string[] { "a", "e", "i", "o", "u" };

            if (room.IsDark) {
                exit.Description = "something";
            }

            if (string.IsNullOrEmpty(exit.Description)) {
                exit.Description = exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
            }

            if (exit.Description.Contains("that leads to")) {
                exit.Description += exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
            }

            string directionCorrected = "To the " + exit.Direction + " there is ";

            if (String.Compare(exit.Direction, "up", true) == 0 || String.Compare(exit.Direction, "down", true) == 0) {
                if (!room.IsDark) {
                    directionCorrected = exit.Description;
                }
                else {
                    directionCorrected = "something";
                }

                directionCorrected += " leads " + exit.Direction + " towards ";

                if (!room.IsDark) {
                    exit.Description = "somewhere";
                }
                else {
                    exit.Description = exit.availableExits[exit.Direction.CamelCaseWord()].Title.ToLower();
                }
            }

            if (!exit.Description.Contains("somewhere") && vowel.Contains(exit.Description[0].ToString())) {
                directionCorrected += "an ";
            }
            else if (!exit.Description.Contains("somewhere") && exit.Description != "something") {
                directionCorrected += "a ";
            }

            return (directionCorrected + exit.Description + ".");
        }

        private string HintCheck(string roomId) {
            StringBuilder sb = new StringBuilder();
            //let's display the room hints if the player passes the check
            foreach (Rooms.RoomModifier mod in Rooms.Room.GetModifiers(roomId)) {
                foreach (Dictionary<string, string> dic in mod.Hints) {
                    sb.AppendLine(dic["Display"]);
                }
            }
            return sb.ToString();
        }

        private void addRoomNorth_Click(object sender, EventArgs e) {
            AddAdjacentRoom(RoomDirection.North);
        }

        private void AddAdjacentRoom(RoomDirection direction) {
            int roomID = GetNewRoomId();
        }

        private int GetNewRoomId() {
            int id = 0;
            MongoCollection roomCollection = MongoUtils.MongoData.GetCollection("World", "Rooms");
            List<BsonDocument> roomCursor = roomCollection.AsQueryable<BsonDocument>().Where(r => r["_id"].AsString.StartsWith(GetZoneCode(roomIdValue.Text))).ToList();
                //.FindAs<BsonDocument>(Query.Matches("_id", new BsonRegularExpression(roomIdValue.Text.Substring(0, 1) + "\\d", "/i/m"))).ToList();
            //id = ParseRoomID(rooms.Max(r => r["_id"].AsString));
            id += 1;

            //if (id >  rooms.Count) { //okay let's not waste any good room ID's lets find the missing one in the mix and return it
               
            //}

            return id;
        }


        //Room ID's will consist of a alpha characters followed by three digits
        //this will allow us to have literally as many zones as we want with as many rooms as we want.
        //A0001, AAB7654, A354738, etc, etc.
        private int ParseRoomID(string roomId) {
            int ignore = 0;
            foreach (char alpha in roomId) {
                if (!char.IsDigit(alpha)) {
                    ignore++;
                }
                else {
                    break;
                }
            }

            int id = 0;
            int.TryParse(roomId.Substring(ignore), out id);

            return id;
        }

        private string GetZoneCode(string roomId) {
            int include = 0;
            foreach (char alpha in roomId) {
                if (char.IsDigit(alpha)) {
                    include++;
                }
                else {
                    break;
                }
            }

            return roomId.Substring(0, include);
        }

        private enum RoomDirection {
            North, South, West, East, Up, Down
        };
    }
}
