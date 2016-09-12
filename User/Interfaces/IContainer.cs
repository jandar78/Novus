using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {
   public interface IContainer {
        List<string> Contents { get; set; }
        List<string> GetContents();
        IItem RetrieveItem(string id);
        bool StoreItem(string id);
        double WeightLimit { get; set; }
        double CurrentWeight { get; set; }
        double ReduceCarryWeightBy { get; set; }
        bool IsOpenable { get; set; }
        bool Opened { get; set; }
        string Examine();
        string Open();
        string Close();
        void Wear();
        string LookIn();
    }
}
