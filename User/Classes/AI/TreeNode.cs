using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Rooms;
using MongoDB.Bson;

namespace AI.PathFinding {
	public class TreeNode {
		public string ID { get; private set; }
		public string Zone { get; private set; }
		public int Number { get; private set; }
		public string Title { get; private set; }
		public bool Traversable { get; private set; }
		public bool IgnoreLocks { get; private set; }
		public bool IgnoreBlocked { get; private set; }

		public TreeNode Parent { get; set; }
		public Dictionary<string, TreeNode> AdjacentNodes;

		public TreeNode(IRoom room) {
			if (room != null) {
				ID = room.Id;
				Zone = room.Zone;
				Number = room.RoomId;
				AdjacentNodes = new Dictionary<string, TreeNode>();
				Title = room.Title;
				Traversable = !room.GetRoomType().HasFlag(RoomTypes.DEADLY); //more work to be done here
			}
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

		private string EndPointID { get; set; }

		public TreeTraverser(TreeNode root, string endPointID, bool stayInZone = false) {
			_root = root;
			StayInZone = stayInZone;
			_visitedNodes = new Stack<TreeNode>();
			_unvisitedNodes = new Queue<TreeNode>();
			_unvisitedNodes.Enqueue(_root);
			EndPointID = endPointID;
		}

		public Stack<TreeNode> GetTraversedNodes() {
			TraverseTree().Wait();
			return _visitedNodes;
		}

        public async Task<List<string>> TraverseTree() {
            return await Task.Run(() => {
                TreeNode currentNode = null;

                while (_unvisitedNodes.Count > 0) {
                    currentNode = _unvisitedNodes.Dequeue();

                    if (!_visitedNodes.Any(n => n.ID == currentNode.ID)) {
                        _visitedNodes.Push(currentNode);
                    }

                    if (currentNode.ID == EndPointID) {//we found the target node
                        break;
                    }

                   EnqueueAdjacentNodes(currentNode);
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
            });
		}

        private void AddAdjacentRoomInDirection(TreeNode currentNode, IRoom room, RoomExits direction) {

            if (room.GetRoomExit(direction) != null) {
                if (currentNode.Parent != null && room.GetRoomExit(direction).availableExits[direction].Id != currentNode.Parent.ID) {
                    var newNode = new TreeNode(room.GetRoomExit(direction).availableExits[direction]);
                    if (!_unvisitedNodes.Contains(newNode)) {
                        if (!currentNode.AdjacentNodes.ContainsKey(direction.ToString())) {
                            currentNode.AdjacentNodes.Add(direction.ToString(), newNode);
                            currentNode.AdjacentNodes[direction.ToString()].Parent = currentNode;
                            AddNode(newNode);
                        }

                    }
                }
            }
        }

        private void EnqueueAdjacentNodes(TreeNode currentNode) {
            IRoom room = Room.GetRoom(currentNode.ID);

            foreach (string direction in Enum.GetNames(typeof(RoomExits))) {
                if (direction != "None") {
                    AddAdjacentRoomInDirection(currentNode, room, (RoomExits)Enum.Parse(typeof(RoomExits), direction));
                }
            }
        }


		private void AddNode(TreeNode node) {
			if (node != null) {
				IRoom room = Room.GetRoom(node.ID);

				if (StayInZone && node.Zone != Root.Zone) {
					//Don't add it to the Queue it's out of bounds
				}
				else {
					if (!_visitedNodes.Any(n => n.ID == node.ID)) {
						_unvisitedNodes.Enqueue(node);
					}
				}
			}
		}
	}
}
