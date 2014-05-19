using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;

namespace Items {
    public partial class Items : Iitem, Iweapon, Iedible, Icontainer, Iiluminate, Iclothing, Ikey {
        
        public double WeightLimit { get; set; }
        public double CurrentWeight { get; set; }
        public double ReduceCarryWeightBy { get; set; }
        public bool Worn { get; set; }
        public bool IsOpenable { get; set; }
        public bool Opened { get; set; }

        public List<string> Contents { get; set; }

        public List<string> GetContents() {
            return Contents;
        }

        public Iitem RetrieveItem(string id) {
            if ((IsOpenable && Opened) || !IsOpenable) {
                if (Contents.Contains(id)) {
                    Iitem temp = Items.GetByID(id);
                    CurrentWeight -= temp.Weight;
                    Contents.Remove(id);
                    Save();
                    if (Worn) {
                        Weight = CurrentWeight - (CurrentWeight * ReduceCarryWeightBy);
                    }
                    return temp;
                }
            }
            return null;
        }

        public bool StoreItem(string id) {
            bool added = false;
            Iitem temp = Items.GetByID(id);

            //containers can't be encumbered they can only hold so much
            if ((IsOpenable && Opened) || !IsOpenable) {
                if (CurrentWeight + temp.Weight <= WeightLimit) {
                    CurrentWeight += temp.Weight;
                    Contents.Add(temp.Id.ToString());
                    added = true;

                    if (Worn) {
                        Weight = CurrentWeight - (CurrentWeight * ReduceCarryWeightBy);
                    }
                    Save();
                }
            }
           
            return added;
        }

        public string Open(){
            string result = null;
            if (IsOpenable && !Opened) {
                result = "You open the " + Name.ToLower();
                Opened = true;
                Save();
            }
            else if (Opened) {
                result = "The " + Name.ToLower() + " is already opened.";
            }
            else {
                result = "The " + Name.ToLower() + " was not designed to be opened.";
            }

            return result;
        }

        public string Close(){
            string result = null;
            if (IsOpenable && Opened) {
                result = "You close the " + Name.ToLower();
                Opened = false;
                Save();
            }
            else if (!Opened) {
                result = "The " + Name.ToLower() + " is already closed.";
            }
            else {
                result = "The " + Name.ToLower() + " was not designed to be closed.";
            }
            
            return result;
        }

        public void Wear() {
            Worn = true;
            if (ItemType.ContainsKey(ItemsType.CONTAINER)) {
                Weight = CurrentWeight - (CurrentWeight * ReduceCarryWeightBy);
            }
            Save();
        }

        public string LookIn() {
            if ((IsOpenable && Opened) || !IsOpenable) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Name + " contents:");
                if (GetContents().Count > 0) {
                    foreach (string itemID in GetContents()) {
                        Iitem tempItem = Items.GetByID(itemID);
                        sb.AppendLine(tempItem.Name);
                    }

                    sb.AppendLine();
                }
                else {
                    sb.AppendLine("\n[ EMPTY ]\n");
                }
                return sb.ToString();
            }
            else {
                return "You must open " + Name + " before seeing what is in it.";
            }
        }
    }
}
