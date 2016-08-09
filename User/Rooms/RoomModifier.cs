using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;

namespace Rooms {
	public class RoomModifier : IRoomModifier {
		public string Name { get; set; }
		public double Value { get; set; }

		public Dictionary<string, List<string>> ImmuneList { get; set; } //key will be Class, Race, Item and the values a list of ID's

		public int TimeInterval { get; set; }
		public List<Dictionary<string, string>> Hints { get; set; }
		public List<Dictionary<string, string>> Affects { get; set; }

		public RoomModifier() {
			ImmuneList = new Dictionary<string, List<string>>();
			Hints = new List<Dictionary<string, string>>();
			Affects = new List<Dictionary<string, string>>();
		}
	}
}
