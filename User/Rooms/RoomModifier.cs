using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rooms {
	public class RoomModifier {
		public string Name { get; set; }
		public double Value { get; set; }

		public Dictionary<string, List<string>> ImmuneList; //key will be Class, Race, Item and the values a list of ID's

		public int TimeInterval { get; set; }
		public List<Dictionary<string, string>> Hints;
		public List<Dictionary<string, string>> Affects;

		public RoomModifier() {
			ImmuneList = new Dictionary<string, List<string>>();
			Hints = new List<Dictionary<string, string>>();
			Affects = new List<Dictionary<string, string>>();
		}
	}
}
