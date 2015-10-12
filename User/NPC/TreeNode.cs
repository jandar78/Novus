using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.PathFinding {
	public class TreeNode {
		public string ID { get; private set; }
		public string Zone { get; private set; }
		public int Number { get; private set; }
		public bool Traversable { get; private set; }
		public bool IgnoreLocks { get; private set; }
		public bool IgnoreBlocked { get; private set; }
		//		public double Distance { get; set; }

		public TreeNode Parent { get; set; }

		//branching nodes
		public TreeNode Up { get;  set; }
		public TreeNode Down { get;  set; }
		public TreeNode North { get;  set; }
		public TreeNode East { get;  set; }
		public TreeNode South { get;  set; }
		public TreeNode West { get;  set; }

		public TreeNode(Rooms.Room room) {
			if (room != null) {
				ID = room.Id;
				Zone = room.Zone;
				Number = room.RoomId;		
			
				Traversable = !room.GetRoomType().HasFlag(Rooms.RoomTypes.DEADLY);
			}

			//		Distance = double.PositiveInfinity;
		}
	}


	public class TreeTraverser {
		TreeNode _root;
		Queue<TreeNode> _unvisitedNodes;
		Stack<TreeNode> _visitedNodes;
		List<string> _foundPath;

		public bool StayInZone { get; private set; }

		public TreeNode Root {
			get {
				return _root;
			}
			set {
				_root = value;
			}
		}

		private int EndPointNumber { get; set; }
		private string EndPointID { get; set; }

		public TreeTraverser(TreeNode root, string endPointID, int endPointNumber, bool stayInZone = false) {
			_root = root;
			StayInZone = stayInZone;
			_visitedNodes = new Stack<TreeNode>();
			_unvisitedNodes = new Queue<TreeNode>();
			_unvisitedNodes.Enqueue(_root);
			EndPointNumber = endPointNumber;
			EndPointID = endPointID;
		}

		//Djisktras alorgithm is what will need to happen here since we have no idea ever where our end room is going to be or har far/close
		//All the rooms will have a value of the same type -unless we penalize for certain rooms types (crossing rivers on foot would be bad same as gorges)

		public List<string> TraverseTree() {
			TreeNode currentNode = null;
			while (_unvisitedNodes.Count > 0) {
				currentNode = _unvisitedNodes.Dequeue();
				Rooms.Room room = Rooms.Room.GetRoom(currentNode.ID);

				if (room.GetRoomExit(Rooms.RoomExits.Up) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.Up).availableExits[Rooms.RoomExits.Up].Id != currentNode.Parent.ID) {
						var upNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.Up).availableExits[Rooms.RoomExits.Up]);
						if (!_unvisitedNodes.Contains(upNode)) {
							currentNode.Up = upNode;
							currentNode.Up.Parent = currentNode;
							AddNode(upNode);
						}
					}
				}
				if (room.GetRoomExit(Rooms.RoomExits.Down) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.Down).availableExits[Rooms.RoomExits.Down].Id != currentNode.Parent.ID) {
						var downNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.Down).availableExits[Rooms.RoomExits.Down]);
						if (!_unvisitedNodes.Contains(downNode)) {
							currentNode.Down = downNode;
							currentNode.Down.Parent = currentNode;
							AddNode(downNode);
						}
					}
				}
				if (room.GetRoomExit(Rooms.RoomExits.North) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.North).availableExits[Rooms.RoomExits.North].Id != currentNode.Parent.ID) {
						var northNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.North).availableExits[Rooms.RoomExits.North]);
						if (!_unvisitedNodes.Contains(northNode)) {
							currentNode.North = northNode;
							currentNode.North.Parent = currentNode;
							AddNode(northNode);
						}
					}
				}
				if (room.GetRoomExit(Rooms.RoomExits.East) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.East).availableExits[Rooms.RoomExits.East].Id != currentNode.Parent.ID) {
						var eastNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.East).availableExits[Rooms.RoomExits.East]);
						if (!_unvisitedNodes.Contains(eastNode)) {
							currentNode.East = eastNode;
							currentNode.East.Parent = currentNode;
							AddNode(eastNode);
						}
					}
				}
				if (room.GetRoomExit(Rooms.RoomExits.South) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.South).availableExits[Rooms.RoomExits.South].Id != currentNode.Parent.ID) {
						var southNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.South).availableExits[Rooms.RoomExits.South]);
						if (!_unvisitedNodes.Contains(southNode)) {
							currentNode.South = southNode;
							currentNode.South.Parent = currentNode;
							AddNode(southNode);
						}
					}
				}
				if (room.GetRoomExit(Rooms.RoomExits.West) != null) {
					if (currentNode.Parent != null && room.GetRoomExit(Rooms.RoomExits.West).availableExits[Rooms.RoomExits.West].Id != currentNode.Parent.ID) {
						var westNode = new TreeNode(room.GetRoomExit(Rooms.RoomExits.West).availableExits[Rooms.RoomExits.West]);
						if (!_unvisitedNodes.Contains(westNode)) {
							currentNode.West = westNode;
							currentNode.West.Parent = currentNode;
							AddNode(westNode);
						}
					}
				}


				ProcessNode(currentNode);
				if (currentNode.ID == EndPointID) //we found the target node
					break;
			}

			_foundPath = new List<string>();

			if (_visitedNodes.Any(tn => tn.ID == EndPointID)) { //did we find the target node? Then let's make a path
				TreeNode pathNode = _visitedNodes.Pop();
				while (pathNode.ID != Root.ID) {
					_foundPath.Add(pathNode.ID);
					pathNode = pathNode.Parent;
				}
			}

			return _foundPath;
		}

		private void ProcessNode(TreeNode node) {
			_visitedNodes.Push(node);
        }

		private void AddNode(TreeNode node) {
			if (node != null) {
				Rooms.Room room = Rooms.Room.GetRoom(node.ID);

				if (StayInZone && node.Zone != Root.Zone) {
					//Don't add it to the Queue it's out of bounds
				}
				else {
					_unvisitedNodes.Enqueue(node);
				}

			}
		}

		
	}
}
