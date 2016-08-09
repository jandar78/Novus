﻿using System;
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
using Crainiate.Diagramming;
using Crainiate.Diagramming.Forms;
using Interfaces;

namespace WorldBuilder {
    public partial class Form1 : Form {
        private List<IExit> exitsInRoom;
        private List<IRoomModifier> modifiersInRoom;

        private void roomRefresh_Click(object sender, EventArgs e) {
            this.roomsListValue.Items.Clear();
            
            if (ConnectedToDB) {
				List<MongoCollection> roomCollections = MongoUtils.MongoData.GetCollections("Rooms");
				foreach (MongoCollection collection in roomCollections) {
					if (!collection.Name.Contains("system")) {
						MongoCursor<BsonDocument> result = null;
						if (string.IsNullOrEmpty(filterValue.Text)) {
							result = collection.FindAllAs<BsonDocument>();
						}
						else {
							if (filterTypeValue.Text == "_id") {
								result = collection.FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, ObjectId.Parse(filterValue.Text)));
							}
							else {
								result = collection.FindAs<BsonDocument>(Query.EQ(filterTypeValue.Text, filterValue.Text));
							}
						}

						foreach (BsonDocument doc in result) {
							this.roomsListValue.Items.Add(doc["Title"].AsString + " (" + doc["_id"].AsString + ")");
						}
					}
				}
            }
        }

        private void roomLoad_Click(object sender, EventArgs e) {
            if (!IsEmpty(roomIdValue.Text)) {
                if (ConnectedToDB) {
                    IRoom room = Rooms.Room.GetRoom(roomIdValue.Text);
                    FillRoomControls(room);
                }
            }
        }

        private void FillRoomControls(IRoom inRoom) {
			if (inRoom != null) {
                Rooms.Room room = inRoom as Rooms.Room;
				roomIdValue.Text = room.Id.ToString();
				roomTitleValue.Text = room.Title;
				roomDescriptionValue.Text = room.Description;
				room.GetRoomExits();
				FillExits(room.RoomExits);
				FillModifiers(Rooms.Room.GetModifiers(room.Id));
				FillMap(room);
			}
			else {
				roomTitleValue.Text = "[New Room]";
				roomDescriptionValue.Text = "[Enter description here]";
				exitsInRoom.Clear();
				modifiersInRoom.Clear();
			}
        }

		private void FillMap(Rooms.Room room) {
			AI.PathFinding.TreeNode startNode = new AI.PathFinding.TreeNode(room);
			startNode.Parent = startNode;
			AI.PathFinding.TreeTraverser traverser = new AI.PathFinding.TreeTraverser(startNode, "");
			Stack<AI.PathFinding.TreeNode> traversedTree = traverser.GetTraversedNodes();

			Crainiate.Diagramming.Model model = new Model();
			
			PointF position = new PointF(200, 400);

			Table roomNode = new Table();
			roomNode.BackColor = Color.Green;
			

			foreach (AI.PathFinding.TreeNode treeNode in traversedTree.Reverse()) {
				if (model.Shapes.ContainsKey(treeNode.ID)) {
					position = model.Shapes[treeNode.ID].Location;
				}

				roomNode.Location = position;
				
				roomNode.Heading = treeNode.ID;
				roomNode.SubHeading = treeNode.Title;
																
				if (!model.Shapes.ContainsKey(treeNode.ID)) {
					model.Shapes.Add(treeNode.ID, roomNode);
				}

				Arrow arrow = new Arrow();
				arrow.DrawBackground = false;
				arrow.Inset = 0;

				foreach (var adjNode in treeNode.AdjacentNodes) {
					RoomExits direction = (RoomExits)Enum.Parse(typeof(RoomExits), adjNode.Key);

					Table adjShape = new Table();
					adjShape.Heading = adjNode.Value.ID;
					adjShape.SubHeading = adjNode.Value.Title;
					adjShape.BackColor = Color.LightBlue;

					switch (direction) {
						case RoomExits.North:
							adjShape.Location = new PointF(position.X, position.Y - 100);
							break;
						case RoomExits.South:
							adjShape.Location = new PointF(position.X, position.Y + 100);
							break;
						case RoomExits.East:
							adjShape.Location = new PointF(position.X + 150, position.Y);
							break;
						case RoomExits.West:
							adjShape.Location = new PointF(position.X - 150, position.Y);
							break;
						case RoomExits.Up:
							adjShape.Location = new PointF(position.X - 150, position.Y - 100);
							break;
						case RoomExits.Down:
							adjShape.Location = new PointF(position.X + 150, position.Y + 100);
							break;
					}

					if (!model.Shapes.ContainsKey(adjNode.Value.ID)) {
						model.Shapes.Add(adjNode.Value.ID, adjShape);
					}
					Connector line = new Connector(model.Shapes[treeNode.ID], model.Shapes[adjNode.Value.ID]);
					line.End.Marker = arrow;

					model.Lines.Add(model.Lines.CreateKey(), line);

					adjShape = new Table();
				}

				roomNode = new Table();
			}

			mapDiagram.SetModel(model);
			mapDiagram.Refresh();

		}


        private void FillModifiers(List<IRoomModifier> list) {
            modifiersInRoom = new List<IRoomModifier>();
            foreach (var modifier in list) {
                roomModifierValue.Items.Add(modifier.Name);
            }
        }

        private void FillExits(List<IExit> exits) {
            roomExitsValue.Items.Clear();
            exitsInRoom = new List<IExit>();
            foreach (IExit exit in exits) {
                roomExitsValue.Items.Add(exit.Direction);
                exitsInRoom.Add(exit);
            }
        }

        private void roomsListValue_DoubleClick(object sender, EventArgs e) {
            IRoom room = LoadRoomInformation();
            displayInGameValue.Text = ClientHandling.MessageBuffer.Format(DisplayAsSeenInGame(room));
        }

        private string DisplayAsSeenInGame(IRoom room) {
            return Look(room);
        }

        private void roomModifierValue_DoubleClick(object sender, EventArgs e) {
            IRoomModifier modifier = modifiersInRoom.Where(mod => mod.Name == roomModifierValue.SelectedValue.ToString()).SingleOrDefault();

        }

        private IRoom LoadRoomInformation() {
            var roomIdToParse = roomsListValue.SelectedItem.ToString();
            roomIdToParse = roomIdToParse.Substring(roomIdToParse.IndexOf('(') + 1).Replace(")", "");
            IRoom room = Rooms.Room.GetRoom(roomIdToParse);
            FillRoomControls(room);
            return room;
        }

        private string Look(IRoom room) {
            StringBuilder sb = new StringBuilder();

                //let's build the description the player will see
                room.GetRoomExits();
                List<IExit> exitList = room.RoomExits;

                sb.AppendLine(("- " + room.Title + " -\t\t\t").ToUpper());
                //TODO: add a "Descriptive" flag, that we will use to determine if we need to display the room description.
                sb.AppendLine(room.Description);
                sb.Append(HintCheck(room.Id));

                foreach (IExit exit in exitList) {
                    sb.AppendLine(GetExitDescription(exit, room));
                }
                        
            return sb.ToString();

        }

        private string GetExitDescription(IExit exit, IRoom room) {
            //there's a lot of sentence  
            string[] vowel = new string[] { "a", "e", "i", "o", "u" };
			RoomExits direction = (RoomExits)Enum.Parse(typeof(RoomExits), exit.Direction.CamelCaseWord());
            if (room.IsDark) {
                exit.Description = "something";
            }

            if (string.IsNullOrEmpty(exit.Description)) {
                exit.Description = exit.availableExits[direction].Title.ToLower();
            }

            if (exit.Description.Contains("that leads to")) {
                exit.Description += exit.availableExits[direction].Title.ToLower();
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
                    exit.Description = exit.availableExits[direction].Title.ToLower();
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
            AddAdjacentRoom(RoomExits.North);
        }

        private void AddAdjacentRoom(RoomExits direction) {
			//todo:we should see if they haven't saved and prompt them too if they are going to be adding a new room
			//let's be smart and detect changes before we prompt them to save. use events wisely.
            string roomID = GetNewRoomId();
			roomIdValue.Text = roomID;
			//todo:we need to add this room as an exit to the previous room, so we'll have to keep it in memory somewhere
			//until they hit save for this room
			roomLoad_Click(null, null);
        }

        private string GetNewRoomId() {
            int id = 0;
            var roomCollection = MongoUtils.MongoData.GetCollection("Rooms", GetZoneCode(roomIdValue.Text));
            var maxRoomNumber = roomCollection.AsQueryable<BsonDocument>().Max(r => ParseRoomID(r.AsBsonDocument["_id"].AsString));
            id = maxRoomNumber++;

			//this may be something we implement in the future right now they can still modify the room ID before saving or should be able to at least
			//if (id > roomCollection.Count()) { //okay let's not waste any good room ID's lets find the missing one in the mix and return it

			//}

			return GetZoneCode(roomIdValue.Text) + id;
        }


        //Room ID's will consist of alpha characters followed by digits
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
    }
}
