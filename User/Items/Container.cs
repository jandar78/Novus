using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using Interfaces;

namespace Items {
    public sealed partial class Items : IItem, IWeapon, IEdible, IContainer, IIluminate, IClothing, IKey {
        
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

        public IItem RetrieveItem(string id) {
            if ((IsOpenable && Opened) || !IsOpenable) {
                if (Contents.Contains(id)) {
                    IItem temp = Items.GetByID(id).Result;
                    CurrentWeight -= temp.Weight;
                    Contents.Remove(id);
                    Save();
                    if (Worn) {
                        Weight = CurrentWeight - (CurrentWeight * ReduceCarryWeightBy);
                    }
                    OnRetrieved(new ItemEventArgs(ItemEvent.RETRIEVE, this.Id));
                    return temp;
                }
            }
            return null;
        }

        public bool StoreItem(string id) {
            bool added = false;
            IItem temp = Items.GetByID(id).Result;

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
                    OnStored(new ItemEventArgs(ItemEvent.STORE, this.Id));
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
                OnOpened(new ItemEventArgs(ItemEvent.OPEN, this.Id));
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
                OnClosed(new ItemEventArgs(ItemEvent.CLOSE, this.Id));
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
            OnWorn(new ItemEventArgs(ItemEvent.WEAR, this.Id));
        }

        public string LookIn() {
            if ((IsOpenable && Opened) || !IsOpenable) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Name + " contents:");
                if (GetContents().Count > 0) {
                    foreach (string itemID in GetContents()) {
                        IItem tempItem = Items.GetByID(itemID).Result;
                        sb.AppendLine(tempItem.Name);
                    }

                    sb.AppendLine();
                }
                else {
                    sb.AppendLine("\n[ EMPTY ]\n");
                }
                OnLookedIn(new ItemEventArgs(ItemEvent.LOOK_IN, this.Id));
                return sb.ToString();
            }
            else {
                return "You must open " + Name + " before seeing what is in it.";
            }
        }
    }
}
