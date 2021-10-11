using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces {
   public interface IContainer {

        List<ObjectId> Contents { get; set; }
        List<ObjectId> GetContents();
        IItem RetrieveItem(ObjectId id);
        bool StoreItem(ObjectId id);
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
