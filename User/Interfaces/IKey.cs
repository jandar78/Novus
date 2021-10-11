using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {
   public interface IKey {

        string DoorID { get; set; }
        bool SkeletonKey { get; set; }
    }
}
