using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rooms {
	
		[Flags]
		public enum RoomTypes {
 			NONE = 1 << 0, //this is more readable than 0x0, 0x1, 0x2, 0x4, 0x8, etc
			OUTDOORS = 1 << 1, 
			INDOORS = 1 << 2, 
			DARK_CAVE = 1 << 3, 
			NO_PVP = 1 << 4, 
			FOREST  = 1 << 5
		}

		
}
