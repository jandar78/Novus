using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tree {
	class Program {
		static void Main(string[] args) {
			Node root = new Node(null, null, "A");
			root.LeftChild = new Node(new Node(null, null, "D"), new Node(null, null, "E"), "B");
			root.RightChild = new Node(new Node(null, null, "F"), new Node(null, null, "G"), "C");

			Queue<Node> nodeQueue = new Queue<Node>();
			Stack<string> values = new Stack<string>();
			
			nodeQueue.Enqueue(root); //a
			nodeQueue.Enqueue(root.LeftChild); //b
			nodeQueue.Enqueue(root.RightChild); //c
			nodeQueue.Enqueue(root.LeftChild.LeftChild); //d
			nodeQueue.Enqueue(root.LeftChild.RightChild); //e
			nodeQueue.Enqueue(root.RightChild.LeftChild); //f
			nodeQueue.Enqueue(root.RightChild.RightChild); //g


			while (nodeQueue.Count > 0) {
				Console.Write(nodeQueue.Dequeue().Value + " ");
			}

			Console.WriteLine("\n");

			nodeQueue.Enqueue(root);

			while (nodeQueue.Count > 0) {
				Node node = nodeQueue.Dequeue();

				values.Push(node.Value);

				if (node.LeftChild != null) nodeQueue.Enqueue(node.LeftChild);
				if (node.RightChild != null) nodeQueue.Enqueue(node.RightChild);

			}

			while (values.Count > 0) {
				Console.Write(values.Pop() + " ");
			}

			Console.ReadKey();
		}
	}


	public class Node {
		public Node LeftChild { get; set; }
		public Node RightChild { get; set; }
		public string Value { get; set; }

		public Node(Node left, Node right, string value) {
			LeftChild = left;
			RightChild = right;
			Value = value;
		}

	}
}
